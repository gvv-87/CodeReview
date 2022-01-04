using Monitel.Mal.Context.CIM16;
using Monitel.Mal.Context.CIM16.Ext.EMS;
using Monitel.Rtdb.Api;
using Monitel.SCADA.Tools.DataExplorer.Extensions;
using Monitel.TDAL;
using Monitel.TDAL.DataStructure;
using System;
using System.Linq;
using System.Windows.Media;

namespace Monitel.SCADA.Tools.DataExplorer.Models
{
    /// <summary>
    /// Модель представления для параметра БДРВ типа OIValue загруженного из файла
    /// </summary>
    public abstract class RtdbQueryValueItem : IQueryValueItem, IComparable
    {
        #region Properties

        private string _name;
        private string _path;

        public long Id { get; private set; }

        public Guid Uid { get; private set; }

        public string Name
        {
            get
            {
                return MeasureValue.IsAlive
                    ? MeasureValue.name
                    : _name;
            }

            set
            {
                _name = value;
            }
        }

        public string DisplayName
        {
            get
            {
                return Name;
            }
        }

        public string SpecificName
        {
            get
            {
                return MeasureValue.GetSpecificName();
            }
        }

        public string Path
        {
            get
            {
                if (_path == null)
                {
                    var path = MeasureValue.GetPathInTree(false, Names.Substation.ClassName).Reverse().ToArray();

                    _path = path.Length == 0
                        ? Name
                        : String.Join("\\", path.Select(x => x.name)) + "\\";
                }

                return _path;
            }
        }

        public string MetaTypeName { get; private set; }

        public object Value { get; protected set; }

        public uint Flags { get; set; }

        public DateTime? QueryTime { get; set; }

        public DateTime? Tstamp { get; set; }

        public DateTime? Tstamp2 { get; set; }

        public DateTime? TstampLocal
        {
            get
            {
                return Tstamp.HasValue ? PlatformInfrastructure.TimeConvert.Converter.SystemToLocal(Tstamp.Value) : (DateTime?)null;
            }
        }

        public DateTime? Tstamp2Local
        {
            get
            {
                return Tstamp2.HasValue ? PlatformInfrastructure.TimeConvert.Converter.SystemToLocal(Tstamp2.Value) : (DateTime?)null;
            }
        }

        public double TimeDif
        {
            get
            {
                return Tstamp.HasValue && Tstamp2.HasValue
                    ? Tstamp.Value.Subtract(Tstamp2.Value).TotalMilliseconds
                    : 0;
            }
        }

        public MeasurementValue MeasureValue { get; private set; }

        public ImageSource ImageSource { get; protected set; }

        public IQueryValueItem Self
        {
            get
            {
                return this;
            }
        }

        public bool? InFilter { get; set; }

        public int? AddrInt1
        {
            get
            {
                switch (MeasureValue)
                {
                    case RemoteAnalogValue rav:
                        return rav.RemoteSource?.addrInt1;

                    case RemoteDiscreteValue rdv:
                        return rdv.RemoteSource?.addrInt1;

                    default:
                        return null;
                }
            }
        }

        public int? AddrInt2
        {
            get
            {
                switch (MeasureValue)
                {
                    case RemoteAnalogValue rav:
                        return rav.RemoteSource?.addrInt2;

                    case RemoteDiscreteValue rdv:
                        return rdv.RemoteSource?.addrInt2;

                    default:
                        return null;
                }
            }
        }

        public string AddrStr1
        {
            get
            {
                switch (MeasureValue)
                {
                    case RemoteAnalogValue rav:
                        return rav.RemoteSource?.addrStr1;

                    case RemoteDiscreteValue rdv:
                        return rdv.RemoteSource?.addrStr1;

                    default:
                        return null;
                }
            }
        }

        #endregion

        #region Constructor

        protected RtdbQueryValueItem(long id, Guid uid, MeasurementValue meas, uint flags, DateTime? tstamp, DateTime? tstamp2, string mtName)
        {
            Id = id;
            Uid = uid;
            MeasureValue = meas;
            Name = meas.name;
            Flags = flags;
            Tstamp = tstamp;
            Tstamp2 = tstamp2;
            MetaTypeName = mtName;
        }

        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            return obj is RtdbQueryValueItem other
                ? other.MeasureValue?.Id == MeasureValue?.Id
                : false;
        }

        public int CompareTo(object obj)
        {
            return Equals(obj)
                ? 0
                : 1;
        }

        public override int GetHashCode()
        {
            return (int)Id;
        }

        #endregion
    }

    public class RtdbQueryAnalogValueItem : RtdbQueryValueItem, IQueryAnalogValueItem
    {
        public double AnalogValue
        {
            get
            {
                return Value is Double
                    ? (Double)Value
                    : 0;
            }
        }

        public RtdbQueryAnalogValueItem(MeasurementValue meas, RtdbValue source, ImageSource imgSource)
            : base(meas.Id, meas.Uid, meas, source.QualityCodes, source.Time, source.Time2, meas.MetaType.Name)
        {
            ImageSource = imgSource;
            Value = source.Value.AnalogValue;
        }

        public RtdbQueryAnalogValueItem(MeasurementValue meas, TDALValue source, ImageSource imgSource)
           : base(meas.Id, meas.Uid, meas, source.Flags, source.Tstamp, source.Tstamp2, meas.MetaType.Name)
        {
            ImageSource = imgSource;
            Value = source.DoubleValue;
        }

        public RtdbQueryAnalogValueItem(IQueryAnalogValueItem val, ImageSource imgSource)
         : base(val.Id, val.Uid, null, val.Flags, val.Tstamp, val.Tstamp2, val.MetaTypeName)
        {
            ImageSource = imgSource;
            Value = val.Value;
        }
    }

    public class RtdbQueryDiscreteValueItem : RtdbQueryValueItem, IQueryDiscreteValueItem
    {
        public long DiscreteValue
        {
            get
            {
                return Value is long
                    ? (long)Value
                    : 0;
            }
        }

        public RtdbQueryDiscreteValueItem(MeasurementValue meas, RtdbValue source, ImageSource imgSource)
            : base(meas.Id, meas.Uid, meas, source.QualityCodes, source.Time, source.Time2, meas.MetaType.Name)
        {
            ImageSource = imgSource;
            Value = source.Value.DiscreteValue;
        }

        public RtdbQueryDiscreteValueItem(MeasurementValue meas, TDALValue source, ImageSource imgSource)
           : base(meas.Id, meas.Uid, meas, source.Flags, source.Tstamp, source.Tstamp2, meas.MetaType.Name)
        {
            ImageSource = imgSource;
            Value = source.LongValue;
        }

        public RtdbQueryDiscreteValueItem(IQueryAnalogValueItem val, ImageSource imgSource)
         : base(val.Id, val.Uid, null, val.Flags, val.Tstamp, val.Tstamp2, val.MetaTypeName)
        {
            ImageSource = imgSource;
            Value = val.Value;
        }
    }

    public class RtdbQueryStringValueItem : RtdbQueryValueItem, IQueryStringValueItem
    {
        public string StringValue
        {
            get
            {
                return Value is string
                    ? (string)Value
                    : String.Empty;
            }
        }

        public RtdbQueryStringValueItem(MeasurementValue meas, RtdbStringValue source, ImageSource imgSource)
            : base(meas.Id, meas.Uid, meas, source.QualityCodes, source.Time, source.Time2, meas.MetaType.Name)
        {
            ImageSource = imgSource;
            Value = source.Value;
        }

        public RtdbQueryStringValueItem(MeasurementValue meas, ITDALValueString source, ImageSource imgSource)
           : base(meas.Id, meas.Uid, meas, source.QualityCodes, source.Time, source.Time2, meas.MetaType.Name)
        {
            ImageSource = imgSource;
            Value = source.Value;
        }

        public RtdbQueryStringValueItem(IQueryAnalogValueItem val, ImageSource imgSource)
         : base(val.Id, val.Uid, null, val.Flags, val.Tstamp, val.Tstamp2, val.MetaTypeName)
        {
            ImageSource = imgSource;
            Value = val.Value;
        }
    }
}

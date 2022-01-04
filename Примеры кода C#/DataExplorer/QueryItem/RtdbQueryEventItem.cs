using Monitel.Mal.Context.CIM16;
using Monitel.Rtdb.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Monitel.SCADA.Tools.DataExplorer.Extensions;
using Monitel.Rtdb.Api.CustomParams;
using Monitel.Localization;

namespace Monitel.SCADA.Tools.DataExplorer.Models
{
    /// <summary>
    /// Модель представления для параметра БДРВ типа CKEvent
    /// </summary>
    public class RtdbQueryEventItem
    {
        #region Properties

        private string _path;

        public List<string> Params { get; private set; }

        public string DisplayName
        {
            get
            {
                return String.Format("{0}:{1}", Id, Name);
            }
        }

        public long Id
        {
            get
            {
                return EventType.Id;
            }
        }

        public string Name
        {
            get
            {
                return EventType.name;
            }
        }

        public int EvKey
        {
            get
            {
                return Source.Key;
            }
        }

        public string Message
        {
            get
            {
                return Source.Message;
            }
        }

        public DateTime Time
        {
            get
            {
                return Source.Time;
            }
        }

        public ushort Flags
        {
            get
            {
                return Source.Flags;
            }
        }

        public Guid UidEnObj
        {
            get
            {
                return Source.ObjectUid;
            }
        }

        public ushort Level
        {
            get
            {
                return 0;// Source.Level;
            }
        }

        public ushort Category
        {
            get
            {
                return 0;//Source.Category;
            }
        }

        public Guid UidUser
        {
            get
            {
                return Source.UserUid;
            }
        }

        public string Path
        {
            get
            {
                if (_path == null)
                {
                    var path = EventType.GetPathInTree().Reverse().ToArray();

                    _path = path.Length == 0
                        ? Name
                        : String.Join("\\", path.Select(x => x.name)) + "\\";
                }

                return _path;
            }
        }

        public RTDBEventType EventType { get; private set; }

        public RtdbEvent Source { get; private set; }

        #endregion

        #region Constructor

        public RtdbQueryEventItem(RTDBEventType evType, RtdbEvent source)
        {
            EventType = evType;
            Source = source;

            ReadParams();
        }

        #endregion

        #region methods

        private void ReadParams()
        {
            try
            {
                Params = new List<string>();

                for (var i = 0; i <= Source.Params.Length - 1; i++)
                {
                    try
                    {
                        switch (Source.Params[i])
                        {
                            case ParamGuid guid:
                                Params.Add(guid.Value.ToString());
                                break;

                            case ParamString str:
                                Params.Add(str.Value);
                                break;

                            case ParamDateTime dt:
                                if (dt.Value.HasValue)
                                    Params.Add(PlatformInfrastructure.TimeConvert.Converter.SystemToLocal(dt.Value.Value).ToString("dd.MM.yyyy HH:mm:ss,fff"));
                                break;

                            case ParamInt64 int64:
                                Params.Add(int64.Value.ToString());
                                break;

                            case ParamFloat64 fl64:
                                Params.Add(fl64.Value.ToString());
                                break;

                            case ParamWideString dStr:
                                Params.Add(dStr.Value);
                                break;
                        }
                    }
                    catch
                    {
                        Params.Add(LocalizationManager.GetString("readParamError"));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        #endregion
    }
}

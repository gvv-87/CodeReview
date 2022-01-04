using Monitel.DataContext.Tools.Icons;
using Monitel.Mal;
using Monitel.Mal.Context.CIM16;
using Monitel.PlatformInfrastructure.Logger;
using Monitel.Rtdb.Api;
using Monitel.SCADA.Tools.DataExplorer.Models;
using Monitel.TDAL;
using Monitel.TDAL.DataStructure;
using Monitel.TDAL.Providers;
using Monitel.PlatformInfrastructure.TimeConvert;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Monitel.Localization;

namespace Monitel.SCADA.Tools.DataExplorer
{
    /// <summary>
    /// Менеджер запросов DataExplorer
    /// </summary>
    public class QueryExecuter : IDisposable
    {
        #region glob

        private IModelImage _modelImage;
        private TDAL.TDAL _tdal;
        private IRtdbConnection _rtdbConn;
        private static Dictionary<string, ImageSource> _images = new Dictionary<string, ImageSource>();
        private IconsProviderMal _iconProvider;
        private IPlatformLogger _logger;
        private Dictionary<string, byte[]> _imagesSources = new Dictionary<string, byte[]>();

        public bool IsConnected
        {
            get
            {
                return _rtdbConn != null && _rtdbConn.State == RtdbConnectionState.Online
                    && _modelImage != null;
            }
        }

        public List<ColumnItem> EventTableColumns;
        public List<ColumnItem> MeasurementTableColumns;

        #endregion

        #region constructor

        public QueryExecuter(IModelImage modelImage, IRtdbConnection rtdbConn, IPlatformLogger logger)
        {
            _modelImage = modelImage;
            _rtdbConn = rtdbConn;
            _logger = logger;

            Init();
            InitColumns();
        }

        #endregion

        #region methods

        /// <summary>
        /// Выполнить запрос
        /// </summary>
        /// <param name="query">Запрос</param>
        /// <returns>Таблица данных</returns>
        public DataTable Execute(IQueryDescriptor query)
        {
            return query.QueryItemType == QyeryTypes.Events
                ? GetEventTable(query)
                : GetMeasTable(query);
        }

        /// <summary>
        /// Выполнить запрос событий
        /// </summary>
        public IEnumerable<RtdbQueryEventItem> GetEvents(IQueryDescriptor query)
        {
            if (query == null)
                throw new ArgumentNullException("query");

            if (query.QueryItemType != QyeryTypes.Events)
                throw new Exception("Тип запроса должен быть Events");

            var ls = new List<RTDBEventType>();

            foreach (var item in query.QueryItems)
            {
                if (!Guid.TryParse(item, out Guid guid))
                    continue;

                if (_modelImage.GetObject(guid) is RTDBEventType obj)
                    ls.Add(obj);
            }

            return ls.Count != 0
            ? GetEvents(ls, query.DateQueryS, query.DateQueryPo)
            : new RtdbQueryEventItem[0];
        }

        /// <summary>
        /// Выполнить запрос измерений
        /// </summary>
        public IEnumerable<IQueryValueItem> GetMeasuarments(IQueryDescriptor query)
        {
            if (query == null)
                throw new ArgumentNullException("query");

            if (query.QueryItemType != QyeryTypes.Measurements)
                throw new Exception("Тип запроса должен быть Measurements");

            var ls = new List<MeasurementValue>();

            foreach (var item in query.QueryItems)
            {
                if (!Guid.TryParse(item, out Guid guid))
                    continue;

                if (_modelImage.GetObject(guid) is MeasurementValue mv)
                    ls.Add(mv);
            }

            if (ls.Count == 0)
                return new RtdbQueryValueItem[0];

            Dictionary<MeasurementValue, List<object>> res = null;

            switch (query.QueryTimeType)
            {
                case QueryTimeTypes.TimeNow:
                    res = GetMeasurementValuesFromRtdb(ls);
                    break;

                case QueryTimeTypes.TimeFix:
                    res = GetMeasurementValuesFromRtdb(ls, query.DateQueryS);
                    break;

                case QueryTimeTypes.Interval:
                    res = GetMeasurementValuesFromRtdb(ls, query.DateQueryS, query.DateQueryPo, query.StepType, query.WatchStep);
                    break;
            }

            var ret = new List<IQueryValueItem>();

            foreach (var lsItem in res.Values)
            {
                foreach (var item in lsItem)
                {
                    switch (item)
                    {
                        case ITDALValueString tdStr:
                            ret.Add(new RtdbQueryStringValueItem(tdStr.MeasurementValue, tdStr, GetImageForMetaType(tdStr.MeasurementValue.MetaType.Name)));
                            break;

                        case TDALValue tdVal:

                            var vi = tdVal.IsDiscret
                                ? (RtdbQueryValueItem)new RtdbQueryDiscreteValueItem(tdVal.MeasureValue, tdVal, GetImageForMetaType(tdVal.MeasureValue.MetaType.Name))
                                : (RtdbQueryValueItem)new RtdbQueryAnalogValueItem(tdVal.MeasureValue, tdVal, GetImageForMetaType(tdVal.MeasureValue.MetaType.Name));

                            ret.Add(vi);
                            break;
                    }
                }
            }

            return ret;
        }

        private void Init()
        {
            _tdal = new TDAL.TDAL(_logger);
            _tdal.AddDataProvider(new ck11rtdbProvider(null, _rtdbConn));

            _iconProvider = new IconsProviderMal() { ModelImage = _modelImage };
        }

        private void InitColumns()
        {
            EventTableColumns = new List<ColumnItem>
            {
                new ColumnItem("Id","Id",typeof(long)),
                new ColumnItem("Uid","Uid",typeof(Guid)),
                new ColumnItem("EvKey","EvKey",typeof(uint)),
                new ColumnItem("Time",LocalizationManager.GetString("prv_time"),typeof(DateTime)),
                new ColumnItem("Message",LocalizationManager.GetString("message"),typeof(string)),
                new ColumnItem("Flags",LocalizationManager.GetString("flag"),typeof(string), GetHexValue),
                new ColumnItem("UidEnObj",LocalizationManager.GetString("object"),typeof(string),GetUserName),
                new ColumnItem("Level",LocalizationManager.GetString("level"),typeof(ushort)),
                new ColumnItem("UidUser",LocalizationManager.GetString("user"),typeof(string),GetUserName),

                new ColumnItem("Params_1","Param_1",typeof(string),GetEventParam),
                new ColumnItem("Params_2","Param_2",typeof(string)),
                new ColumnItem("Params_3","Param_3",typeof(string)),
                new ColumnItem("Params_4","Param_4",typeof(string)),
                new ColumnItem("Params_5","Param_5",typeof(string)),
                new ColumnItem("Params_6","Param_6",typeof(string)),
                new ColumnItem("Params_7","Param_7",typeof(string)),
                new ColumnItem("Params_8","Param_8",typeof(string)),
            };

            MeasurementTableColumns = new List<ColumnItem>
            {
                new ColumnItem("Id","ID",typeof(long)),
                new ColumnItem("Uid","Uid",typeof(Guid)),
                new ColumnItem("ImageSource",LocalizationManager.GetString("icon"),typeof(byte[])),
                new ColumnItem("DisplayName",LocalizationManager.GetString("paramName"),typeof(string)),
                new ColumnItem("SpecificName",LocalizationManager.GetString("specificName"),typeof(string)),
                new ColumnItem("Path",LocalizationManager.GetString("pathInTree"),typeof(string)),
                new ColumnItem("QueryTime",LocalizationManager.GetString("queryTime"),typeof(DateTime)),
                new ColumnItem("Tstamp",LocalizationManager.GetString("prv_time"),typeof(DateTime)),
                new ColumnItem("Value",LocalizationManager.GetString("prv_value"),typeof(object)),
                new ColumnItem("Flags",LocalizationManager.GetString("qualityCode"),typeof(string), GetHexValue),
                new ColumnItem("Tstamp2",LocalizationManager.GetString("prv_time2"),typeof(DateTime)),
                new ColumnItem("TimeDif",LocalizationManager.GetString("difference"),typeof(double)),
                new ColumnItem("AddrInt1",LocalizationManager.GetString("intAdressOne"),typeof(int)),
                new ColumnItem("AddrInt2",LocalizationManager.GetString("intAdressTwo"),typeof(int)),
                new ColumnItem("AddrStr1",LocalizationManager.GetString("stringAdress"),typeof(string)),
            };
        }

        private DataTable GetEventTable(IQueryDescriptor query)
        {
            var items = GetEvents(query);

            var tableSource = new DataTable();

            foreach (var col in EventTableColumns)
                tableSource.Columns.Add(new DataColumn(col.FiledName, col.Type) { Caption = col.Caption });

            foreach (var item in items.ToArray())
            {
                var rw = tableSource.NewRow();

                tableSource.Rows.Add(rw);

                rw["Id"] = item.Id;
                rw["Uid"] = item.Source.Uid;
                rw["Message"] = item.Message ?? (object)DBNull.Value;
                rw["Time"] = item.Time;
                rw["UidEnObj"] = item.UidEnObj;
                rw["Flags"] = GetHexValue(null, item.Flags);
                rw["EvKey"] = item.EvKey;
                rw["Level"] = item.Level;
                rw["UidUser"] = GetUserByUid(item.UidUser);

                for (var i = 0; i < item.Params.Count; i++)
                {
                    var colName = "Param_" + i + 1;

                    if (tableSource.Columns.Contains(colName))
                        rw[colName] = item.Params[i];
                }
            }

            return tableSource;
        }

        private DataTable GetMeasTable(IQueryDescriptor query)
        {
            var items = GetMeasuarments(query);

            var tableSource = new DataTable();

            foreach (var col in MeasurementTableColumns)
                tableSource.Columns.Add(new DataColumn(col.FiledName, col.Type) { Caption = col.Caption });

            if (query.QueryTimeType == QueryTimeTypes.Interval && query.WatchStep != 0)
            {
                var groups = items.GroupBy(x => x.Id);

                foreach (var gr in groups)
                {
                    DateTime? stTime = null;

                    foreach (var val in gr.OrderBy(x => x.Tstamp))
                    {
                        if (stTime == null)
                            stTime = query.DateQueryS;
                        else
                        {
                            switch (query.StepType)
                            {
                                case StepMeasureTypes.Sec:
                                    stTime = stTime.Value.AddSeconds(query.WatchStep);
                                    break;

                                case StepMeasureTypes.Min:
                                    stTime = stTime.Value.AddMinutes(query.WatchStep);
                                    break;

                                case StepMeasureTypes.Hour:
                                    stTime = stTime.Value.AddHours(query.WatchStep);
                                    break;

                                case StepMeasureTypes.Day:
                                    stTime = stTime.Value.AddDays(query.WatchStep);
                                    break;

                                case StepMeasureTypes.Month:
                                    stTime = stTime.Value.AddMonths(query.WatchStep);
                                    break;
                            }
                        }

                        val.QueryTime = stTime;
                    }
                }
            }

            foreach (var item in items.ToArray())
            {
                var rw = tableSource.NewRow();

                tableSource.Rows.Add(rw);

                rw["Id"] = item.Id;
                rw["Uid"] = item.Uid;
                rw["ImageSource"] = ((RtdbQueryValueItem)item).ImageSource != null ? ImageSourceToBytes((RtdbQueryValueItem)item) : (object)DBNull.Value;
                rw["Path"] = item.Path ?? (object)DBNull.Value;
                rw["DisplayName"] = item.DisplayName ?? (object)DBNull.Value;
                rw["SpecificName"] = item.SpecificName ?? (object)DBNull.Value;

                if (query.QueryTimeType == QueryTimeTypes.Interval)
                    rw["QueryTime"] = item.QueryTime ?? (object)DBNull.Value;

                rw["Tstamp"] = item.Tstamp ?? (object)DBNull.Value;
                rw["Tstamp2"] = item.Tstamp2 ?? (object)DBNull.Value;
                rw["Value"] = item.Value;
                rw["Flags"] = GetHexValue(null, item.Flags);
                rw["TimeDif"] = item.TimeDif;

                if (item is RtdbQueryAnalogValueItem rav)
                {
                    rw["AddrInt1"] = rav.AddrInt1;
                    rw["AddrInt2"] = rav.AddrInt2;
                    rw["AddrStr1"] = rav.AddrStr1;
                }
                else if (item is RtdbQueryDiscreteValueItem rdv)
                {
                    rw["AddrInt1"] = rdv.AddrInt1;
                    rw["AddrInt2"] = rdv.AddrInt2;
                    rw["AddrStr1"] = rdv.AddrStr1;
                }               
            }

            return tableSource;
        }

        private string GetUserByUid(Guid uid)
        {
            return _modelImage.GetObject(uid) is Person pers
                ? pers.name
                : String.Empty;
        }

        public byte[] ImageSourceToBytes(RtdbQueryValueItem item)
        {
            if (_imagesSources.ContainsKey(item.MetaTypeName))
                return _imagesSources[item.MetaTypeName];

            byte[] bytes = null;

            if (item.ImageSource is BitmapSource bitmapSource)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    bytes = stream.ToArray();
                }
            }

            _imagesSources.Add(item.MetaTypeName, bytes);

            return bytes;
        }

        private ImageSource GetImageForMetaType(string mtName)
        {
            if (_iconProvider == null)
                return null;

            if (!_images.ContainsKey(mtName))
            {
                var mc = _modelImage.MetaData.Classes.FirstOrDefault(x => String.Equals(x.Name, mtName));

                if (mc != null)
                {
                    var data = _iconProvider.GetClassIcon(mc.Id);

                    return _images[mtName] = ReadImage(data);
                }
            }
            else
                return _images[mtName];

            return null;
        }

        internal ImageSource ReadImage(Stream stream)
        {
            BitmapImage bmp = null;
            if (stream != null)
            {
                bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = stream;
                bmp.EndInit();
            }

            ImageSource img = bmp;

            if (img != null && img.CanFreeze)
                img.Freeze();

            return img;
        }

        internal ImageSource ReadImage(byte[] bytes)
        {
            return ReadImage(bytes != null ? new MemoryStream(bytes) : null);
        }

        #region Queries

        private void SortByType(IEnumerable<MeasurementValue> vals, out List<MeasurementValue> strMeas, out List<MeasurementValue> numMeas)
        {
            strMeas = new List<MeasurementValue>();
            numMeas = new List<MeasurementValue>();

            foreach (var item in vals)
            {
                if (item is StringMeasurementValue)
                    strMeas.Add(item);
                else
                    numMeas.Add(item);
            }
        }

        /// <summary>
        /// Получить список значений измерений из БДРВ для заданных измерений за тукущее время
        /// </summary>
        private Dictionary<MeasurementValue, List<object>> GetMeasurementValuesFromRtdb(IEnumerable<MeasurementValue> vals)
        {
            var ret = new Dictionary<MeasurementValue, List<object>>();

            if (!IsConnected)
                return ret;

            try
            {
                SortByType(vals, out List<MeasurementValue> strMeas, out List<MeasurementValue> numMeas);

                if (strMeas.Any())
                {
                    var syncRead = new GetTrackerGeneric(_logger, new TDALGetQueryParams
                    {
                        DtFrom = new DatePoint(),
                        Measures = strMeas
                    });

                    _tdal.Get(syncRead);
                    syncRead.WaitCompletion();

                    foreach (var item in syncRead.Result)
                    {
                        if (!ret.ContainsKey(item.MeasurementValue))
                            ret[item.MeasurementValue] = new List<object>() { item };
                        else
                            ret[item.MeasurementValue].Add(item);
                    }
                }

                if (numMeas.Any())
                {
                    foreach (var item in _tdal.GetDataSync(numMeas, new DatePoint()))
                    {
                        var ls = new List<object>();
                        ret[item.Key] = ls;

                        foreach (var vl in item.Value)
                            ls.Add(vl);
                    }
                }

                return ret;
            }
            catch (Exception ex)
            {
                _logger.Write(LogCategory.Error, LogPriority.Highest, ex.ToString());
            }

            return ret;
        }

        /// <summary>
        /// Получить список значений измерений из БДРВ для заданных измерений за указанное время
        /// </summary>
        private Dictionary<MeasurementValue, List<object>> GetMeasurementValuesFromRtdb(IEnumerable<MeasurementValue> vals, DateTime anch)
        {
            var ret = new Dictionary<MeasurementValue, List<object>>();

            if (!IsConnected)
                return ret;

            anch = anch.AddTicks(-(anch.Ticks % TimeSpan.TicksPerSecond));

            try
            {
                SortByType(vals, out List<MeasurementValue> strMeas, out List<MeasurementValue> numMeas);

                var dp = new DatePoint(timeAnchor: anch.LocalToSystem());

                if (strMeas.Any())
                {
                    var syncRead = new GetTrackerGeneric(_logger, new TDALGetQueryParams
                    {
                        DtFrom = dp,
                        Measures = strMeas
                    });

                    _tdal.Get(syncRead);
                    syncRead.WaitCompletion();

                    foreach (var item in syncRead.Result)
                    {
                        if (!ret.ContainsKey(item.MeasurementValue))
                            ret[item.MeasurementValue] = new List<object>() { item };
                        else
                            ret[item.MeasurementValue].Add(item);
                    }
                }

                if (numMeas.Any())
                {
                    foreach (var item in _tdal.GetDataSync(numMeas, dp))
                    {
                        var ls = new List<object>();
                        ret[item.Key] = ls;

                        foreach (var vl in item.Value)
                            ls.Add(vl);
                    }
                }

                return ret;
            }
            catch (Exception ex)
            {
                _logger.Write(LogCategory.Error, LogPriority.Highest, ex.ToString());
            }

            return ret;
        }

        /// <summary>
        /// Получить список значений измерений из БДРВ для заданных измерений за указанный интервал времени
        /// </summary>
        private Dictionary<MeasurementValue, List<object>> GetMeasurementValuesFromRtdb(IEnumerable<MeasurementValue> vals, DateTime anch, DateTime interv, StepMeasureTypes stepType, byte step)
        {
            var ret = new Dictionary<MeasurementValue, List<object>>();

            if (!IsConnected)
                return ret;

            anch = anch.AddTicks(-(anch.Ticks % TimeSpan.TicksPerSecond));
            interv = interv.AddTicks(-(interv.Ticks % TimeSpan.TicksPerSecond));

            var sub = interv.Subtract(anch);

            try
            {
                var stepTm = GetStepFromValue(step, stepType);

                var dtFrom = new DatePoint(timeAnchor: anch.LocalToSystem());
                var dtTo = new DatePoint(timeAnchor: interv.LocalToSystem());

                SortByType(vals, out List<MeasurementValue> strMeas, out List<MeasurementValue> numMeas);

                if (strMeas.Any())
                {
                    var syncRead = new GetTrackerGeneric(_logger, new TDALGetQueryParams
                    {
                        DtFrom = dtFrom,
                        DtTo = dtTo,
                        Step = stepTm,
                        Measures = strMeas
                    });

                    _tdal.Get(syncRead);
                    syncRead.WaitCompletion();

                    foreach (var item in syncRead.Result)
                    {
                        if (!ret.ContainsKey(item.MeasurementValue))
                            ret[item.MeasurementValue] = new List<object>() { item };
                        else
                            ret[item.MeasurementValue].Add(item);
                    }
                }

                if (numMeas.Any())
                {
                    foreach (var item in _tdal.GetDataSync(numMeas, dtFrom, dtTo, stepTm))
                    {
                        var ls = new List<object>();
                        ret[item.Key] = ls;

                        foreach (var vl in item.Value)
                            ls.Add(vl);
                    }
                }

                return ret;
            }
            catch
            { }

            return ret;
        }

        public static TimeSpan GetStepFromValue(byte step, StepMeasureTypes stepType)
        {
            var stepTm = TimeSpan.Zero;

            if (step != 0)
            {
                switch (stepType)
                {
                    case StepMeasureTypes.Sec:
                        stepTm = TimeSpan.FromSeconds(step);
                        break;
                    case StepMeasureTypes.Min:
                        stepTm = TimeSpan.FromMinutes(step);
                        break;
                    case StepMeasureTypes.Hour:
                        stepTm = TimeSpan.FromHours(step);
                        break;
                    case StepMeasureTypes.Day:
                        stepTm = TimeSpan.FromDays(step);
                        break;
                    case StepMeasureTypes.Month:
                        stepTm = TimeSpan.FromDays(step * 30);
                        break;
                }
            }

            return stepTm;
        }

        /// <summary>
        /// Запросить измерения за указанный интервал
        /// </summary>
        private IEnumerable<RtdbQueryEventItem> GetEvents(IEnumerable<RTDBEventType> evs, DateTime ts, DateTime ts2)
        {
            var list = new List<RtdbQueryEventItem>();

            if (!IsConnected)
                return list;

            ts = ts.AddTicks(-(ts.Ticks % TimeSpan.TicksPerSecond));
            ts2 = ts2.AddTicks(-(ts2.Ticks % TimeSpan.TicksPerSecond));

            var _evs = evs.ToDictionary(p => p.Uid);

            var request = new Rtdb.Api.Requests.EventsIntervalReadRequest(evs.Select(x => x.Uid).ToArray(), Guid.Empty, Guid.Empty, ts.LocalToSystem(), ts2 != null ? ts2.LocalToSystem() : (DateTime?)null);

            var tmp = _rtdbConn.SendRequest(request);

            var res = tmp.WaitAll();

            if (!res.Error.IsNone)
                throw new Exception(res.Error.Type.ToString());

            foreach (var oik_event in tmp.WaitAllResponses())
            {
                if (oik_event.HasErrors && oik_event.ErrorResults.Any())
                    throw new Exception(String.Join(Environment.NewLine, oik_event.ErrorResults.Select(x => x.Error.ToString())));

                list.Add(new RtdbQueryEventItem(_evs[oik_event.Event.Uid], oik_event.Event));
            }

            return list;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Экспортировать запрос в xml
        /// </summary>
        public static XElement GetXml(IQueryDescriptor item)
        {
            var root = new XElement("QueryItem");
            root.Add(new XAttribute("QueryItemType", item.QueryItemType));
            root.Add(new XAttribute("DateQueryS", item.DateQueryS));
            root.Add(new XAttribute("DateQueryPo", item.DateQueryPo));
            root.Add(new XAttribute("StepType", item.StepType));
            root.Add(new XAttribute("QueryTimeType", item.QueryTimeType));
            root.Add(new XAttribute("WatchStep", item.WatchStep));

            var measList = new XElement("QueryItems");
            root.Add(measList);

            foreach (var meas in item.QueryItems)
                measList.Add(new XElement("Item", meas));

            return root;
        }

        /// <summary>
        /// Импортировать запрос из XML
        /// </summary>
        public static QueryDescriptor GetQuery(XElement item)
        {
            var ds = new QueryDescriptor();

            if (!String.Equals(item.Name.LocalName, "QueryItem"))
                throw new Exception("Неверный формат запроса");

            var attr = item.Attributes("QueryItemType").FirstOrDefault();

            if (attr != null)
            {
                Enum.TryParse(attr.Value, out QyeryTypes qType);
                ds.QueryItemType = qType;
            }

            attr = item.Attributes("DateQueryS").FirstOrDefault();

            if (attr != null)
            {
                DateTime.TryParse(attr.Value, out DateTime time);
                ds.DateQueryS = time;
            }

            attr = item.Attributes("DateQueryPo").FirstOrDefault();

            if (attr != null)
            {
                DateTime.TryParse(attr.Value, out DateTime time);
                ds.DateQueryPo = time;
            }

            attr = item.Attributes("StepType").FirstOrDefault();

            if (attr != null)
            {
                Enum.TryParse(attr.Value, out StepMeasureTypes sType);
                ds.StepType = sType;
            }

            attr = item.Attributes("QueryTimeType").FirstOrDefault();

            if (attr != null)
            {
                Enum.TryParse(attr.Value, out QueryTimeTypes sType);
                ds.QueryTimeType = sType;
            }

            attr = item.Attributes("WatchStep").FirstOrDefault();

            if (attr != null)
            {
                if (byte.TryParse(attr.Value, out byte sType))
                    ds.WatchStep = sType;
            }

            var items = item.Elements("QueryItems").FirstOrDefault();

            if (items != null)
            {
                ds.QueryItems = items.Elements("Item")
                    .Where(x => !String.IsNullOrEmpty(x.Value))
                    .Select(x => x.Value)
                    .ToArray();
            }

            return ds;
        }

        #endregion

        #region column transforms

        private object GetEventParam(ColumnItem arg1, object arg2)
        {
            return null;
        }

        private object GetUserName(ColumnItem arg1, object arg2)
        {
            return arg2 is Guid guid
                ? GetUserByUid(guid)
                : arg2?.ToString();
        }

        private static object GetHexValue(ColumnItem arg1, object arg2)
        {
            if (arg2 != null)
            {
                long.TryParse(arg2.ToString(), out long val);

                var tmpV = val.ToString("X");

                var n = 8 - tmpV.Length;

                if (n > 0)
                    tmpV = new String('0', n) + tmpV;

                return String.Format("0x{0}", tmpV);
            }

            return String.Empty;
        }

        #endregion

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _tdal?.Dispose();
            _iconProvider?.Dispose();
        }

        #endregion
    }

    public class ColumnItem
    {
        private Func<ColumnItem, object, object> _transfor;

        public string FiledName { get; set; }
        public string Caption { get; set; }
        public Type Type { get; set; }

        public ColumnItem(string name, string caption, Type type, Func<ColumnItem, object, object> transfor = null)
        {
            FiledName = name;
            Caption = caption;
            Type = type;
            _transfor = transfor;
        }

        public object GetFieldValue(object obj)
        {
            var val = obj.GetType().GetProperty(FiledName).GetValue(obj, System.Reflection.BindingFlags.Public, null, null, null);

            return _transfor != null
                ? _transfor(this, val)
                : val;
        }
    }
}

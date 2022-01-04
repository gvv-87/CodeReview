using Monitel.Mal;
using System;
using System.Collections.Generic;

namespace Monitel.SCADA.Tools.DataExplorer.Models
{
    /// <summary>
    /// Структура хранимого запроса
    /// </summary>
    public interface IQueryDescriptor
    {
        #region Properties

        /// <summary>
        /// Тип запрашиваемых объектов
        /// </summary>
        QyeryTypes QueryItemType { get; set; }

        /// <summary>
        /// Запрос значений с
        /// </summary>
        DateTime DateQueryS { get; set; }

        /// <summary>
        /// запрос значений по
        /// </summary>
        DateTime DateQueryPo { get; set; }

        /// <summary>
        /// Тип шага
        /// </summary>
        StepMeasureTypes StepType { get; set; }

        /// <summary>
        /// Тип запроса
        /// </summary>
        QueryTimeTypes QueryTimeType { get; set; }

        /// <summary>
        /// Интервал шага
        /// </summary>
        byte WatchStep { get; set; }

        /// <summary>
        /// Наименование запроса
        /// </summary>
        string QueryName { get; set; }

        /// <summary>
        /// Список измерений
        /// </summary>
        string[] QueryItems { get; set; }

        #endregion
    }

    public enum QyeryTypes { Measurements, Events }

    /// <summary>
    /// Тип запроса
    /// </summary>
    public enum QueryTimeTypes { TimeNow, TimeFix, Interval, Incoming, Writen }

    /// <summary>
    /// Шаг, для запроса в интервале
    /// </summary>
    public enum StepMeasureTypes { Sec, Min, Hour, Day, Month }
}

using Monitel.SCADA.Tools.DataExplorer.Models;
using Monitel.Settings.Interfaces;
using System;
using System.Collections.Generic;

namespace Monitel.SCADA.UICommon.DiagramsSet
{
    /// <summary>
    /// Набор измерений
    /// </summary>
    public class Diagram
    {
        #region glob

        private IEnumerable<string> _measurementInfoValues;
        private List<string> _extensions;

        [NonSerialized]
        internal Dictionary<string, Action<DSetStore, ISettingsGroup>> _extDick = new Dictionary<string, Action<DSetStore, ISettingsGroup>>();

        #endregion

        #region Properties

        /// <summary>
        /// Видимость набора
        /// </summary>
        public AccessLayer AccessLayer { get; set; }

        /// <summary>
        /// Путь в дереве
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Уникальный идентификатор
        /// </summary>
        public string UID { get; private set; }

        /// <summary>
        /// Ниманование набора
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Запрос значений с
        /// </summary>
        public DateTime DatePoint1 { get; set; }

        /// <summary>
        /// запрос значений по
        /// </summary>
        public DateTime DatePoint2 { get; set; }

        /// <summary>
        /// Тип шага
        /// </summary>
        public StepMeasureTypes StepType { get; set; }

        /// <summary>
        /// Интервал шага
        /// </summary>
        public byte WatchStep { get; set; }

        /// <summary>
        /// Список измерений
        /// </summary>
        public IEnumerable<string> MeasurementInfoValues
        {
            get
            {
                if (_measurementInfoValues == null)
                    _measurementInfoValues = new string[0];

                return _measurementInfoValues;
            }
            set
            {
                _measurementInfoValues = value;
            }
        }

        /// <summary>
        /// Список дополнительных расширений
        /// </summary>
        public List<string> Extensions
        {
            get
            {
                if (_extensions == null)
                    _extensions = new List<string>();

                return _extensions;
            }
            private set
            {
                _extensions = value;
            }
        }

        #endregion

        #region constructor

        public Diagram()
        {
            UID = Guid.NewGuid().ToString();
            Extensions = new List<string>();
        }

        #endregion
    }

    /// <summary>
    /// Уровень доступности набора
    /// </summary>
    public enum AccessLayer
    {
        /// <summary>
        /// Доступен всем
        /// </summary>
        Common,

        /// <summary>
        /// Доступен владельцу
        /// </summary>
        User,

        /// <summary>
        /// Не используется
        /// </summary>
        Root
    }
}

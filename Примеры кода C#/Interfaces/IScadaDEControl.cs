using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Monitel.Mal.Context.CIM16;

namespace Monitel.SCADA.UICommon.Interfaces
{
    /// <summary>
    /// Определяет методы для обмена данными (Data Exchange) между компонентом SCADA
    /// и приложением, использующим его.
    /// </summary>
    public interface IScadaDEControl : IScadaUIControl
    {
        /// <summary>
        /// Установка значения измерения, в которое необходимо произвести ручной ввод
        /// </summary>
        /// <param name="_measValue">Значение измерения для осуществления ручного ввода</param>
        void SetMeasurementValues(IEnumerable<MeasurementValue> _measValues);

        /// <summary>
        /// Обновляет значения измерений
        /// </summary>
        /// <param name="_changedValues">Изменившиеся значения измерений</param>
        void UpdateMeasurementValues(IEnumerable<MeasurementValue> _changedValues);
        
        /// <summary>
        /// Происходит, когда необходимо добавить значения измерений для отслеживания
        /// </summary>
        event ChangeMVEventHandler AppendMonitoringValues;

        /// <summary>
        /// Происходит, когда необходимо перестать отслеживать значения измерений
        /// </summary>
        event ChangeMVEventHandler RemoveMonitoringValues;
    }

    public class ChangeMVEventArgs : EventArgs
    {
        public ChangeMVEventArgs(IEnumerable<MeasurementValue> changedValues)
        {
            MeasurementValues = changedValues;
        }
        /// <summary>
        /// Значения измерений
        /// </summary>
        public IEnumerable<MeasurementValue> MeasurementValues { get; private set; }
    }

    public delegate void ChangeMVEventHandler(object sender, ChangeMVEventArgs e);
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitel.SCADA.UICommon.Documents
{
    /// <summary>
    /// Интерфейс для реализации окна настройки документов
    /// </summary>
    public interface IDocumentsSetting
    {
        /// <summary>
        /// Метод, предназначенный для отображения окна настроек документа
        /// </summary>
        public void Show();

        /// <summary>
        /// Флаг, указывающий для скрытия меню "Настройки"
        /// Если false, то скрываем
        /// </summary>
        public bool IsNeedShowDialogSetting { get; set; }
    }
}

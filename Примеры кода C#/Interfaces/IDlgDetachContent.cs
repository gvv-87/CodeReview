using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Monitel.SCADA.UICommon.Interfaces
{
    /// <summary>
    /// Интерфейс для реализации возможности отделения контента в отдельное немодальное окно
    /// </summary>
    public interface IDlgDetachContent
    {
        /// <summary>
        /// событие необходимости отделения контента
        /// </summary>
        event EventHandler<DlgDetachContentEventArgs> DetachContent;
    }

    /// <summary>
    /// Вид источника для браузера
    /// </summary>
    public enum WebSourceKind
    {
        Uri,
        Html
    }

    /// <summary>
    /// Аргументы события отделения контента в отдельное окно
    /// </summary>
    public class DlgDetachContentEventArgs : EventArgs
    {
        /// <summary>
        /// Заголовок окна
        /// </summary>
        public string Title { get;  }
        /// <summary>
        /// Вид источника
        /// </summary>
        public WebSourceKind SourceKind { get; }
        /// <summary>
        /// Источник браузера (Uri or Html)
        /// </summary>
        public string Source { get; }

        public DlgDetachContentEventArgs(string title, WebSourceKind sourceKind, string source)
        {
            Title = title;
            SourceKind = sourceKind;
            Source = source;
        }
    }


}

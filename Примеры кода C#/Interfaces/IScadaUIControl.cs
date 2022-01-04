using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Monitel.Jal.Client;
using Monitel.UI.Infrastructure.Services;
using Monitel.Rtdb.Api;

namespace Monitel.SCADA.UICommon.Interfaces
{
    /// <summary>
    /// Базовый интерфейс общих компонентов пользовательского интерфейса для SCADA.
    /// Определяет методы для работы с общими компонентами SCADA
    /// </summary>
    public interface IScadaUIControl : IDisposable
    {
        /// <summary>
        /// Инициализация элемента управления
        /// </summary>
        /// <param name="_serviceManager">Менеджер служб</param>
        /// <param name="_journalData">Менеджер подключения к сервису JAL</param>
        /// <param name="_rtdbConnection">Соединение с БДРВ</param>
        void Init(IServiceManager _serviceManager, IJournalProvider _jalProvider = null, IRtdbConnection _rtdbConnection = null, Monitel.TDAL.TDAL _tdalProvider = null);
    }
}

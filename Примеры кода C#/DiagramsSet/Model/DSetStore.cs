using Monitel.Settings.Interfaces;
using System;
using System.Collections.Generic;

namespace Monitel.SCADA.UICommon.DiagramsSet
{
    /// <summary>
    /// Класс для работы с наборами
    /// </summary>
    public class DSetStore
    {
        #region glob

        private ISettingsManager _sm;
        private static string SETTINGS_GROUP = "DiagramsSet";
        private static string DIAGRAM_MD = "Diagram";
        private static string EXTENSION_MD = "Extension";

        #endregion

        #region constructor

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="settingsManager">Менеджер доступа к БД Settings</param>
        public DSetStore(ISettingsManager settingsManager)
        {
            _sm = settingsManager;
        }

        #endregion

        /// <summary>
        /// Запросить набор с указанным Uid
        /// </summary>
        /// <param name="uid">Uid набора</param>
        /// <returns>в случае успеха объект Diagram иначе null</returns>
        internal T GetExtension<T>(Diagram dg, string uid)
        {
            ISettingsGroup group = null;

            if (dg.AccessLayer == AccessLayer.Common)
                group = _sm.GetAllUsersGroup(SETTINGS_GROUP, "server");
            else
                group = _sm.GetUserGroup(SETTINGS_GROUP, "server");

            T res = default(T);

            if (group != null)
                group.TryGet<T>(uid, out res, EXTENSION_MD);

            return res;
        }

        /// <summary>
        /// Есть ли набор с заданным uid
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool IsDiagramExist(string uid)
        {
            return GetDiagram(uid) != null;
        }

        /// <summary>
        /// Получить набор по заданному uid
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public Diagram GetDiagram(string uid)
        {
            Diagram res = null;

            if (_sm.GetAllUsersGroup(SETTINGS_GROUP, "server")?.TryGet<Diagram>(uid, out res, DIAGRAM_MD) != true)
                _sm.GetUserGroup(SETTINGS_GROUP, "server")?.TryGet<Diagram>(uid, out res, DIAGRAM_MD);

            return res;
        }

        /// <summary>
        /// Получить список всех наборов
        /// </summary>
        /// <param name="ac">Тип доступа</param>
        public IEnumerable<Diagram> GetDiagrams(AccessLayer ac = AccessLayer.Common)
        {
            var res = new List<Diagram>();
            Diagram tmp = null;

            if (ac == AccessLayer.Common)
            {
                var _commonGroup = _sm.GetAllUsersGroup(SETTINGS_GROUP, "server");

                if (_commonGroup != null)
                    foreach (var item in _commonGroup.GetAll())
                    {
                        if (item.TryGet<Diagram>(out tmp) && item.Modificator1 == DIAGRAM_MD)
                            res.Add(tmp);
                    }
            }
            else
            {
                var _userGroup = _sm.GetUserGroup(SETTINGS_GROUP, "server");

                if (_userGroup != null)
                    foreach (var item in _userGroup.GetAll())
                    {
                        if (item.TryGet<Diagram>(out tmp) && item.Modificator1 == DIAGRAM_MD)
                            res.Add(tmp);
                    }
            }

            return res;
        }

        /// <summary>
        /// Запросить дополнительные опции
        /// </summary>
        /// <typeparam name="T">Тип настройки</typeparam>
        /// <returns>Вернет настройку или null если такой настрйоки нет</returns>
        public T GetExtension<T>(Diagram obj)
        {
            var key = obj.UID + "_" + typeof(T).Name;

            var ext = GetExtension<T>(obj, key);

            if (ext != null)
                AddExtension<T>(obj, ext);

            return ext;
        }

        /// <summary>
        /// Добавить дополнительные опции в набор
        /// </summary>
        /// <typeparam name="T">Тип опции</typeparam>
        /// <param name="ext">Опция</param>
        public void AddExtension<T>(Diagram obj, T ext)
        {
            var key = obj.UID + "_" + typeof(T).Name;

            Action<DSetStore, ISettingsGroup> ac = (st, gr) =>
            {
                st.SaveExt<T>(gr, obj, key, ext);
            };

            if (!obj.Extensions.Contains(key))
                obj.Extensions.Add(key);

            if (!obj._extDick.ContainsKey(key))
                obj._extDick.Add(key, ac);
            else
                obj._extDick[key] = ac;
        }

        /// <summary>
        /// Сохранить набор в БД
        /// </summary>
        public void Save(Diagram obj)
        {
            var group = obj.AccessLayer == AccessLayer.Common
                ? _sm.GetAllUsersGroup(SETTINGS_GROUP, "server")
                : _sm.GetUserGroup(SETTINGS_GROUP, "server");

            if (group != null)
            {
                group.BeginUpdate();
                group.Set<Diagram>(obj.UID, obj, DIAGRAM_MD);

                foreach (var val in obj._extDick.Values)
                    val(this, group);

                group.EndUpdate();
            }
        }

        internal void SaveExt<T>(ISettingsGroup group, Diagram dg, string name, T obj)
        {
            group.Set<T>(name, obj, EXTENSION_MD);
        }

        /// <summary>
        /// Удалить набор из БД
        /// </summary>
        public void Remove(Diagram item)
        {
            var group = item.AccessLayer == AccessLayer.Common
              ? _sm.GetAllUsersGroup(SETTINGS_GROUP, "server")
              : _sm.GetUserGroup(SETTINGS_GROUP, "server");

            if (group == null)
                return;

            Settings.Setting st = null;

            if (group.TryGet(item.UID, out st, DIAGRAM_MD))
            {
                group.BeginUpdate();
                group.Remove(item.UID, DIAGRAM_MD);

                foreach (var ext in item.Extensions)
                    group.Remove(ext, EXTENSION_MD);

                group.EndUpdate();
            }
        }
    }
}

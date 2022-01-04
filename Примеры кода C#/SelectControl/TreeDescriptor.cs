using System;
using System.Collections.Generic;
using System.Linq;
using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.DataContext.Tools.TreeProviders.Base;
using Monitel.DataContext.Tools.TreeProviders.Interfaces;
using Monitel.Localization;
using Monitel.Mal;

namespace Monitel.SCADA.UICommon.SelectControl
{
    public class TreeDescriptor : ITreeDescriptorHandle
    {
        private readonly ModelObjectViewModel _viewModel;
        private BaseTreeIcons _icons;
        private List<ITreeItem> _roots;

        internal TreeDescriptor(ModelObjectViewModel viewModel, ITreeDataProvider provider)
        {
            _viewModel = viewModel;
            Provider = provider;
        }

        private ITreeItem FindTreeItem(IMalObject malObject)
        {
            if (malObject == null) return null;
            //получить путь
            List<IMalObject> path = malObject.GetPath();
            if (path == null) return null;
            IEnumerable<ITreeItem> items = null;
            ITreeItem result = null;
            //срез модели может не содержать верхней части (до BaseObjectRoot)
            //надо убрать часть пути, который отсутствует в срезе модели
            IMalObject rootMalObject = _viewModel.ModelItems.Values.First().MalObject;
            while (path.Any())
                if (path.First() != rootMalObject) path.RemoveAt(0);
                else break;
            if (path.Any()) path.RemoveAt(0);
            foreach (IMalObject moParent in path)
            {
                if (items == null) items = Roots;
                else items = result.Children;
                result = FindTreeItemIn(moParent, items);
                if (result == null)
                {
                    _viewModel.ServiceManager.Journal.WriteMessage(UI.Infrastructure.Services.JournalMessageType.Warning, 0,
                        "SelectControl TreeDescriptor", null, string.Format("[SelectControl TreeDescriptor] {2} {0} ({1})",
                        moParent.GetName(), moParent.Uid, LocalizationManager.GetString("noFoundITreeItemForObject")), null);
                    break;
                }
            }
            return result;
        }

        private ITreeItem FindTreeItemIn(IMalObject malObject, IEnumerable<ITreeItem> items)
        {
            ITreeItem result = null;
            foreach (ITreeItem item in items)
            {
                if (item.LinkedObject.Id == malObject.Id) result = item;
                if (result != null) break;
            }
            return result;
        }

        #region ITreeDescriptorHandle

        public ITreeIcons Icons
        {
            get
            {
                if (_icons == null)
                {
                    _icons = new BaseTreeIcons();
                    _icons.SetDataSources(_viewModel.ServiceManager.ClassIcons.Provider);
                }
                return _icons;
            }
        }

        public string Id => "FilterTree";

        public bool IsEditingAllowed => false;

        public bool IsSorted => false;

        public string ModelName => _viewModel.ServiceManager.DataSource.MainModelImage.MetaData.ContextName;

        public ITreeDataProvider Provider { get; }

        public IEnumerable<ITreeItem> Roots
        {
            get
            {
                if (_roots == null)
                {
                    _roots = new List<ITreeItem>();
                    if (_viewModel.ModelItems.Any())
                        foreach (ModelItem item in _viewModel.ModelItems.First().Value.Items.Values)
                            _roots.Add(new TreeItem(item, null));
                }
                return _roots;
            }
        }

        public string Title => "FilterTree";

        public string GroupTitle => Provider.Title;

        public void Activate(Action<TreeItemChangesArgs> treeItemChangesHandler, Action reloadedHandler)
        {

        }

        public void Clear()
        {
            if (_roots != null)
            {
                _roots.Clear();
                _roots = null;
            }
        }

        public void Deactivate()
        {
            Clear();
        }

        public IEnumerable<ITreeItem> FindItems(long linkedObjectId)
        {
            ITreeItem item = FindTreeItem(_viewModel.ServiceManager.DataSource.MainModelImage.GetObject(linkedObjectId));
            if (item != null)
                return new List<ITreeItem>() { item };
            else
                return Enumerable.Empty<ITreeItem>();
        }

        public IDictionary<long, IEnumerable<ITreeItem>> FindItems(IEnumerable<long> linkedObjectIds)
        {
            return linkedObjectIds.Distinct().ToDictionary(p => p, p => FindItems(p));
        }

        #endregion
    }
}

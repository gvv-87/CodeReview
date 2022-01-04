using System.Collections.Generic;
using System.Linq;
using Monitel.DataContext.Tools;
using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.DataContext.Tools.TreeProviders;
using Monitel.DataContext.Tools.TreeProviders.Interfaces;
using Monitel.Mal;

namespace Monitel.SCADA.UICommon.SelectControl
{
    public class TreeItem : ITreeItem
    {
        private ModelItem _item;
        private List<ITreeItem> childrens = null;

        internal IMalObject FolderNode { get; private set; }

        internal TreeItem(ModelItem item, TreeItem parent)
        {
            _item = item;
            LinkedObject = new LinkedObjectMal(_item.MalObject);
            Parent = parent;
            IsInFilter = item.IsInFilter;
            if (_item.MalObject != null && _item.MalObject.MetaType.Name == MetaNames.CLASS_FOLDER)
                FolderNode = _item.MalObject.GetByAssoc1(MetaNames.PROPERTY_FOLDER_CREATINGNODE);
        }

        #region ITreeItem

        public IEnumerable<ITreeItem> Children
        {
            get
            {
                if (childrens == null)
                {
                    childrens = new List<ITreeItem>(_item.Items.Count);
                    if (_item.Items.Any())
                        foreach (ModelItem item in _item.Items.Values)
                            childrens.Add(new TreeItem(item, this));
                }
                return childrens;
            }
        }

        public bool HasChildren
        {
            get { return !_item.IsLeaf; }
        }

        public bool HasIcon
        {
            get { return _item.Image != null; }
        }

        public long IconId
        {
            get
            {
                return _item.ViewModel.ServiceManager.ClassIcons.Provider.GetObjectIconKey(_item.MalObject.Id, _item.MalObject.MetaType.Id);
            }
        }

        public bool IsEditingAllowed
        {
            get { return false; }
        }

        public bool IsFolder
        {
            get { return _item.IsFolder; }
        }

        public bool IsInFilter { get; private set; }

        public ILinkedObject LinkedObject
        {
            get; private set;
        }

        public ITreeItem Parent
        {
            get; private set;
        }

        public string Title
        {
            get { return _item.MalObject != null ? _item.MalObject.GetName() : ""; }
        }

        public bool CanBeChildOf(ITreeItem parent)
        {
            return false;
        }

        public void SetParent(ITreeItem parent)
        {

        }

        #endregion
    }
}

using Monitel.Localization;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Monitel.SCADA.UICommon.DiagramsSet
{
    /// <summary>
    /// Логический элемент дерева наборов "папка"
    /// </summary>
    public class FolderItem : IDiagramItem, INotifyPropertyChanged
    {
        #region glob

        private string _name;
        public DSetStore _store { get; set; }
        private AccessLayer _acLayer;
        private bool _isExpanded;
        private bool _isSelected;

        #endregion

        #region Properties

        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
                DoPropertyChanged("IsExpanded");

                if (_isExpanded && Parent != null)
                    Parent.IsExpanded = true;
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                DoPropertyChanged("IsSelected");
            }
        }

        public AccessLayer AccessLayer
        {
            get
            {
                return Parent != null ? Parent.AccessLayer : _acLayer;
            }
            set
            {
                _acLayer = value;
            }
        }

        public bool IsRoot { get; set; }

        public string Path { get; set; }

        public string UID { get; set; }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    if (_name != null && value != null)
                        NameChanged(_name, value);

                    _name = value;
                }
            }
        }

        public FolderItem Parent { get; set; }

        public ObservableCollection<IDiagramItem> Items { get; private set; }

        #endregion

        #region constructor

        public FolderItem(string name, DSetStore store)
        {
            Items = new ObservableCollection<IDiagramItem>();
            _store = store;
            Name = String.IsNullOrEmpty(name) ? Name = LocalizationManager.GetString("newFolder") : name;
        }

        #endregion

        #region Methods

        private void NameChanged(string oldName, string newName)
        {
            if (_store == null || IsRoot)
                return;

            var all = _store.GetDiagrams(AccessLayer);

            var pathOld = String.IsNullOrEmpty(Path)
                ? oldName
                : Path + "\\" + oldName;

            var pathNew = String.IsNullOrEmpty(Path)
                ? newName
                : Path + "\\" + newName;

            foreach (var item in all.Where(x => !String.IsNullOrEmpty(x.Path) && x.Path.StartsWith(pathOld)))
            {
                item.Path = pathNew + item.Path.Remove(0, pathOld.Length);
                _store.Save(item);
            }
        }

        public void Remove()
        {
            foreach (var item in Items.ToArray())
                item.Remove();

            if (Parent != null)
                Parent.Items.Remove(this);
        }

        public void AddItem(IDiagramItem item)
        {
            Items.Add(item);
            item.Parent = this;
            item.AccessLayer = AccessLayer;

            if (!IsRoot)
                item.Path = String.IsNullOrEmpty(Path)
                    ? String.Format("{0}", Name)
                    : String.Format("{0}\\{1}", Path, Name);

            IsExpanded = true;
        }

        public DiagramItem AddItem(Diagram item)
        {
            var newItem = new DiagramItem(_store, item);

            AddItem(newItem);

            return newItem;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void DoPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

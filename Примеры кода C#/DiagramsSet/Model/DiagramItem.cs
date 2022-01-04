
using System.ComponentModel;

namespace Monitel.SCADA.UICommon.DiagramsSet
{
    /// <summary>
    /// Въюшка для набора
    /// </summary>
    public class DiagramItem : IDiagramItem, INotifyPropertyChanged
    {
        #region glob

        private DSetStore _store;
        private Diagram _dg;
        private bool _isSelected;

        #endregion

        #region Properties
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

                if (_isSelected && Parent != null)
                    Parent.IsExpanded = true;
            }
        }


        public string UID
        {
            get
            {
                return _dg.UID;
            }
        }

        public string Name
        {
            get
            {
                return _dg.Name;
            }
            set
            {
                if (_dg.Name != value)
                {
                    var old = _dg.Name;
                    _dg.Name = value;

                    if (old != null)
                        Save();
                }
            }
        }

        public string Path
        {
            get
            {
                return _dg.Path;
            }
            set
            {
                if (_dg.Path != value)
                {
                    var old = _dg.Path;
                    _dg.Path = value;

                    if (old != null)
                        Save();
                }
            }
        }

        public AccessLayer AccessLayer
        {
            get
            {
                return _dg.AccessLayer;
            }

            set
            {
                _dg.AccessLayer = value;
            }
        }

        public Diagram Source
        {
            get
            {
                return _dg;
            }
        }

        public FolderItem Parent { get; set; }

        #endregion

        #region constructor

        public DiagramItem(DSetStore store, Diagram dg)
        {
            _store = store;
            _dg = dg;
        }

        #endregion

        #region methods

        public void Save()
        {
            _store.Save(_dg);
        }

        public void Remove()
        {
            if (Parent != null)
                Parent.Items.Remove(this);

            if (_store != null)
                _store.Remove(_dg);
        }

        public T GetExtension<T>()
        {
            if (_store != null)
                return _store.GetExtension<T>(_dg);

            return default(T);
        }

        public void AddExtension<T>(T obj)
        {
            if (_store != null)
                _store.AddExtension<T>(_dg, obj);
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

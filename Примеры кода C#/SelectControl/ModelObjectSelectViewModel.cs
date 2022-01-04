using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.Mal;
using Monitel.Mal.Native;
using Monitel.Mal.Providers.Mal;
using Monitel.PlatformInfrastructure.TextTools;
using Monitel.UI.Infrastructure.Services;
using Monitel.Mal.Context.CIM16.Ext.EMS;
using cim = Monitel.Mal.Context.CIM16;

namespace Monitel.SCADA.UICommon.SelectControl
{
    internal class ModelObjectViewModel
    {
        internal struct MetaInfo
        {
            internal ClassAssociation ParentAssociation;
            internal ClassAttribute NameAttribute;
            internal MetaClass FolderClass;
        }

        private ModelObjectSelectControl parent;
        private Dictionary<long, ModelItem> modelItems = new Dictionary<long, ModelItem>(); //отфильтрованная модель
        private ObservableCollection<ModelItem> currentPath = new ObservableCollection<ModelItem>(); //путь до текущего объекта модели
        private List<ModelItem> currentItems = new List<ModelItem>(); //коллекция всех объектов нижнего уровня для текущего объекта модели
        private HashSet<MetaClass> filterClasses = new HashSet<MetaClass>();
        private ModelItem currentItem = null; //текущий объект модели
        private string mask = null; //маска для фильтрации объектов
        private bool isCheckedOnlyVisible = false;
        private bool isFuzzySearch = true;

        internal MetaInfo Meta = new MetaInfo();
        internal IServiceManager ServiceManager { get; private set; }
        internal MalProvider MalProvider { get; private set; }
        internal ModelObjectSelectControl Parent { get { return parent; } }
        internal Dictionary<long, ModelItem> ModelItems { get { return modelItems; } }
        internal HashSet<MetaClass> FilterClasses { get { return filterClasses; } }
        internal ObservableCollection<ModelItem> CurrentPath { get { return currentPath; } }
        internal List<ModelItem> CurrentItems { get { return currentItems; } }
        internal ModelItem CurrentItem
        {
            get { return currentItem; }
            set
            {
                currentItem = value;
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    RefreshPath();
                    RefreshItems();
                    CollectionViewSource.GetDefaultView(Parent.lbItems.ItemsSource).Refresh();
                }
                finally { Mouse.OverrideCursor = null; }
            }
        }

        internal IEnumerable<ModelItem> CheckedObjects
        {
            get
            {
                List<ModelItem> result = new List<ModelItem>();
                Stack<ModelItem> items = new Stack<ModelItem>(ModelItems.Values);
                while (items.Any())
                {
                    ModelItem item = items.Pop();
                    if (item.IsChecked) result.Add(item);
                    foreach (ModelItem mi in item.Items.Values) items.Push(mi);
                }
                return result;
            }
        }

        private void SetCurrentItemsCount()
        {
            Parent.CurrentItemsCount = currentItems.Count(i => !string.IsNullOrEmpty(i.Name));
        }

        internal bool IsFiltered
        {
            get { return filterClasses.Any(); }
        }

        internal string Mask
        {
            get { return mask; }
            set
            {
                mask = value;
                RefreshItems();
            }
        }

        internal bool IsCheckVisible { get; set; }

        internal bool IsFuzzySearch
        {
            get { return isFuzzySearch; }
            set
            {
                isFuzzySearch = value;
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    RefreshItems();
                }
                finally { Mouse.OverrideCursor = null; }
            }
        }

        internal bool IsCheckedOnlyVisible
        {
            get { return isCheckedOnlyVisible; }
            set
            {
                isCheckedOnlyVisible = value;
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    RefreshItems();
                }
                finally { Mouse.OverrideCursor = null; }
            }
        }

        internal Style ObjectStyle { get; set; }

        internal Style CheckedObjectStyle { get; set; }

        internal ModelObjectViewModel(ModelObjectSelectControl parent, IServiceManager serviceManager, MalProvider malProvider)
        {
            this.parent = parent;
            ServiceManager = serviceManager;
            MalProvider = malProvider;
            MetaClass ioClass = ServiceManager.DataSource.MainModelImage.MetaData.Classes[cim.Names.IdentifiedObject.ClassName];
            Meta.ParentAssociation = ioClass.Associations.FirstOrDefault(ca => ca.Name.Equals(cim.Names.IdentifiedObject.Properties.ParentObject));
            Meta.NameAttribute = ioClass.Attributes.FirstOrDefault(ca => ca.Name.Equals(cim.Names.IdentifiedObject.Properties.name));
            Meta.FolderClass = ServiceManager.DataSource.MainModelImage.MetaData.Classes[cim.Names.Folder.ClassName];
        }

        internal void SetCheckedObjects(IEnumerable<IMalObject> checkedObjects)
        {
            Stack<ModelItem> items = new Stack<ModelItem>(ModelItems.Values);
            while (items.Any())
            {
                ModelItem item = items.Pop();
                item.IsChecked = item.MalObject != null && checkedObjects.Contains(item.MalObject);
                foreach (ModelItem mi in item.Items.Values) items.Push(mi);
            }
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                RefreshItems();
                CollectionViewSource.GetDefaultView(Parent.lbItems.ItemsSource).Refresh();
            }
            finally { Mouse.OverrideCursor = null; }
        }

        internal void SetClassFilter(IEnumerable<MetaClass> metaClasses)
        {
            IMalObject currentPathObject = currentItem != null ? currentItem.MalObject : null;
            modelItems.Clear();
            currentPath.Clear();
            filterClasses.Clear();

            MalSnapshot snapshot = MalProvider.GetCurrentSnapshot();

            Guid[] aorUids = null;
            var diogenDiagramClass = snapshot.MetaData.Classes[nameof(cim.DiogenDiagram)];
            if (metaClasses.Contains(diogenDiagramClass))
                aorUids =
                    ServiceManager
                    .AccessService
                    .Queries
                    .GetAllowedObjectUids(Access.SpecialUids.OperationUids.ControlInAreaOfResponsibility);

            foreach (MetaClass metaClass in metaClasses)
            {
                filterClasses.Add(metaClass);
                MalObjectPtr[] malObjectPtrs = snapshot.GetObjects(metaClass);
                for (int i = 0; i < malObjectPtrs.Length; i++)
                {
                    MalObjectPtr malObjectPtr = malObjectPtrs[i];
                    if (metaClass != diogenDiagramClass || IsAOR(malObjectPtr, aorUids))
                        AddModelItemPath(malObjectPtr, snapshot);
                }
            }
            if (modelItems.Count > 1)
            {
                MetaClass baseObjectClass = ServiceManager.DataSource.MainModelImage.MetaData.Classes[cim.Names.BaseObjectRoot.ClassName];
                foreach (KeyValuePair<long, ModelItem> kvp in modelItems.ToArray())
                {
                    if (kvp.Value.MalObject.MetaType != baseObjectClass) modelItems.Remove(kvp.Key);
                }
            }
            if (modelItems.Count == 1)
            {
                //убрать корневую часть пути без вариантов выбора
                ModelItem rootItem = modelItems.Values.First();
                while (rootItem.Items.Count == 1)
                    rootItem = rootItem.Items.Values.First();
                if (modelItems.Values.First() != rootItem)
                {
                    if (FilterClasses.Any(fc => rootItem.MetaClass.IsDescendantOf(fc))) rootItem = rootItem.Parent;
                    rootItem.Parent = null;
                    modelItems.Clear();
                    modelItems.Add(rootItem.MalObject.Id, rootItem);
                }
                if (currentPathObject != null)
                {
                    IMalObject parentObj = currentPathObject;
                    ModelItem item = null;
                    do
                    {
                        item = FindModelItem(parentObj.Id);
                        parentObj = parentObj.GetParent();
                    } while (parentObj != null && item == null);
                    if (item != null)
                    {
                        if (item.IsLeaf) item = item.Parent;
                        CurrentItem = item;
                    }
                    else CurrentItem = modelItems.First().Value;
                }
                else CurrentItem = modelItems.First().Value;
            }
            else CurrentItem = null;
        }

        private bool IsAOR(MalObjectPtr malObjectPtr, Guid[] aorUids)
        {
            if (aorUids?.Any() ?? false)
            {
                var mObj = ServiceManager.DataSource.MainModelImage.GetObject(malObjectPtr.ObjectId.Value) as cim.IdentifiedObject;
                if (mObj is cim.DiogenDiagram dd)
                    mObj = dd.OwnerObject;
                if (mObj.GetAreaOfResponsibility() is cim.AreaOfResponsibility aor)
                    return aorUids.Contains(aor.Uid);
                else
                    return true;
            }
            else
                return true;
        }

        internal void ClearFilter()
        {
            CurrentItem = null;
            modelItems.Clear();
            filterClasses.Clear();
        }

        private void AddModelItemPath(MalObjectPtr malObjectPtr, MalSnapshot snapshot)
        {
            MalObjectPtr parentObjPtr = snapshot.GetLinkToOne(malObjectPtr, Meta.ParentAssociation);
            if (parentObjPtr.IsNull) return; //это выпавший из визуального дерева объект
            List<MalObjectPtr> parents = new List<MalObjectPtr>();
            HashSet<long> ids = new HashSet<long>();
            MalObjectPtr parent = malObjectPtr;
            while (!parent.IsNull)
            {
                if (!ids.Contains(parent.ObjectId.Value))
                    ids.Add(parent.ObjectId.Value);
                else
                {
                    if (ServiceManager.DataSource.MainModelImage.GetObject(parent.ObjectId.Value) is IMalObject mo)
                        throw new Exception($"The object (Uid={mo.Uid}) has Parent reference to itself. The model needs validation.");
                    else
                        throw new Exception($"The object (Id={parent.ObjectId.Value}) does not found in the model. The model needs validation.");
                }
                parents.Insert(0, parent);
                parent = snapshot.GetLinkToOne(parent, Meta.ParentAssociation);
            }
            ModelItem currentParent = null;
            Dictionary<long, ModelItem> currentItems = modelItems;
            foreach (MalObjectPtr mo in parents)
            {
                ModelItem item = null;
                if (!currentItems.ContainsKey(mo.ObjectId.Value))
                {
                    item = new ModelItem(this, currentParent, mo);
                    item.IsLeaf = (mo == parents.Last());
                    currentItems.Add(mo.ObjectId.Value, item);
                    if (currentParent != null) currentParent.IsLeaf = false;
                }
                else
                    item = currentItems[mo.ObjectId.Value];
                currentParent = item;
                currentItems = item.Items;
            }
        }

        private void RefreshPath()
        {
            if (currentItem != null)
            {
                //построить новый путь
                List<ModelItem> newPath = new List<ModelItem>();
                ModelItem item = currentItem;
                while (item != null)
                {
                    newPath.Insert(0, item);
                    item = item.Parent;
                }
                //перестроить текущий путь в соответствии с новым
                for (int i = 0; i < newPath.Count; i++)
                {
                    item = newPath[i];
                    if (i < currentPath.Count)
                    {
                        if (currentPath[i] != item)
                        {
                            //удалить несовпадающую часть пути
                            while (i < currentPath.Count) currentPath.RemoveAt(i);
                            currentPath.Add(item);
                        }
                    }
                    else
                        currentPath.Add(item);
                }
                while (currentPath.Count > newPath.Count) currentPath.RemoveAt(newPath.Count);
            }
            else
                currentPath.Clear();
        }

        private List<ModelItem> tmpItems = new List<ModelItem>();
        private int tmpItemsCount;
        //если список содержит большее количество записей, 
        //то сортировку по имени не выполняем из-за соображений производительности
        private const int itemsSortCount = 5000;

        internal void SetMaskWithCancellation(string mask, CancellationToken token)
        {
            this.mask = mask;
            RefreshItems(token);
        }

        internal void RefreshItems(CancellationToken? cancellationToken = null)
        {
            if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                cancellationToken.Value.ThrowIfCancellationRequested();
            currentItems.Clear();
            tmpItems.Clear();
            if (currentItem != null)
                RefreshItemsIn(currentItem.Items, cancellationToken);
            tmpItemsCount = tmpItems.Count;
            tmpItems.Sort(ComparisonModelItem);
            int n = -1;
            for (int i = 0; i < tmpItems.Count; i++)
            {
                ModelItem item = tmpItems[i];
                if (i == 0 && !string.IsNullOrEmpty(item.Path)) break;
                else if (!string.IsNullOrEmpty(item.Path))
                {
                    n = i;
                    break;
                }
            }
            if (n > 0) tmpItems.Insert(n, new ModelItem(this, null, null));
            currentItems.AddRange(tmpItems);
            Parent.OnCurrentItemsChanged();
            SetCurrentItemsCount();
        }

        private void RefreshItemsIn(Dictionary<long, ModelItem> items, CancellationToken? cancellationToken)
        {
            string maskLower = string.IsNullOrEmpty(mask) ? null : mask.ToLower();
            foreach (ModelItem item in items.Values)
            {
                if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                    cancellationToken.Value.ThrowIfCancellationRequested();
                if (item.Items.Count > 0) RefreshItemsIn(item.Items, cancellationToken);
                string itemName = item.Name.ToLower();
                //проверка на показ только отмеченных
                if (item.IsInFilter && (!isCheckedOnlyVisible || item.IsChecked))
                {
                    //проверка на маску фильтрации
                    //по имени или идентификатору
                    bool include = string.IsNullOrEmpty(maskLower) ||
                        itemName.Contains(maskLower) ||
                        item.MalObject.Uid.ToString().Equals(mask, StringComparison.InvariantCultureIgnoreCase);
                    if (include) item.SearchCompliance = SearchCompliances.Exact;
                    else
                    {
                        if (IsFuzzySearch)
                        {
                            //если разрешен нечеткий поиск
                            DLDistance dist = LevenshteinSort.DamerauLevenshteinDistance(maskLower, itemName);
                            include = dist.MatchCount >= maskLower.Length * 0.8 && dist.ErrorCount <= 2;
                            if (include) item.SearchCompliance = SearchCompliances.Fuzzy;
                        }
                    }
                    if (include) tmpItems.Add(item);
                }
            }
        }

        private int ComparisonModelItem(ModelItem x, ModelItem y)
        {
            if (x.Parent != y.Parent)
            {
                if (x.Parent == currentItem) return -1;
                else if (y.Parent == currentItem) return 1;
                else return tmpItemsCount > itemsSortCount ? 0 : ComparisonModelItemIn(x, y);
            }
            else return tmpItemsCount > itemsSortCount ? 0 : ComparisonModelItemIn(x, y);
        }

        private int ComparisonModelItemIn(ModelItem x, ModelItem y)
        {
            if (x.SearchCompliance != y.SearchCompliance)
            {
                if (x.SearchCompliance < y.SearchCompliance) return -1;
                else return 1;
            }
            else return string.Compare(x.Name, y.Name, true);
        }

        internal ModelItem FindModelItem(long malObjectId)
        {
            return FindModelItemIn(malObjectId, modelItems);
        }

        private ModelItem FindModelItemIn(long malObjectId, Dictionary<long, ModelItem> items)
        {
            ModelItem result = null;
            if (items.ContainsKey(malObjectId))
                result = items[malObjectId];
            else
                foreach (ModelItem item in items.Values)
                {
                    result = FindModelItemIn(malObjectId, item.Items);
                    if (result != null) break;
                }
            return result;
        }

    }

    public enum SearchCompliances
    {
        Exact = 0,
        Fuzzy = 1,
        ByPath = 2
    }

    public class ModelItem : INotifyPropertyChanged
    {
        private long _malObjectId;
        private Dictionary<long, ModelItem> _items = new Dictionary<long, ModelItem>();
        private ModelItem _parent;
        private bool _isChecked = false;
        private IMalObject _malObject = null;

        internal ModelObjectViewModel ViewModel { get; }


        public IMalObject MalObject
        {
            get
            {
                if (_malObject == null && _malObjectId > 0)
                    _malObject = ViewModel.ServiceManager.DataSource.MainModelImage.GetObject(_malObjectId);
                return _malObject;
            }
        }

        public bool MalObjectIsAlive
        {
            get
            {
                IMalObject mo = MalObject;
                if (mo != null) return mo.IsAlive;
                else return false;
            }
        }
        public Dictionary<long, ModelItem> Items { get { return _items; } }

        public ModelItem Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public MetaClass MetaClass { get; private set; }
        /// <summary>
        /// Услолвие поиска, в соответствии с которым ModelItem был отобран
        /// </summary>
        public SearchCompliances SearchCompliance { get; set; }
        /// <summary>
        /// Является ли ModelItem последним в иерархии
        /// </summary>
        public bool IsLeaf { get; set; }
        /// <summary>
        /// Входит ли класс MAL объекта ModelItem в список классов, по которым отфильтрована модель
        /// </summary>
        public bool IsInFilter
        {
            get
            {
                return ViewModel.FilterClasses.Any(mc => MetaClass.IsDescendantOf(mc));
            }
        }

        public bool HasItems { get { return _items.Values.Any(_mi => !_mi.IsLeaf); } }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                IsCheckedSet(value);
                ModelObjectSelectControl selControl = ViewModel.Parent;
                if (!selControl.isCheckingInternal && selControl.ViewKind != ModelObjectSelectControl.ViewKinds.Flat)
                    selControl.TreeControlSetCheckedObjects(new IMalObject[1] { MalObject }, value);
            }
        }

        private void IsCheckedSet(bool value)
        {
            if (_isChecked == value) return;
            _isChecked = value;
            OnPropertyChanged("IsChecked");
            if (!_isChecked && ViewModel.IsCheckedOnlyVisible)
            {
                ViewModel.RefreshItems();
                CollectionViewSource.GetDefaultView(ViewModel.Parent.lbItems.ItemsSource).Refresh();
            }
            if (_isChecked) ViewModel.Parent.OnObjectChecked(MalObject);
            else ViewModel.Parent.OnObjectUnchecked(MalObject);
        }

        internal void IsCheckedSetByCode(bool value)
        {
            IsCheckedSet(value);
        }

        public bool CheckShow { get { return ViewModel.IsCheckVisible; } }

        public Style ObjectStyle { get { return ViewModel.ObjectStyle; } }

        public Style CheckedObjectStyle { get { return ViewModel.CheckedObjectStyle; } }

        private string name = null;
        public string Name
        {
            get
            {
                if (name == null)
                {
                    if (MetaClass != null && MetaClass.Name.Equals(cim.Names.BaseObjectRoot.ClassName)) name = "";
                    else if (MalObject != null) name = MalObject.GetName();
                    if (name == null) name = "";
                }
                return name;
            }
        }

        public bool IsFolder { get; private set; }

        private ImageSource _image;
        public ImageSource Image
        {
            get
            {
                if (_image == null)
                {
                    if (ViewModel.ServiceManager.ClassIcons.GetObjectIcon(_malObjectId) is IIcon icon)
                        _image = icon.Image.GetAsFrozen() as ImageSource;
                }
                return _image;
            }
        }

        public string Path
        {
            get
            {
                string result = null;
                ModelItem parent = Parent;
                while (parent != null)
                {
                    if (parent == ViewModel.CurrentItem)
                    {
                        if (!string.IsNullOrEmpty(result)) result = "...\\" + result;
                        break;
                    }
                    if (!string.IsNullOrEmpty(result)) result = '\\' + result;
                    result = parent.Name + result;
                    parent = parent.Parent;
                }
                return result;
            }
        }

        internal ModelItem(ModelObjectViewModel viewModel, ModelItem parent, MalObjectPtr? malObjectPtr)
        {
            ViewModel = viewModel;
            _parent = parent;
            if (malObjectPtr.HasValue)
            {
                _malObjectId = malObjectPtr.Value.ObjectId.Value;
                if (!malObjectPtr.Value.IsNull)
                {
                    MetaClass = ViewModel.MalProvider.GetCurrentSnapshot().GetClass(malObjectPtr.Value);
                    //IIcon icon = ViewModel.ServiceManager.ClassIcons.GetObjectIcon(_malObjectId);
                    //if (icon != null) Image = icon.Image;
                    if (MetaClass.IsDescendantOf(ViewModel.Meta.FolderClass)) IsFolder = true;
                }
            }
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(Name) ? Name : base.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler tmp = PropertyChanged;
            if (tmp != null) tmp(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

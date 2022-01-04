using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.Diogen.Controls;
using Monitel.Localization;
using Monitel.Mal;
using Monitel.Mal.Providers;
using Monitel.Mal.Providers.Mal;
using Monitel.PlatformInfrastructure.TextTools;
using Monitel.UI.Infrastructure.Events;
using Monitel.UI.Infrastructure.ObjectTree;
using Monitel.UI.Infrastructure.Services;
using Monitel.UI.ObjectTree;

namespace Monitel.SCADA.UICommon.SelectControl
{
    /// <summary>
    /// Interaction logic for ModelObjectSelectControl.xaml
    /// </summary>
    public partial class ModelObjectSelectControl : UserControl, INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Варианты представления объектов модели
        /// </summary>
        public enum ViewKinds
        {
            /// <summary>
            /// список (плоское представление)
            /// </summary>
            Flat,
            /// <summary>
            /// иерархическое представление
            /// </summary>
            Tree,
            /// <summary>
            /// поддерживаются оба варианта
            /// </summary>
            Both
        }

        private const int maskEditDelay = 1000; //мс
        private const double dragShift = 7;
        private const string malObjectUIDFormat = "Monitel.Mag.Format.ObjectUid";
        private const int pathMenuPageLength = 25;
        private const string viewKindNameFlat = "Плоский список";
        private const string viewKindNameTree = "Иерархический список";

        private bool allowDrag = false;

        private ModelObjectViewModel viewModel;
        private string mask = null;
        private Timer maskEditTimer;
        private DateTime lastClickTime = DateTime.MinValue;
        private bool isCompactView = false;
        private bool isCheckVisible = false;
        private bool isCheckedOnlyVisible = false;
        private Style objectStyle = null;
        private Style checkedObjectStyle = null;
        private Timer itemDblClickTimer;
        private int itemClickCount = 0;
        private ModelItem itemClicked = null;
        private int currentItemsCount;
        private Point? mouseDownPos;
        private Dictionary<ModelItem, int> dicPageNums = new Dictionary<ModelItem, int>();
        private bool isFuzzySearch = true;
        private bool isTreeView = false;
        private ViewKinds viewKind = ViewKinds.Both;
        private ObjectTreeDataSource treeDS;

        internal bool isCheckingInternal = false;

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        private static bool SetCursorPos(Point point)
        {
            double dpi = Monitel.Diogen.Win32.NF.GetDPIFactor();
            int x = Convert.ToInt32(point.X / dpi);
            int y = Convert.ToInt32(point.Y / dpi);
            return SetCursorPos(x, y);
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public ModelObjectSelectControl()
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                IsTreeView = false;
                DataContext = this;
            }
            tbMask.Style = Resources["maskStyleDefault"] as Style;
        }

        private void ItemDblClickTimerCallback(object state)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!(itemClicked == null || itemClicked.MalObject == null))
                    {
                        if (itemClickCount > 1) OnObjectDblClick(itemClicked.MalObject);
                        else OnObjectClick(itemClicked.MalObject);
                    }
                    itemClickCount = 0;
                    itemClicked = null;
                }));
        }

        /// <summary>
        /// Инициализирует элемент управления
        /// </summary>
        /// <param name="serviceManager">IServiceManager</param>
        public void Init(IServiceManager serviceManager, MalProvider malProvider)
        {
            maskEditTimer = new Timer(maskEditTimerCallback);
            itemDblClickTimer = new Timer(ItemDblClickTimerCallback);
            viewModel = new ModelObjectViewModel(this, serviceManager, malProvider);
            viewModel.IsCheckVisible = isCheckVisible;
            viewModel.IsCheckedOnlyVisible = isCheckedOnlyVisible;
            viewModel.ObjectStyle = objectStyle;
            viewModel.CheckedObjectStyle = checkedObjectStyle;
            viewModel.IsFuzzySearch = isFuzzySearch;
            lbPath.ItemsSource = viewModel.CurrentPath;
            lbItems.ItemsSource = viewModel.CurrentItems;
        }

        /// <summary>
        /// Если True - используется иерархическое представление,  иначе - плоское
        /// </summary>
        public bool IsTreeView
        {
            get { return isTreeView; }
            set
            {
                if (value && viewKind == ViewKinds.Flat) return;
                isTreeView = value;
                TreeViewVisibleUpdate();
                if (isTreeView)
                {
                    TreeControlSetCheckedObjects();
                    treeControl.Focus();
                }
                else
                    lbItems.Focus();
                OnPropertyChanged("IsTreeView");
                OnPropertyChanged("CurrentItemsCount");
            }
        }

        internal void TreeControlSetCheckedObjects()
        {
            treeControl.CheckedObjectsChanged -= TreeControl_CheckedObjectsChanged;
            try
            {
                treeControl.CheckObjects(treeControl.CheckedObjects.Select(p => p.Id), false);
                treeControl.CheckObjects(CheckedObjects.Select(co => co.MalObject.Id), true);
            }
            finally
            {
                treeControl.CheckedObjectsChanged += TreeControl_CheckedObjectsChanged;
            }
        }

        internal void TreeControlSetCheckedObjects(IEnumerable<IMalObject> malObjects, bool isChecked)
        {
            treeControl.CheckedObjectsChanged -= TreeControl_CheckedObjectsChanged;
            try
            {
                treeControl.CheckObjects(malObjects.Select(p => p.Id), isChecked);
            }
            finally
            {
                treeControl.CheckedObjectsChanged += TreeControl_CheckedObjectsChanged;
            }
        }

        /// <summary>
        /// Вариант представления объектов модели
        /// </summary>
        public ViewKinds ViewKind
        {
            get { return viewKind; }
            set
            {
                viewKind = value;
                if (viewKind != ViewKinds.Both) chkIsTreeView.Visibility = Visibility.Collapsed;
                else chkIsTreeView.Visibility = Visibility.Visible;
            }
        }

        private void TreeViewInit()
        {
            treeDS = new ObjectTreeDataSource(viewModel.ServiceManager, new TreeProvider(viewModel));
            treeDS.SelectActiveTree(treeDS.Trees.First().Id);
            treeControl.Services = viewModel.ServiceManager;
            treeControl.TreeDataSource = treeDS;
            treeControl.MarkSystemObjects = true;
            treeControl.ObjectContextMenuOpening += TreeControl_ObjectContextMenuOpening;
            treeControl.ObjectMouseClicked += TreeControl_ObjectMouseClicked;
            treeControl.ObjectMouseDoubleClicked += TreeControl_ObjectMouseDoubleClicked;
            treeControl.CheckedObjectsChanged -= TreeControl_CheckedObjectsChanged;
            treeControl.CheckedObjectsChanged += TreeControl_CheckedObjectsChanged;
            treeControl.ObjectKeyUpped += TreeControl_ObjectKeyUpped;
            Type treeControlType = treeControl.GetType();
            FieldInfo treeInfo = treeControlType.GetField("tree", BindingFlags.Instance | BindingFlags.NonPublic);
            if (treeInfo != null)
            {
                object tree = treeInfo.GetValue(treeControl);
                PropertyInfo showBorderInfo = tree.GetType().GetProperty("ShowBorder");
                if (showBorderInfo != null)
                    showBorderInfo.SetValue(tree, false);
            }
            treeSearchControl.SelectedClass = viewModel.ServiceManager.DataSource.MainModelImage.MetaData.Classes[Mal.Context.CIM16.Names.IdentifiedObject.ClassName];
        }

        private void TreeViewDispose()
        {
            if (treeDS != null)
            {
                treeDS.Dispose();
                treeDS = null;
                treeControl.ObjectContextMenuOpening -= TreeControl_ObjectContextMenuOpening;
                treeControl.ObjectMouseClicked -= TreeControl_ObjectMouseClicked;
                treeControl.ObjectMouseDoubleClicked -= TreeControl_ObjectMouseDoubleClicked;
                treeControl.CheckedObjectsChanged -= TreeControl_CheckedObjectsChanged;
                treeControl.ObjectKeyUpped -= TreeControl_ObjectKeyUpped;
                //данный метод по факту вызывается только при разрыве соединения, при этом освобождать эти контролы не требуется
                //treeControl.Dispose();
                //treeSearchControl.Dispose();
            }
        }

        private void TreeControl_ObjectKeyUpped(object sender, ObjectKeyEventArgs e)
        {
            if (treeControl.MultiSelectStyle == MultiSelectStyle.CheckBoxes)
            {
                if (e.Key == Key.Space)
                {
                    bool isChecked = treeControl.CheckedObjects.Select(p => p.Id).ToArray().Contains(e.ObjectId);
                    treeControl.CheckObjects(new long[] { e.ObjectId }, !isChecked);
                }
            }
            else if (e.Key == Key.Enter)
            {
                var mo = viewModel.ServiceManager.DataSource.MainModelImage.GetObject(e.ObjectId);
                if (mo != null)
                    OnObjectDblClick(mo);
            }
        }

        private void TreeControl_CheckedObjectsChanged(object sender, EventArgs e)
        {
            SetCheckedObjects(treeControl.CheckedObjects.Select(p => viewModel.ServiceManager.DataSource.MainModelImage.GetObject(p.Id)));
        }

        private void TreeControl_ObjectMouseClicked(object sender, ObjectEventArgs e)
        {
            var mo = viewModel.ServiceManager.DataSource.MainModelImage.GetObject(e.ObjectId);
            if (mo != null)
                OnObjectClick(mo);
        }

        private void TreeControl_ObjectMouseDoubleClicked(object sender, ObjectEventArgs e)
        {
            var mo = viewModel.ServiceManager.DataSource.MainModelImage.GetObject(e.ObjectId);
            if (mo != null)
                OnObjectDblClick(mo);
        }

        private void TreeControl_ObjectContextMenuOpening(object sender, ObjectContextMenuOpeningEventArgs e)
        {
            var mo = viewModel.ServiceManager.DataSource.MainModelImage.GetObject(e.ObjectId);
            if (mo != null)
                ObjectContextMenuOpening(e.Menu, mo);
        }

        private void TreeViewVisibleUpdate()
        {
            if (isTreeView)
            {
                lbItems.Visibility = Visibility.Collapsed;
                if (!string.IsNullOrEmpty(Mask)) SearchInTree();
                else treeSearchControl.Visibility = Visibility.Collapsed;
                gridTree.Visibility = Visibility.Visible;
                txStatisticTitle.Text = LocalizationManager.GetString("totalFound");
                tbMaskPrompt.Text = LocalizationManager.GetString("find");
                chkIsTreeView.ToolTip = viewKindNameFlat;
            }
            else
            {
                lbItems.Visibility = Visibility.Visible;
                gridTree.Visibility = Visibility.Collapsed;
                txStatisticTitle.Text = LocalizationManager.GetString("totalObjects");
                tbMaskPrompt.Text = LocalizationManager.GetString("filter");
                chkIsTreeView.ToolTip = viewKindNameTree;
            }
            spStatistic.Visibility = (!isTreeView || treeSearchControl.IsVisible) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Словарь объектов модели, отфильтрованный в соответствии со списком классов, переданных в SetClassFilter()
        /// </summary>
        public Dictionary<long, ModelItem> ModelItems { get { return viewModel != null ? viewModel.ModelItems : null; } }

        /// <summary>
        /// Если True - отмеченные позиции в списке объектов будут выделяться стилем (CheckedObjectStyle) и иконкой
        /// </summary>
        public bool IsCheckVisible
        {
            get { return isCheckVisible; }
            set
            {
                isCheckVisible = value;
                if (viewModel != null) viewModel.IsCheckVisible = value;
                if (viewKind != ViewKinds.Flat)
                {
                    if (value) treeControl.MultiSelectStyle = UI.ObjectTree.MultiSelectStyle.CheckBoxes;
                    else if (SelectionMode == SelectionMode.Single) treeControl.MultiSelectStyle = UI.ObjectTree.MultiSelectStyle.Disabled;
                    else treeControl.MultiSelectStyle = UI.ObjectTree.MultiSelectStyle.ControlSelect;
                }
            }
        }

        /// <summary>
        /// Если True - в списке будут показываться только отмеченные объекты. Требуется установка свойства IsCheckVisible.
        /// </summary>
        public bool IsCheckedOnlyVisible
        {
            get { return isCheckedOnlyVisible; }
            set
            {
                isCheckedOnlyVisible = value;
                if (viewModel != null)
                {
                    viewModel.IsCheckedOnlyVisible = value;
                    CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
                }
            }
        }

        /// <summary>
        /// Стиль для элементов в списке объектов (TargetType = TextBlock)
        /// </summary>
        public Style ObjectStyle
        {
            get { return objectStyle; }
            set
            {
                objectStyle = value;
                if (viewModel != null) viewModel.ObjectStyle = objectStyle;
            }
        }

        /// <summary>
        /// Стиль для отмеченных элементов в списке объектов (TargetType = TextBlock)
        /// </summary>
        public Style CheckedObjectStyle
        {
            get { return checkedObjectStyle; }
            set
            {
                checkedObjectStyle = value;
                if (viewModel != null) viewModel.CheckedObjectStyle = checkedObjectStyle;
            }
        }

        /// <summary>
        /// Список объектов для текущего пути
        /// </summary>
        public List<ModelItem> CurrentItems { get { return viewModel != null ? viewModel.CurrentItems : null; } }

        /// <summary>
        /// Количество объектов для текущего пути
        /// </summary>
        public int CurrentItemsCount
        {
            get
            {
                if (IsTreeView)
                    return treeSearchControl.SearchList.Count;
                else
                    return currentItemsCount;
            }
            internal set
            {
                currentItemsCount = value;
                OnPropertyChanged("CurrentItemsCount");
            }
        }

        /// <summary>
        /// Разрешен ли нечеткий поиск
        /// </summary>
        public bool IsFuzzySearch
        {
            get { return viewModel != null ? viewModel.IsFuzzySearch : isFuzzySearch; }
            set
            {
                isFuzzySearch = value;
                if (viewModel != null)
                {
                    viewModel.IsFuzzySearch = value;
                    CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
                    if (viewKind != ViewKinds.Flat) SearchInTree();
                }
                OnPropertyChanged("IsFuzzySearch");
            }
        }

        /// <summary>
        /// Коллекция отмеченных объектов во всем срезе модели
        /// </summary>
        public IEnumerable<ModelItem> CheckedObjects
        {
            get { return viewModel.CheckedObjects; }
        }

        /// <summary>
        /// Коллекция выделенных объектов в текущем списке
        /// </summary>
        public IEnumerable<ModelItem> SelectedItems
        {
            get
            {
                if (IsTreeView)
                {
                    List<ModelItem> result = new List<ModelItem>();
                    if (treeControl.SelectedObject != null)
                    {
                        IMalObject selObj = viewModel.ServiceManager.DataSource.MainModelImage.GetObject(treeControl.SelectedObject.Id);

                        if (selObj != null && selObj.IsAlive)
                        {
                            if (viewModel.FilterClasses.Any(mc => selObj.MetaType.IsDescendantOf(mc)))
                            {
                                ModelItem item = viewModel.FindModelItem(selObj.Id);
                                if (item != null) result.Add(item);
                            }
                        }
                    }
                    return result;
                }
                else
                    return lbItems.SelectedItems.Cast<ModelItem>();
            }
        }

        /// <summary>
        /// Находит переданный объект и выделяет его
        /// </summary>
        /// <param name="malObject">Объект для выделения</param>
        /// <returns>True - если объект найден и выделен, False - в противном случае</returns>
        public bool SelectObject(IMalObject malObject)
        {
            bool result = false;
            bool treeResult = false, listResult = false;
            if (malObject != null)
            {
                if (ViewKind != ViewKinds.Flat)
                    treeResult = treeControl.LocateObject(malObject.Id);

                if (ViewKind != ViewKinds.Tree)
                {
                    ModelItem item = viewModel.FindModelItem(malObject.Id);
                    if (!(item == null || item.Parent == null))
                    {
                        viewModel.CurrentItem = item.Parent;
                        lbItems.SelectedItem = item;
                        lbItems.ScrollIntoView(item);
                        listResult = true;
                    }
                }
                if (IsTreeView)
                    result = treeResult;
                else
                    result = listResult;
            }
            return result;
        }

        /// <summary>
        /// Если True - видно поле маски для фильтрации текущего списка объектов
        /// </summary>
        public bool IsMaskVisible
        {
            get { return sepMask.Visibility == Visibility.Visible; }
            set
            {
                if (value)
                {
                    tbMask.Visibility = Visibility.Collapsed;
                    tbMaskPrompt.Visibility = Visibility.Visible;
                    sepMask.Visibility = Visibility.Visible;
                }
                else
                {
                    tbMask.Visibility = Visibility.Collapsed;
                    tbMaskPrompt.Visibility = Visibility.Collapsed;
                    sepMask.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Определяет поведение выделения объектов в текущем списке
        /// </summary>
        public SelectionMode SelectionMode
        {
            get { return lbItems.SelectionMode; }
            set
            {
                lbItems.SelectionMode = value;
                if (value == SelectionMode.Single) treeControl.MultiSelectStyle = UI.ObjectTree.MultiSelectStyle.Disabled;
                else if (IsCheckVisible) treeControl.MultiSelectStyle = UI.ObjectTree.MultiSelectStyle.CheckBoxes;
                else treeControl.MultiSelectStyle = UI.ObjectTree.MultiSelectStyle.ControlSelect;
            }
        }

        /// <summary>
        /// Если True - элемент управления отображается в компактном виде:
        /// список объектов в одну строку, иконка 16х16, стандартный шрифт
        /// </summary>
        public bool IsCompactView
        {
            get { return isCompactView; }
            set
            {
                isCompactView = value;

                if (IsCompactView)
                {
                    tbMask.SetResourceReference(TextBox.StyleProperty, "maskStyleCompact");
                    tbMaskPrompt.SetResourceReference(TextBox.StyleProperty, "maskStyleCompact");
                }
                else
                {
                    tbMask.SetResourceReference(TextBox.StyleProperty, "maskStyleDefault");
                    tbMaskPrompt.SetResourceReference(TextBox.StyleProperty, "maskStyleDefault");
                }
                ICollectionView view = CollectionViewSource.GetDefaultView(lbItems.ItemsSource);
                if (view != null) view.Refresh();
            }
        }

        /// <summary>
        /// Устанавливает список классов модели, экземпляры которых будут отображаться в списке выбора
        /// </summary>
        /// <param name="metaClasses"></param>
        public void SetClassFilter(IEnumerable<MetaClass> metaClasses)
        {
            if (viewModel != null)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    dicPageNums.Clear();
                    IMalObject treeCurrent = null;
                    if (viewKind != ViewKinds.Flat && treeControl.SelectedObject != null)
                        treeCurrent = viewModel.ServiceManager.DataSource.MainModelImage.GetObject(treeControl.SelectedObject.Id);
                    viewModel.SetClassFilter(metaClasses);
                    CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
                    if (treeDS == null && viewKind != ViewKinds.Flat) TreeViewInit();
                    if (viewKind != ViewKinds.Flat)
                    {
                        treeControl.TreeDataSource.SelectActiveTree(treeControl.TreeDataSource.Trees.First().Id);
                        if (IsTreeView)
                        {
                            if (!string.IsNullOrEmpty(Mask)) SearchInTree();
                            else
                            {
                                if (treeCurrent != null)
                                {
                                    IMalObject findMO = treeCurrent;
                                    while (findMO != null && !treeControl.LocateObject(findMO.Id))
                                        findMO = findMO.GetParent();
                                }
                            }
                        }
                    }
                }
                finally { Mouse.OverrideCursor = null; }
            }
        }

        /// <summary>
        /// Очищает список классов модели
        /// </summary>
        public void ClearFilter()
        {
            if (viewModel != null) viewModel.ClearFilter();
        }

        /// <summary>
        /// Выполняет проверку изменившихся объектов. Если добавлен или удален объект из списка отображаемых классов, то выполняется перерисовка списка выбора.
        /// </summary>
        /// <param name="changedObjects"></param>
        public void CheckChangedObject(IEnumerable<ObjectChange> changedObjects)
        {
            if (viewModel != null)
            {
                ClassesCollection classes = viewModel.ServiceManager.DataSource.MainModelImage.MetaData.Classes;
                foreach (ObjectChange changeObj in changedObjects)
                {
                    if (changeObj.ChangeType == ObjectChangeType.Delete || changeObj.ChangeType == ObjectChangeType.Create)
                    {
                        MetaClass objMC = classes[changeObj.Id.ClassId];
                        if (viewModel.FilterClasses.Any(mc => objMC.IsDescendantOf(mc)))
                        {
                            SetClassFilter(viewModel.FilterClasses.ToArray());
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Выполняет отметку заданных объектов. Для остальных объектов отметка будет снята
        /// </summary>
        /// <param name="checkedObjects">Объекты, которые должны быть отмечены</param>
        public void SetCheckedObjects(IEnumerable<IMalObject> checkedObjects)
        {
            if (viewModel != null)
            {
                isCheckingInternal = true;
                try
                {
                    viewModel.SetCheckedObjects(checkedObjects);
                }
                finally { isCheckingInternal = false; }
                if (ViewKind != ViewKinds.Flat) TreeControlSetCheckedObjects();
            }
        }

        /// <summary>
        /// Снимает отметку со всех объектов 
        /// </summary>
        public void ClearCheckedObjects()
        {
            if (viewModel != null)
            {
                isCheckingInternal = true;
                try
                {
                    viewModel.SetCheckedObjects(Enumerable.Empty<IMalObject>());
                }
                finally { isCheckingInternal = false; }
                if (IsTreeView) TreeControlSetCheckedObjects();
            }
        }

        /// <summary>
        /// Если True - список объектов отфильтрован, в методе SetClassFilter()
        /// </summary>
        public bool IsFiltered
        {
            get
            {
                return viewModel != null ? viewModel.IsFiltered : false;
            }
        }

        /// <summary>
        /// Маска для фильтрации текущего списка объектов
        /// </summary>
        public string Mask
        {
            get { return mask; }
            set
            {
                mask = value;
                if (IsMaskVisible) maskEditTimer.Change(maskEditDelay, Timeout.Infinite);
                else
                {
                    viewModel.Mask = value;
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
                        if (IsTreeView) SearchInTree();
                    }
                    finally { Mouse.OverrideCursor = null; }
                }
                OnPropertyChanged("Mask");
            }
        }

        /// <summary>
        /// Разрешает или запрещает операции drag and drop
        /// </summary>
        public bool AllowDrag
        {
            get { return allowDrag; }
            set { allowDrag = value; }
        }

        /// <summary>
        /// Событие - изменилось свойство
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Событие - щелкнули на объекте
        /// </summary>
        public event EventHandler<MalObjectEventArgs> ObjectClick;

        /// <summary>
        /// Событие - выполнен двойной клик на объекте
        /// </summary>
        public event EventHandler<MalObjectEventArgs> ObjectDblClick;

        /// <summary>
        /// Событие - открывается контекстное меню объекта
        /// </summary>
        public event EventHandler<ObjectMenuOpeningEventArgs> ObjectMenuOpening;

        /// <summary>
        /// Событие - изменилось содержимое списка объектов для текущего пути
        /// </summary>
        public event EventHandler<EventArgs> CurrentItemsChanged;

        /// <summary>
        /// Событие - отметили объект
        /// </summary>
        public event EventHandler<MalObjectEventArgs> ObjectChecked;

        /// <summary>
        /// Событие - сняли отметку с объекта
        /// </summary>
        public event EventHandler<MalObjectEventArgs> ObjectUnchecked;

        /// <summary>
        /// Событие - отпустили клавишу в списке объектов
        /// </summary>
        public event KeyEventHandler ListKeyUp;

        /// <summary>
        /// Событие - начали операцию перетаскивания
        /// </summary>
        public event EventHandler<MalObjectEventArgs> BeginDrag;

        /// <summary>
        /// Перемещает фокус ввода в поле фильтра
        /// </summary>
        public void FocusToMaskEdit()
        {
            tbMaskPrompt.Visibility = Visibility.Collapsed;
            tbMask.Visibility = Visibility.Visible;
            tbMask.Focus();
        }

        /// <summary>
        /// Перемещает фокус ввода в элемент управления
        /// </summary>
        public void FocusToSelectControl()
        {
            if (IsTreeView) treeControl.Focus();
            else lbItems.Focus();
        }

        /// <summary>
        /// IDisposable.Dispose()
        /// </summary>
        public void Dispose()
        {
            if (maskEditTimer != null)
            {
                maskEditTimer.Dispose();
                maskEditTimer = null;
            }
            if (itemDblClickTimer != null)
            {
                itemDblClickTimer.Dispose();
                itemDblClickTimer = null;
            }
            TreeViewDispose();
        }

        Task maskEditTask;
        CancellationTokenSource maskEditCTS;
        CancellationToken maskEditCT;
        private void maskEditTimerCallback(object _state)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    if (maskEditTask != null)
                    {
                        //cancel and dispose antecedent task
                        TaskStatus ts = maskEditTask.Status;
                        if (ts == TaskStatus.Running || ts == TaskStatus.WaitingForActivation || ts == TaskStatus.WaitingToRun || ts == TaskStatus.WaitingForChildrenToComplete)
                        {
                            maskEditCTS.Cancel();
                            try { maskEditTask.Wait(); }
                            catch { }
                        }
                        Mouse.OverrideCursor = null;
                        maskEditTask.Dispose();
                        maskEditTask = null;
                        maskEditCTS.Dispose();
                        maskEditCTS = null;
                    }
                    //create task for prepare items
                    maskEditCTS = new CancellationTokenSource();
                    maskEditCT = maskEditCTS.Token;
                    maskEditTask = new Task((state) =>
                    {
                        viewModel.SetMaskWithCancellation(Mask, (CancellationToken)state);
                    }, maskEditCT, maskEditCT);
                    maskEditTask.ContinueWith(antecedent =>
                    {
                        //refresh listbox in main thread
                        CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
                        if (IsTreeView) SearchInTree();
                        if (!antecedent.IsCanceled) Mouse.OverrideCursor = null; //nomal completion
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                    Mouse.OverrideCursor = Cursors.AppStarting;
                    maskEditTask.Start();
                }));
        }

        private void SearchInTree()
        {
            if (!string.IsNullOrEmpty(Mask))
            {
                treeSearchControl.FillSearchList(GetSearchList().Select(p => p.Id));
                if (treeSearchControl.SearchList.Count > 0)
                    treeControl.LocateObject((treeSearchControl.SearchList.GetItemAt(0) as SearchItemViewModel).TreeItem.LinkedObject.Id);
                OnPropertyChanged("CurrentItemsCount");
                treeSearchControl.Visibility = Visibility.Visible;
                treeSearchControl.IsSearchExpanded = true;
                spStatistic.Visibility = Visibility.Visible;
            }
            else
            {
                treeSearchControl.FillSearchList(null);
                treeSearchControl.Visibility = Visibility.Collapsed;
                if (IsTreeView) spStatistic.Visibility = Visibility.Collapsed;
            }
        }

        private IEnumerable<IMalObject> GetSearchList()
        {
            List<IMalObject> result = new List<IMalObject>();
            Guid uid;
            if (Guid.TryParse(Mask, out uid))
            {
                IMalObject mo = viewModel.ServiceManager.DataSource.MainModelImage.GetObject(uid);
                if (mo != null) result.Add(mo);
            }
            else
            {
                string maskLower = string.IsNullOrEmpty(Mask) ? null : Mask.ToLower();
                foreach (MetaClass metaClass in viewModel.FilterClasses)
                {
                    IMalObject[] objs = viewModel.ServiceManager.DataSource.MainModelImage.GetObjects(metaClass);
                    result.AddRange(objs.Where(mo =>
                    {
                        string name = mo.GetName();
                        if (string.IsNullOrEmpty(name)) return false;
                        name = name.ToLower();
                        bool include = name.Contains(maskLower);
                        if (!include && IsFuzzySearch)
                        {
                            //если разрешен нечеткий поиск
                            DLDistance dist = LevenshteinSort.DamerauLevenshteinDistance(maskLower, name);
                            include = dist.MatchCount >= maskLower.Length * 0.8 && dist.ErrorCount <= 2;
                        }
                        return include;
                    }));
                }
            }
            return result;
        }

        private void cmnPath_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsControl itemsCtrl = sender as ItemsControl;
            InitPathMenu(itemsCtrl);
        }

        private void InitPathMenu(ItemsControl itemsCtrl)
        {
            if (itemsCtrl == null) return;
            List<MenuItem> delItems = new List<MenuItem>();
            foreach (MenuItem item in itemsCtrl.Items.Cast<MenuItem>())
            {
                item.Loaded -= cmnPath_Loaded;
                if (item.Tag is int)
                {
                    int i = (int)item.Tag;
                    if (i < 0) item.Click -= miPrev_Click;
                    else item.Click -= miNext_Click;
                }
                else
                    item.Click -= miPath_Click;
                delItems.Add(item);
            }

            ModelItem modelItem = itemsCtrl.DataContext as ModelItem;
            int pageNum = 0;
            if (dicPageNums.ContainsKey(modelItem))
                pageNum = dicPageNums[modelItem];
            ModelItem[] childItems = modelItem.Items.Values.Where(_mi => !_mi.IsLeaf).OrderByDescending(mi => mi.IsFolder).ThenBy(mi => mi.Name).ToArray();
            int startNum = pageNum * pathMenuPageLength;
            int stopNum = startNum + pathMenuPageLength;
            int length = childItems.Length;
            bool isPrev = false;
            for (int i = 0; i < length; i++)
            {
                ModelItem child = childItems[i];
                if (!isPrev && pageNum > 0)
                {
                    isPrev = true;
                    //пункт меню для предыдущей страницы
                    MenuItem miPrev = new MenuItem()
                    {
                        Header = string.Format(LocalizationManager.GetString("previous"), pathMenuPageLength, startNum),
                        StaysOpenOnClick = true,
                        DataContext = modelItem,
                        Tag = -1
                    };
                    miPrev.Click += miPrev_Click;
                    itemsCtrl.Items.Add(miPrev);
                }
                if (i >= startNum && i < stopNum)
                {
                    ExtendedMenuItem mi = new ExtendedMenuItem()
                    {
                        Header = child.Name,
                        Icon = new Image() { Source = child.Image },
                        StaysOpenOnClick = true,
                        DataContext = child
                    };
                    if (child.HasItems) mi.Loaded += cmnPath_Loaded;
                    mi.Click += miPath_Click;
                    itemsCtrl.Items.Add(mi);
                }
                if (i >= stopNum)
                {
                    //пункт меню для следующей страницы
                    int last = length - stopNum;
                    MenuItem miNext = new MenuItem()
                    {
                        Header = string.Format(LocalizationManager.GetString("next"), pathMenuPageLength < last ? pathMenuPageLength : last, last),
                        StaysOpenOnClick = true,
                        DataContext = modelItem,
                        Tag = 1
                    };
                    miNext.Click += miNext_Click;
                    itemsCtrl.Items.Add(miNext);
                    break;
                }
            }

            foreach (MenuItem item in delItems)
                itemsCtrl.Items.Remove(item);
        }

        private void miPrev_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            ReloadPathMenuPage(item, PageDirection.Previous);
        }

        private void miNext_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            ReloadPathMenuPage(item, PageDirection.Next);
            e.Handled = true;
        }

        private int UpdatePageNum(MenuItem menuItem, int summand)
        {
            int pageNum = 0;
            bool isPage = false;
            ModelItem modelItem = menuItem.DataContext as ModelItem;
            if (dicPageNums.ContainsKey(modelItem))
            {
                pageNum = dicPageNums[modelItem];
                isPage = true;
            }
            pageNum += summand;
            if (isPage)
                dicPageNums[modelItem] = pageNum;
            else
                dicPageNums.Add(modelItem, pageNum);
            return pageNum;
        }

        private void miPath_Click(object sender, RoutedEventArgs e)
        {
            ModelItem modelItem = (sender as MenuItem).DataContext as ModelItem;
            viewModel.CurrentItem = modelItem;
            CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
            ContextMenu menu = FindParentMenu(sender as FrameworkElement);
            if (menu != null)
                menu.IsOpen = false;
            if (viewKind != ViewKinds.Flat)
                treeControl.LocateObject(modelItem.MalObject.Id);
        }

        private ContextMenu FindParentMenu(FrameworkElement item)
        {
            FrameworkElement parent = item.Parent as FrameworkElement;
            while (parent != null && !(parent is ContextMenu))
                parent = parent.Parent as FrameworkElement;
            return parent as ContextMenu;
        }

        private void ReloadPathMenuPage(MenuItem menuItem, PageDirection direction)
        {
            int pageNum;
            if (direction == PageDirection.Previous)
                pageNum = UpdatePageNum(menuItem, -1);
            else
                pageNum = UpdatePageNum(menuItem, 1);

            ItemsControl itemsCtrl = menuItem.Parent as ItemsControl;
            ModelItem modelItem = menuItem.DataContext as ModelItem;

            //ухищрения, чтобы контекстное меню не пропало после перерисовки новой страницы
            //необходимо, чтобы после перерисовки под курсором мыши был пункт меню
            bool needMouseMove = false;
            Point mousePoint = new Point();
            DependencyObject dobj = VisualTreeHelper.GetParent(menuItem);
            while (!(dobj == null || dobj is StackPanel))
                dobj = VisualTreeHelper.GetParent(dobj);
            StackPanel panel = dobj as StackPanel;
            if (panel != null)
            {
                //ставим мышь в середину панели по горизонтали
                Point mouseP = Mouse.GetPosition(panel); // текущее положение мыши
                double nx = panel.ActualWidth / 2;
                mousePoint = panel.PointToScreen(new Point(nx, mouseP.Y));
                needMouseMove = true;
            }

            if (direction == PageDirection.Next)
            {
                //если переход на следующую страницу:
                //найти пункт меню, индекс которого соответствует последнему пункту меню на следующей странице
                //если курсор мыши ниже, поднять его вверх до этого пункта
                int itemIndex = modelItem.Items.Count(kvp => !kvp.Value.IsLeaf) - pageNum * pathMenuPageLength;
                if (itemIndex < itemsCtrl.Items.Count)
                {
                    MenuItem lastItem = itemsCtrl.Items.GetItemAt(itemIndex) as MenuItem;
                    Point mouseP = Mouse.GetPosition(lastItem);
                    if (mouseP.Y > lastItem.ActualHeight - 5)
                    {
                        Point ps = lastItem.PointToScreen(new Point(mouseP.X, lastItem.ActualHeight - 5));
                        mousePoint.Y = ps.Y;
                        if (!needMouseMove)
                        {
                            mousePoint.X = ps.X;
                            needMouseMove = true;
                        }
                    }
                }
            }
            if (needMouseMove) SetCursorPos(mousePoint);
            InitPathMenu((menuItem as ItemsControl).Parent as ItemsControl);
        }

        private void btnPathItem_Click(object sender, RoutedEventArgs e)
        {
            ModelItem modelItem = (sender as FrameworkElement).DataContext as ModelItem;
            viewModel.CurrentItem = modelItem;
            CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
            if (viewKind != ViewKinds.Flat)
                treeControl.LocateObject(modelItem.MalObject.Id);
        }

        private void OnObjectClick(IMalObject malObject)
        {
            ObjectClick?.Invoke(this, new MalObjectEventArgs(malObject));
        }

        private void OnObjectDblClick(IMalObject malObject)
        {
            ObjectDblClick?.Invoke(this, new MalObjectEventArgs(malObject));
        }

        private void OnObjectMenuOpening(IMalObject malObject, ContextMenu menu)
        {
            ObjectMenuOpening?.Invoke(this, new ObjectMenuOpeningEventArgs(malObject, menu));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void OnCurrentItemsChanged()
        {
            CurrentItemsChanged?.Invoke(this, new EventArgs());
        }

        internal void OnObjectChecked(IMalObject malObject)
        {
            ObjectChecked?.Invoke(this, new MalObjectEventArgs(malObject));
        }

        internal void OnObjectUnchecked(IMalObject malObject)
        {
            ObjectUnchecked?.Invoke(this, new MalObjectEventArgs(malObject));
        }

        private void OnListKeyUp(KeyEventArgs e)
        {
            ListKeyUp?.Invoke(this, e);
        }

        private void OnBeginDrag(IMalObject malObject)
        {
            BeginDrag?.Invoke(this, new MalObjectEventArgs(malObject));
        }

        private object itemTemplateMouseDowned = null;

        private void ItemTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            itemTemplateMouseDowned = sender;
            mouseDownPos = e.MouseDevice.GetPosition(this);
        }

        private void ItemTemplate_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender == itemTemplateMouseDowned && sender is FrameworkElement fe && fe.DataContext is ModelItem mi)
            {
                itemTemplateMouseDowned = null;
                itemClicked = mi;
                itemDblClickTimer.Change(System.Windows.Forms.SystemInformation.DoubleClickTime, Timeout.Infinite);
                itemClickCount++;
            }
            mouseDownPos = null;
        }

        private void ItemTemplate_MouseMove(object sender, MouseEventArgs e)
        {
            if (allowDrag && mouseDownPos.HasValue && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mouseP = e.MouseDevice.GetPosition(this);
                if (Math.Abs(mouseP.X - mouseDownPos.Value.X) > dragShift || Math.Abs(mouseP.Y - mouseDownPos.Value.Y) > dragShift)
                {
                    ModelItem item = (sender as FrameworkElement).DataContext as ModelItem;
                    if (!(item == null || item.MalObject == null))
                    {
                        OnBeginDrag(item.MalObject);
                        mouseDownPos = null;
                    }
                }
            }
        }

        private void tbMask_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Mask))
            {
                tbMask.Visibility = Visibility.Collapsed;
                tbMaskPrompt.Visibility = Visibility.Visible;
            }
        }

        private void tbMask_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => tbMask.SelectAll()), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void ItemTemplate_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu menu = (sender as FrameworkElement).ContextMenu;
            ModelItem modelItem = (sender as FrameworkElement).DataContext as ModelItem;
            if (modelItem != null)
            {
                ObjectContextMenuOpening(menu, modelItem.MalObject);
                if (!string.IsNullOrEmpty(modelItem.Path))
                {
                    if (menu.Items.Count > 0)
                        menu.Items.Add(new Separator());
                    MenuItem mi = new MenuItem()
                    {
                        Header = LocalizationManager.GetString("setPath"),
                        DataContext = modelItem
                    };
                    mi.Click += miSetPath_Click;
                    menu.Items.Add(mi);
                }
            }
            e.Handled = menu.Items.Count == 0;
        }

        private void ObjectContextMenuOpening(ContextMenu menu, IMalObject malObject)
        {
            foreach (MenuItem mi in menu.Items.OfType<MenuItem>())
            {
                mi.Click -= miSetPath_Click;
                mi.Click -= ItemIsTreeView_Click;
            }
            menu.Items.Clear();
            OnObjectMenuOpening(malObject, menu);
            AddIsTreeViewItem(menu);
        }

        private void AddIsTreeViewItem(ContextMenu menu)
        {
            if (ViewKind == ViewKinds.Both)
            {
                if (menu.HasItems) menu.Items.Add(new Separator());
                MenuItem item = new MenuItem();
                if (IsTreeView) item.Header = viewKindNameFlat;
                else item.Header = viewKindNameTree;
                item.Click += ItemIsTreeView_Click;
                menu.Items.Add(item);
            }
        }

        private void ItemIsTreeView_Click(object sender, RoutedEventArgs e)
        {
            IsTreeView = !IsTreeView;
        }

        private void miSetPath_Click(object sender, RoutedEventArgs e)
        {
            ModelItem modelItem = (sender as MenuItem).DataContext as ModelItem;
            viewModel.CurrentItem = modelItem.Parent;
            CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
            if (viewKind != ViewKinds.Flat)
                treeControl.LocateObject(viewModel.CurrentItem.MalObject.Id);
        }

        private void tbMaskPrompt_GotFocus(object sender, RoutedEventArgs e)
        {
            FocusToMaskEdit();
        }

        private void lbItems_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is ModelItem item)
            {
                if (IsCheckVisible && e.Key == Key.Space)
                    item.IsChecked = !item.IsChecked;
                else if (e.Key == Key.Enter)
                    OnObjectClick(item.MalObject);
                OnListKeyUp(e);
            }
        }

        private void treeControl_SelectedObjectChanged(object sender, ObjectEventArgs e)
        {
            ModelItem item = viewModel.FindModelItem(e.ObjectId);
            if (item != null)
            {
                if (item.IsLeaf) item = item.Parent;
                if (viewModel.CurrentItem != item)
                {
                    viewModel.CurrentItem = item;
                    CollectionViewSource.GetDefaultView(lbItems.ItemsSource).Refresh();
                }
            }
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
                FocusToMaskEdit();
            e.Handled = true;
        }

        private void treeControl_StartDrag(object sender, ObjectListStartDragEventArgs e)
        {
            if (e.ObjectIdList.Any()
                && viewModel.ServiceManager.DataSource.MainModelImage.GetObject(e.ObjectIdList.First()) is IMalObject mObj)
            {
                OnBeginDrag(mObj);
                e.Handled = true;
            }
        }
    }

    internal enum PageDirection
    {
        Previous,
        Next
    }

    public class MalObjectEventArgs : EventArgs
    {
        public IMalObject MalObject { get; private set; }
        public MalObjectEventArgs(IMalObject malObject)
        {
            MalObject = malObject;
        }
    }

    public class ObjectMenuOpeningEventArgs : EventArgs
    {
        public IMalObject MalObject { get; private set; }
        public ContextMenu Menu { get; private set; }
        public ObjectMenuOpeningEventArgs(IMalObject malObject, ContextMenu menu)
        {
            MalObject = malObject;
            Menu = menu;
        }
    }

    public class ModelItemContainerStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            Style result = null;
            ModelObjectSelectControl ctrl = null;
            if (!(item == null || container == null))
            {
                DependencyObject parent = VisualTreeHelper.GetParent(container);
                while (!(parent == null || parent is ModelObjectSelectControl))
                    parent = VisualTreeHelper.GetParent(parent);
                if (parent != null)
                {
                    ctrl = parent as ModelObjectSelectControl;
                    string name = null;
                    ModelItem mItem = item as ModelItem;
                    if (mItem != null) name = mItem.Name;
                    if (string.IsNullOrEmpty(name))
                        result = Application.Current.TryFindResource("lbItemTemplDisableStyle") as Style;
                    else
                    {
                        if (ctrl.IsCompactView)
                            result = Application.Current.TryFindResource("lbItemTemplStyle") as Style;
                        else
                            result = Application.Current.TryFindResource("lbItemPressTemplStyle") as Style;
                    }
                }
            }
            return result;

        }
    }

    public class ModelItemTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate result = base.SelectTemplate(item, container);
            ModelObjectSelectControl ctrl = null;
            if (container != null)
            {
                DependencyObject parent = VisualTreeHelper.GetParent(container);
                while (!(parent == null || parent is ModelObjectSelectControl))
                    parent = VisualTreeHelper.GetParent(parent);
                if (parent != null)
                {
                    ctrl = parent as ModelObjectSelectControl;
                    if (ctrl.IsCompactView)
                        result = ctrl.Resources["modelItemCompactTemplate"] as DataTemplate;
                    else
                        result = ctrl.Resources["modelItemTemplate"] as DataTemplate;
                }
            }
            return result;
        }
    }

}


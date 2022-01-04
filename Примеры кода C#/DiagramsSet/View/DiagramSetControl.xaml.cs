using Monitel.Localization;
using Monitel.UI.Infrastructure.Services;
using Monitel.UI.WPFExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Monitel.SCADA.UICommon.DiagramsSet.View
{
    /// <summary>
    /// Диалог дерева наборов
    /// </summary>
    public partial class DiagramSetControl : UserControl, INotifyPropertyChanged
    {
        #region glob

        private TreeViewItem _selectedTreeItem;
        private ObservableCollection<IDiagramItem> _treeItems;
        private DSetStore _store;
        private Dictionary<ContentControl, IDiagramItem> _editBoxes = new Dictionary<ContentControl, IDiagramItem>();
        private Diagram _selectOnStart;
        private DiagramItem[] _commonGroup;
        private DiagramItem[] _userGroup;

        #endregion

        #region Properties

        /// <summary>
        /// Дерево наборов
        /// </summary>
        public ObservableCollection<IDiagramItem> TreeItems
        {
            get
            {
                if (_treeItems == null)
                    _treeItems = new ObservableCollection<IDiagramItem>();

                return _treeItems;
            }
            private set
            {
                _treeItems = value;
                DoPropertyChanged("TreeItems");
            }
        }

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
           "SelectedItem", typeof(IDiagramItem), typeof(DiagramSetControl), null);

        /// <summary>
        /// Выбранный элемент дерева
        /// </summary>
        public IDiagramItem SelectedItem
        {
            get { return (IDiagramItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }


        public static readonly DependencyProperty ServiceProperty = DependencyProperty.Register(
            "Services", typeof(IServiceManager), typeof(DiagramSetControl),
            new PropertyMetadata(DependencyPropertyChanged));

        /// <summary>
        /// Менеджер
        /// </summary>
        public IServiceManager Services
        {
            get { return (IServiceManager)GetValue(ServiceProperty); }
            set { SetValue(ServiceProperty, value); }
        }

        private static void DependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (DiagramSetControl)d;

            if (e.Property == ServiceProperty)
                ctrl.Init((IServiceManager)e.NewValue);
        }

        #endregion

        #region Init

        public DiagramSetControl()
        {
            InitializeComponent();

            mainGrid.DataContext = this;
        }

        /// <summary>
        /// Задать менеджер служб для контрола
        /// </summary>
        /// <param name="service"></param>
        public void Init(IServiceManager service)
        {
            if (service != null)
            {
                _store = new DSetStore(service.SettingsService.SettingsManager);

                TreeItems = new ObservableCollection<IDiagramItem>(GetTree());

                if (_selectOnStart != null && _commonGroup != null && _userGroup != null)
                    foreach (var ch in _commonGroup.Union(_userGroup))
                    {
                        if (ch.UID == _selectOnStart.UID)
                        {
                            ch.IsSelected = true;
                            break;
                        }
                    }
            }
        }

        #endregion

        #region methods

        private static T FindAnchestor<T>(DependencyObject current)
         where T : DependencyObject
        {
            do
            {
                if (current is T)
                    return (T)current;

                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);

            return null;
        }

        #region tree init

        private IEnumerable<IDiagramItem> MakeTree(IEnumerable<DiagramItem> ls)
        {
            var res = new List<IDiagramItem>();

            foreach (var item in ls)
            {
                if (!String.IsNullOrEmpty(item.Path))
                {
                    var path = item.Path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    var root = res.FirstOrDefault(x => x.Name.Equals(path[0])) as FolderItem;

                    if (root == null)
                    {
                        root = new FolderItem(path[0], _store) { AccessLayer = item.AccessLayer };
                        res.Add(root);
                    }

                    FolderItem parent = root;

                    for (var i = 1; i < path.Count; i++)
                    {
                        parent = root.Items.FirstOrDefault(x => x is FolderItem && x.Name == path[i]) as FolderItem;

                        if (parent == null)
                        {
                            parent = new FolderItem(path[i], _store) { AccessLayer = item.AccessLayer };
                            root.AddItem(parent);
                        }

                        root = parent;
                    }

                    parent.AddItem(item);
                }
                else
                    res.Add(item);
            }

            return res;
        }

        /// <summary>
        /// Запросить все дерево наборов
        /// </summary>
        /// <returns>Вернет две папки Общие и Пользовательские</returns>
        public IEnumerable<IDiagramItem> GetTree()
        {
            var usersFl = new FolderItem(LocalizationManager.GetString("custom"), _store) { AccessLayer = AccessLayer.User, IsRoot = true };
            var commonFl = new FolderItem(LocalizationManager.GetString("common"), _store) { AccessLayer = AccessLayer.Common, IsRoot = true };

            _commonGroup = _store.GetDiagrams(AccessLayer.Common).Select(x => new DiagramItem(_store, x)).ToArray();
            _userGroup = _store.GetDiagrams(AccessLayer.User).Select(x => new DiagramItem(_store, x)).ToArray();


            foreach (var item in MakeTree(_commonGroup))
                commonFl.AddItem(item);

            foreach (var item in MakeTree(_userGroup))
                usersFl.AddItem(item);

            return new IDiagramItem[] { commonFl, usersFl };
        }

        /// <summary>
        /// Создать папку в дереве
        /// </summary>
        /// <param name="parent">Родительская папка</param>
        /// <param name="name">Наименование папки</param>
        /// <returns>Объект Folder</returns>
        public FolderItem GetNewFolder(FolderItem parent, string name)
        {
            var fl = new FolderItem(name, _store);

            if (parent != null)
                parent.AddItem(fl);

            return fl;
        }

        public void SelectItem(Diagram item)
        {
            _selectOnStart = item;
        }

        #endregion

        #endregion

        #region Handlers

        private void treeMenuOpen(object sender, RoutedEventArgs e)
        {
            var menu = sender as ContextMenu;

            menu.DataContext = this;
        }

        private void OnPreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tmp = FindAnchestor<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (tmp != null && _selectedTreeItem != tmp)
            {
                _selectedTreeItem = tmp;
                _selectedTreeItem.Focus();
            }
        }

        private void treeItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedItem = ((TreeView)sender).SelectedItem as IDiagramItem;
        }

        #region Rename handlers

        private void NameCtrlLoaded(object sender, RoutedEventArgs e)
        {
            var ctrl = sender as ContentControl;

            _editBoxes.Add(ctrl, ctrl.Tag as IDiagramItem);
        }

        private void TreeItemPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                var item = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (item == null)
                    return;

                SetTemplate(item, "editedBox");

                e.Handled = true;
            }
        }

        private void TbEdit_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                if (e.Key == Key.Escape)
                {
                    var tb = sender as TextBox;

                    if (tb != null)
                        tb.Undo();
                }

                var parent = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (parent != null)
                    parent.Focus();
            }
            else if (e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
            }
        }

        private void TbEdit_OnLoaded(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;

            tb.Focus();
            tb.SelectAll();
            tb.KeyUp += TbEdit_KeyUp;
            tb.LostFocus += tb_LostFocus;
        }

        private void tb_LostFocus(object sender, RoutedEventArgs e)
        {
            var item = sender as TextBox;

            item.LostFocus -= tb_LostFocus;
            item.KeyUp -= TbEdit_KeyUp;

            var treeItem = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);

            SetTemplate(treeItem, "notEditedBox");
        }

        private void SetTemplate(TreeViewItem node, string tmpName)
        {
            var item = (IDiagramItem)node.Header;

            var exist = _editBoxes.FirstOrDefault(x => x.Value.Equals(item));

            if (exist.Key != null)
            {
                var templ = FindResource(tmpName) as ControlTemplate;

                if (templ != null)
                    exist.Key.Template = templ;
            }
        }

        #endregion

        #endregion

        #region Commands

        private RelayCommand _renameDiagram;

        /// <summary>
        /// Добавить элемент в родительский 
        /// </summary>
        public ICommand RenameDiagram
        {
            get
            {
                return _renameDiagram ?? (_renameDiagram = new RelayCommand(
                                                                  e =>
                                                                  {
                                                                      SetTemplate(_selectedTreeItem, "editedBox");
                                                                  },
                                                                  e =>
                                                                  {
                                                                      if (_selectedTreeItem == null)
                                                                          return false;

                                                                      if (_selectedTreeItem.Header is FolderItem && ((FolderItem)_selectedTreeItem.Header).IsRoot)
                                                                          return false;

                                                                      return true;
                                                                  }));
            }
        }

        private RelayCommand _createFolder;

        /// <summary>
        /// Добавить элемент в родительский 
        /// </summary>
        public ICommand CreateFolder
        {
            get
            {
                return _createFolder ?? (_createFolder = new RelayCommand(
                                                                  e =>
                                                                  {
                                                                      var fl = GetNewFolder(e as FolderItem, "");
                                                                  },
                                                                  e =>
                                                                  {
                                                                      return _selectedTreeItem != null && _selectedTreeItem.Header is FolderItem;
                                                                  }));
            }
        }

        private RelayCommand _removeFolder;

        /// <summary>
        /// Удалить элемент 
        /// </summary>
        public ICommand RemoveFolder
        {
            get
            {
                return _removeFolder ?? (_removeFolder = new RelayCommand(
                                                                  e =>
                                                                  {
                                                                      if (MessageBox.Show(Localization.LocalizationManager.GetString("msgRemoveDiagramFolderYN"), LocalizationManager.GetString("attention"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                                                          ((IDiagramItem)e).Remove();
                                                                  },
                                                                  e =>
                                                                  {
                                                                      if (_selectedTreeItem == null || !(_selectedTreeItem.Header is FolderItem))
                                                                          return false;

                                                                      if (_selectedTreeItem.Header is FolderItem && ((FolderItem)_selectedTreeItem.Header).IsRoot)
                                                                          return false;

                                                                      return true;
                                                                  }));
            }
        }

        private RelayCommand _removeDiagram;

        /// <summary>
        /// Добавить элемент в родительский 
        /// </summary>
        public ICommand RemoveDiagram
        {
            get
            {
                return _removeDiagram ?? (_removeDiagram = new RelayCommand(
                                                                  e =>
                                                                  {
                                                                      if (MessageBox.Show(Localization.LocalizationManager.GetString("msgRemoveDiagramYN"), LocalizationManager.GetString("attention"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                                                          ((IDiagramItem)e).Remove();
                                                                  },
                                                                  e =>
                                                                  {
                                                                      return _selectedTreeItem != null;
                                                                  }));
            }
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

using System;
using System.Collections.Generic;
using Monitel.DataContext.Tools.AllowedTree;
using Monitel.DataContext.Tools.Icons;
using Monitel.DataContext.Tools.TreeProviders.Interfaces;
using Monitel.Mal;

namespace Monitel.SCADA.UICommon.SelectControl
{
    public class TreeProvider : ITreeDataProviderMal
    {
        private ModelObjectViewModel _viewModel;
        private IModelImage _modelImage;
        private IconsProvider _iconsProvider;
        private List<ITreeDescriptorHandle> _treeDescriptors = new List<ITreeDescriptorHandle>();

        internal TreeProvider(ModelObjectViewModel viewModel)
        {
            _viewModel = viewModel;
            _treeDescriptors.Add(new TreeDescriptor(viewModel, this));
        }

        #region ITreeDataProvider

        public string Title
        {
            get { return "FilterTree"; }
        }

        public event EventHandler<TreeDescriptorChangesEventArgs> TreesChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<ITreeDescriptorHandle> GetTreesDescriptors()
        {
            return _treeDescriptors;
        }

        public void SetDataSources(IModelImage modelImage, IconsProvider iconsProvider, AllowedTreeProvider allowedTree)
        {
            _modelImage = modelImage;
            _iconsProvider = iconsProvider;
        }

        public void SetNameType(Guid nameTypeUid)
        {

        }

        public void SetUseAliasMetaNames(bool value)
        {

        }

        public bool IsModelChangeProcessingDisabled
        {
            get;
            set;
        }

        public bool IsFilterSupported => false;

        public void SetFilter(IEnumerable<long> objects)
        {
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_treeDescriptors != null)
            {
                _treeDescriptors.Clear();
                _treeDescriptors = null;
                _iconsProvider = null;
                _modelImage = null;
            }
        }

        #endregion
    }
}

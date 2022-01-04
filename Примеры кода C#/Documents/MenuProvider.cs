using Monitel.Diogen.Core;
using Monitel.Diogen.Infrastructure.Extensions;
using Monitel.Extensions.Http;
using Monitel.OikDebugLoggerImp;
using Monitel.PlatformInfrastructure.Logger;
using Monitel.WS_Documents.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using lm = Monitel.Localization.LocalizationManager;
using ws = Monitel.WS_Documents.Client;

namespace Monitel.SCADA.UICommon.Documents
{

    public class MenuProvider
    {
        private IEnumerable<Guid> _objectsGuid;
        private Uri _unknownUriLink;
        private BitmapFrame _unknownImgLink;
        private CancellationTokenSource _tokenSource;
        private static string _pathToDowlaod;
        private ws.DocumentsClientProvider _docClient;
        private MenuItem _documentMenu;
        private IDocumentsSetting _documentSetting;

        private static HashSet<Guid> _allObjGuids = new HashSet<Guid>();
        private List<ws.DocumentSummary> _documents = new List<ws.DocumentSummary>();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);

        private IPlatformLogger _logger;

        private MenuProvider(IEnumerable<Guid> objectGuid, IPlatformLogger logger, IDocumentsSetting docSetting)
        {
            _objectsGuid = objectGuid;
            _documentSetting = docSetting;

            _logger = logger ?? throw new ArgumentNullException(nameof(MenuProvider));
        }

        /// <summary>
        /// Метод для создания Меню и его подменю. 
        /// </summary>
        /// <param name="objectGuid">список guid_ов энегрообъектов</param>
        /// <returns></returns>
        public static MenuItem GetMenuItems(IEnumerable<Guid> objectGuid, IPlatformLogger logger, IDocumentsSetting docSetting)
            => new MenuProvider(objectGuid, logger, docSetting).CreateMenuItems();

        private MenuItem CreateMenuItems()
        {
            _documents?.Clear();
            _allObjGuids?.Clear();

            RotateTransform rotateTransform = new RotateTransform();

            DoubleAnimation anime = new DoubleAnimation()
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1.5),
                RepeatBehavior = RepeatBehavior.Forever
            };

            if (_objectsGuid == null) return null;

            var imageAnime = new Viewbox
            {
                Child = Icons.Loaded as FrameworkElement,
                Stretch = Stretch.None,
                RenderTransform = rotateTransform
            };

            _documentMenu = new MenuItem()
            {
                Header = lm.GetString("documents"),
                IsEnabled = false,
                Icon = imageAnime
            };

            //Для указания центра анимации
            var rotationTransforImage = imageAnime.Child as FrameworkElement;
            rotateTransform.CenterX = rotationTransforImage.Width / 2;
            rotateTransform.CenterY = rotationTransforImage.Height / 2;

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, anime);

            ToolTipService.SetShowOnDisabled(_documentMenu, true);
            _documentMenu.ToolTip = lm.GetString("ServiceIsUnavailable");

            _unknownUriLink = CoreHelpers.MakeURI(@"/Resources/document-template.png");
            _unknownImgLink = BitmapFrame.Create(_unknownUriLink, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            _documentMenu.Loaded += MainMenuItem_Loaded;
            _documentMenu.Unloaded += MainMenuItem_Unloaded;

            return _documentMenu;
        }

        private void MainMenuItem_Unloaded(object sender, RoutedEventArgs e)
        {
            _documents?.Clear();
            _allObjGuids?.Clear();
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
        }

        private async void MainMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _docClient = new ws.DocumentsClientProvider(_logger);
                GetPathToDownload();
                _tokenSource = new CancellationTokenSource();

                if (!_tokenSource.IsCancellationRequested)
                {
                    var cancelToken = _tokenSource.Token;
                    await FillDocumentsAsync(cancelToken);
                }

            }
            catch (Exception ex)
            {
                _documents = null;

                if (ex is TaskCanceledException)
                    _logger?.Write(LogCategory.Error, LogPriority.Medium, $"WS_Documetns - MainMenuItem_Loaded\r\n{ex.DetailedMessage()}");
                else
                    _logger?.Write(LogCategory.Error, LogPriority.Medium, $"WS_Documetns - MainMenuItem_Loaded\r\n{ex.DetailedMessage()}");
            }
        }

        private async Task FillDocumentsAsync(CancellationToken cancelToken)
        {
            var docRequest = new ws.GetDocumentsRequest()
            {
                CategoriesUids = new List<Guid>(),
                ObjectUids = (ICollection<Guid>)_objectsGuid,
                GroupingType = ws.GetDocumentsGroupingType.ByObject,
                RequestType = ws.GetDocumentsRequestType.IncludeAll,
                Type = ws.MonitelDocumentType.SCADADoc
            };

            var responce = await _docClient.GetMainClient().PostAllAsync(1000, 0, docRequest, cancelToken);

            if (cancelToken.IsCancellationRequested) return;

            _documents = new List<ws.DocumentSummary>();

            if (responce != null)
            {
                foreach (var docSumm in responce.Data)
                {
                    foreach (var summary in docSumm.Value)
                        _documents.Add(summary);

                    _allObjGuids.Add(new Guid(docSumm.Key));
                }
                CreateMainMenu();
            }
        }

        private void CreateMainMenu()
        {
            var uriLink = CoreHelpers.MakeURI(@"/Resources/MAG_LinkDoc_16x16.png");
            var imgLink = BitmapFrame.Create(uriLink, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            _documentMenu.Icon = new Image() { Source = imgLink };

            CreateSubMenu(_documents, _documentMenu.Items);

            _documentMenu.ToolTip = null;
            _documentMenu.IsEnabled = true;

            //Item для настроек документов 
            if (_documentSetting.IsNeedShowDialogSetting)
            {
                if (_documentMenu?.Items.Count != 0)
                    _documentMenu.Items.Add(new Separator());

                MenuItem settingMenu = new MenuItem()
                {
                    Header = lm.GetString("setting"),
                    Tag = lm.GetString("setting")
                };
                settingMenu.Click += SettingMenu_Click;
                _documentMenu.Items.Add(settingMenu);
            }
        }

        private void SettingMenu_Click(object sender, RoutedEventArgs e)
        {
            _documentSetting.Show();
        }

        private void CreateSubMenu(List<ws.DocumentSummary> documents, ItemCollection items)
        {
            MenuItem categoryItem = null;
            MenuItem newItem = null;
            bool isNeedSeparator = true;

            if (documents == null || _tokenSource.IsCancellationRequested) return;

            foreach (var summary in documents)
            {
                var source = (string.IsNullOrEmpty(summary.LocalURI)) ? summary.ExternalURI : summary.LocalURI;

                //категория
                bool isNotAny = items.OfType<MenuItem>().ToList().Where(t => t.Tag != null && t.Tag.Equals(summary.Category?.Uid)).Any();
                bool isNotobjGuid = summary.IdentifiedObject != null && summary.IdentifiedObject.Uid == _objectsGuid.FirstOrDefault();

                if (isNotobjGuid)
                    newItem = CreateSubMenuAndCategoriMenu(items, ref categoryItem, summary, source, isNotAny);
                else
                {
                    if (isNeedSeparator)
                    {
                        items.Add(new Separator());
                        isNeedSeparator = false;
                        isNotAny = false;
                    }
                    newItem = CreateSubMenuAndCategoriMenu(items, ref categoryItem, summary, source, isNotAny);
                }
            }
        }

        //Добавление категорий
        private MenuItem CreateSubMenuAndCategoriMenu(ItemCollection items, ref MenuItem categoryItem, DocumentSummary summary, string source, bool isNotAny)
        {
            MenuItem newItem;
            bool isCategoryExist = false;

            if (summary.Category != null && !isNotAny)
            {
                isCategoryExist = true;
                categoryItem = new MenuItem()
                {
                    Header = summary.Category.Name,
                    Tag = summary.Category.Uid
                };

            }
            else if (summary.Category != null)
                isCategoryExist = true;

            newItem = new MenuItem()
            {
                Header = summary.FileName,
                IsEnabled = !DocumentHelpers.IsLocked(source),
            };

            if (!string.IsNullOrWhiteSpace(summary.FileName))
            {
                newItem.Icon = new Image()
                {
                    Source = DocumentHelpers.GetIcon(summary.FileName, true),
                    Stretch = Stretch.None
                };
            }
            else
                newItem.Icon = new Image() { Source = _unknownImgLink };


            newItem.Tag = summary;
            newItem.Click += NewItem_Click;

            ToolTipService.SetShowOnDisabled(newItem, true);
            newItem.ToolTip = !newItem.IsEnabled ? lm.GetString("notaccess") :
                string.IsNullOrWhiteSpace(summary.ExternalURI) ? lm.GetFormated("DowlaodPath{0}", _pathToDowlaod) : null;

            if (isCategoryExist)
            {
                categoryItem?.Items.Add(newItem);

                if (!isNotAny)
                    items.Add(categoryItem);
            }
            else
                items.Add(newItem);
            return newItem;
        }

        private async void NewItem_Click(object sender, RoutedEventArgs e)
        {
            var document = (sender as MenuItem).Tag as ws.DocumentSummary;
            try
            {
                if (string.IsNullOrEmpty(document.LocalURI))
                    Process.Start(document.ExternalURI);
                else
                    await Download(document);
            }
            catch (Exception ex)
            {
                ErrorInfoShow(document, ex);
            }
        }

        private async Task Download(ws.DocumentSummary doc)
        {
            var cancelTokenSource = new CancellationTokenSource();
            var token = cancelTokenSource.Token;

            using var fileClient = new WS_FileStorage.Client.FileStorageClientProvider(_logger);
            try
            {
                await fileClient.GetMainClient().GetLocalAsync(doc.Uid, doc.CreatedAt.ToString("yyyy-MM-dd"), doc.FileName, "scadadocs", _pathToDowlaod, token);
                Process.Start($"{_pathToDowlaod}\\{doc.FileName}");
            }

            catch (Exception ex)
            {
                ErrorInfoShow(doc, ex);
            }
        }

        private static void GetPathToDownload()
        {
            if (string.IsNullOrWhiteSpace(_pathToDowlaod))
                SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero, out _pathToDowlaod);
        }
               
        public static HashSet<Guid> GetAllGuid()
        {
            return _allObjGuids;
        }

        private void ErrorInfoShow(ws.DocumentSummary document, Exception ex)
        {
            string exMessage = null;
            string fileInfo = null;

            if (ex is ClientRequestException<WS_FileStorage.Client.ProblemDetails> clientEx)
            {
                exMessage = $"{lm.GetString(clientEx.Result.Type)}\r\n{clientEx.Result.Detail}";
                _logger.Write(LogCategory.Error, LogPriority.Medium, "FileStorageClient",  clientEx.Message);
            }
            else if (ex.HResult == -2147467259)
            {
                exMessage = ex.Message;
                fileInfo = $"{_pathToDowlaod}\\{document.FileName}";
            }
            else
            {
                exMessage = ex.Message;
                _logger.Write(LogCategory.Error, LogPriority.Medium, "WS_Documetns", ex.Message);
            }

            var errorInfo = new ErrorInformation(exMessage, fileInfo);
            errorInfo.ShowDialog();
        }
    }
}

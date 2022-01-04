using Microsoft.Web.WebView2.Core;
using Monitel.PlatformInfrastructure.Logger;
using Monitel.PlatformInfrastructure.ResourceUids;
using Monitel.Supervisor.Client;
using Monitel.Supervisor.Infrastructure.Rpc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using wf = Microsoft.Web.WebView2.WinForms;

namespace Monitel.SCADA.UICommon.Documents
{
    /// <summary>
    /// Interaction logic for SettingsUC.xaml
    /// </summary>
    public partial class SettingsUC : UserControl
    {
        private const string magDocsEnjOdjUri = "/documents/magdocuments/?$auth_type=Negotiate#/enrgyobject/";
        private string _uriSource;
        private wf.WebView2 _webView2;

        public IPlatformLogger Logger { get; set; }

        public SettingsUC()
        {
            InitializeComponent();
            WindowsFormsHost host = new WindowsFormsHost();
            _webView2 = new wf.WebView2();
            host.Child = _webView2;
            gbWebView.Content = host;
            _uriSource = CreateUri();
            InitWebView2();
        }

        private async void InitWebView2()
          => await InitWV2Async();

        private string CreateUri()
        {
            using SupervisorClient _supervisorClient = new SupervisorClient(null);
            _supervisorClient.Connect();

            ISession session = _supervisorClient.GetSession(ResourceUids.BASE_WEB_PUBLIC_URL, Supervisor.RPC.Clew.HostRole.Master);
            if (session != null)
            {
                IServerResource resource = session.Resources.FirstOrDefault();
                if (resource != null)
                    return resource.ConnectionString + magDocsEnjOdjUri;
            }

            return null;
        }

        private async Task InitWV2Async()
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Monitel"));
                await _webView2.EnsureCoreWebView2Async(env);
            }
            catch (Exception ex)
            {
                Logger?.Write(LogCategory.Error, LogPriority.Medium, "Monitel.SCADA.UICommon.Documents - InitWebView2", null, ex.Message);
            }
        }

        public void ReleaseWebView2()
        {
            try
            {
                // без установки Visible = false падает Dispose
                _webView2.Visible = false;
                _webView2.Dispose();
            }
            catch (Exception ex)
            {
                Logger?.Write(LogCategory.Error, LogPriority.Medium, "Monitel.SCADA.UICommon.Documents - ReleaseWebView2", null, ex.Message);
            }
        }

        public void VewView2SetUri(Guid energyObjGuid)
        {
            _webView2.Source = new Uri($"{_uriSource}{energyObjGuid}");
        }
    }
}
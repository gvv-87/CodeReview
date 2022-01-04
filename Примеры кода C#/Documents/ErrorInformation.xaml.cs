using System.Diagnostics;
using System.Windows;

using lm = Monitel.Localization.LocalizationManager;


namespace Monitel.SCADA.UICommon.Documents
{
    /// <summary>
    /// Interaction logic for ErrorInformation.xaml
    /// </summary>
    public partial class ErrorInformation : Window
    {
        internal string ErrorInfo { get; set; }
        private string _fileInfo;

        internal ErrorInformation(string exceptionMessage, string fileInfo)
        {
            InitializeComponent();
            ErrorInfo = exceptionMessage;
            Title = lm.GetString("documents");
            _fileInfo = fileInfo;
            if (string.IsNullOrWhiteSpace(fileInfo))
            { 
                btnShowInFolder.Visibility = Visibility.Collapsed; 
                btnCancel.Visibility = Visibility.Visible;
            }

            DataContext = this;
        }

        private void ButtonShowInFolder_Click(object sender, RoutedEventArgs e)
        {
            Process ExplorerWindowProcess = new Process();
            ExplorerWindowProcess.StartInfo.FileName = "explorer.exe";
            ExplorerWindowProcess.StartInfo.Arguments = "/select,\"" + _fileInfo + "\"";
            ExplorerWindowProcess.Start();

            Close();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e) => Close();
        
    }
}

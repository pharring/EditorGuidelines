using System.Diagnostics;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace ColumnGuide
{
    /// <summary>
    /// Interaction logic for OldVsVersionDialog.xaml
    /// </summary>
    public partial class OldVsVersionDialog : DialogWindow
    {
        public OldVsVersionDialog()
        {
            InitializeComponent();
        }

        public bool DontShowAgain => _dontShowAgainCheckBox.IsChecked ?? false;

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}

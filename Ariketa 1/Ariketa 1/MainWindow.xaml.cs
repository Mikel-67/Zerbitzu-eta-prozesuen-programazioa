using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ariketa_1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void ftpProzesuaHasi(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("ftp.exe");
        }
        private void killProzesua(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process[] procesos = System.Diagnostics.Process.GetProcessesByName("ftp");
            if (procesos != null)
            {
                label.Content = $"Prozesu eliminatuta: {procesos[0].ProcessName}";
                foreach (var proceso in procesos)
                {
                    proceso.Kill();
                }
            }
            else
            {
                label.Content = "Ez dago ftp prozesurik martxan";
            }
        }
        private void showInfoProzesua(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process[] procesos = System.Diagnostics.Process.GetProcesses();
            ComboBox.Items.Clear();
            foreach (var proceso in procesos)
            {
                ComboBox.Items.Add($"ID: {proceso.Id} - Nombre: {proceso.ProcessName} - Memoria: {proceso.WorkingSet64}");
            }
        }
    }
}
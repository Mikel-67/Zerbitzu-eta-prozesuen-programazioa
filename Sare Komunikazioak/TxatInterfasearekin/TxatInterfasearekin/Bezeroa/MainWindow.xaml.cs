using System.IO;
using System.Net.Sockets;
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

namespace Bezeroa
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string ip = "";
        private int port = 13000;
        TcpClient client;
        private string izena = "";
        private NetworkStream stream = null;
        private StreamReader reader = null;
        private StreamWriter writer = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BezeroaKonektatu(object sender, RoutedEventArgs e)
        {
            mainList.Items.Clear();
            try
            {
                //Konexioa egin
                client = new TcpClient();
                ip = ipTextBox.Text;
                client.Connect(ip, port);
                izena = izenaTextBox.Text;
                mainList.Items.Add("Bezeroa konektatuta: " + izena);

                //Errorea eman dezaketen opzioak kendu
                izenaTextBox.IsEnabled = false;
                ipTextBox.IsEnabled = false;
                KonexioBtn.IsEnabled = false;
                DeskonexioBtn.IsEnabled = true;
                MezuaBidaliBtn.IsEnabled = true;

                stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                writer.WriteLine(izena);

                int count = int.Parse(reader.ReadLine());

                for (int i = 0; i < count; i++)
                {
                    string mezua = reader.ReadLine();
                    mainList.Items.Add(mezua);
                }
                Thread mezuakJaso = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            string mezua = reader.ReadLine();
                            if (mezua == "Zerbitzaria deskonektatu da")
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    mainList.Items.Add("Zerbitzaria deskonektatu da");
                                    BezeroaDeskonektatu(null, null);
                                });
                                break;
                            }
                            Dispatcher.Invoke(() =>
                            {
                                mainList.Items.Add(mezua);
                            });
                        }
                        catch (IOException)
                        {
                            break;
                        }
                    }
                });
                mezuakJaso.Start();
            }
            catch (SocketException ex)
            {
                mainList.Items.Add("Errorea konexioan: Zerbitzaria itzalita dago");
            }
        }

        private void BezeroaDeskonektatu(object sender, RoutedEventArgs e)
        {
            string itzali = "/exit";
            try
            {
                writer.WriteLine(itzali);
            }catch (IOException)
            {
                //Zerbitzaria itzalita dago
            }


            //Itzali konexioa
            client.Close();
            mainList.Items.Add(izena+" deskonektatu da.");

            //Hasierako egoeraraa bueltatu
            izenaTextBox.IsEnabled = true;
            ipTextBox.IsEnabled = true;
            KonexioBtn.IsEnabled = true;
            DeskonexioBtn.IsEnabled = false;
            MezuaBidaliBtn.IsEnabled = false;
        }
        private void BidaliMezua(object sender, RoutedEventArgs e)
        {
            try
            {
                string mezua = mezuaTextBox.Text;
                writer.WriteLine(mezua);
                mezuaTextBox.Clear();
            }
            catch (IOException)
            {
                mainList.Items.Add("Errorea mezua bidaltzerakoan: Zerbitzaria itzalita dago");
                BezeroaDeskonektatu(null,null);
            }
        }
    }
}
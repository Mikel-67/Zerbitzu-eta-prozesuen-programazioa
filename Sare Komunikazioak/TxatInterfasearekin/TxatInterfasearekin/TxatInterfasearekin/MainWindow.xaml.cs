using System.IO;
using System.Net;
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

namespace TxatInterfasearekin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Mezuak 
        { 
            public string Izena { get; set; }
            public string Mezua { get; set; }
            public DateTime denbora { get; set; }
            public Mezuak(string izena, string mezua)
            {
                Izena = izena;
                Mezua = mezua;
                denbora = DateTime.Now;
            }
            public override string ToString()
            {
                return $"[{denbora.ToString()}] {Izena}: {Mezua}";
            }
        }
        public class Txata
        {
            public List<Mezuak> mezuak = new List<Mezuak>();
            public List<StreamWriter> writerList;
            public Txata(List<Mezuak> mezuakLista)
            {
                mezuak = mezuakLista;
                writerList = new List<StreamWriter>();
            }
        }

        private IPAddress ip = new IPAddress(new byte[] { 127, 0, 0, 1 });
        private int port = 13000;
        private TcpListener tcpListener;
        private Thread zerbitzariharia;
        private List<Mezuak> mezuaLista;
        private Txata txata = new Txata(null);
        public MainWindow()
        {
            InitializeComponent();
            tcpListener = new TcpListener(ip, port);
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (tcpListener.Server != null && tcpListener.Server.IsBound)
            {
                foreach (StreamWriter writer in txata.writerList)
                {
                    writer.WriteLine("Zerbitzaria deskonektatu da");
                }
                tcpListener.Stop();
                Dispatcher.Invoke(() => mainList.Items.Add("Zerbitzaria itzalita"));
            }
        }

        private void ZerbitzariaPiztu(object sender, RoutedEventArgs e)
        {
            zerbitzariharia = new Thread(() =>
            {
                tcpListener.Start();
                Dispatcher.Invoke(() => mainList.Items.Add("Zerbitzaria piztuta: IP: " + ip + " Portua: " + port));

                mezuaLista = new List<Mezuak>();
                txata = new Txata(mezuaLista);

                while (true)
                {
                    try
                    {
                        TcpClient client = tcpListener.AcceptTcpClient();
                        Thread haria = new Thread(() => funtzioa(client));
                        haria.Start();
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                }
            });
            zerbitzariharia.Start();
        }

        private void ZerbitzariaItzali(object sender, RoutedEventArgs e)
        {
            foreach (StreamWriter writer in txata.writerList)
            {
                writer.WriteLine("Zerbitzaria deskonektatu da");
            }
            tcpListener.Stop();
            mainList.Items.Add("Zerbitzaria itzalita");
        }

        private void funtzioa(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);
            using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            string izena = reader.ReadLine();
            try
            {
                Dispatcher.Invoke(() => 
                {
                    mainList.Items.Add(izena + " konektatu da.");
                    UserList.Items.Add(izena);
                    UserKop.Text = UserList.Items.Count.ToString();
                });
            }
            catch (TaskCanceledException e)
            {
                //Itzalita dago
            }
            
            txata.writerList.Add(writer);

            //Txata ikusiarazi bezeroa konektatzean
            int count = txata.mezuak.Count;

            writer.WriteLine(count);

            foreach (Mezuak mezua in txata.mezuak)
            {
                writer.WriteLine(mezua.ToString());
            }

            while (true)
            {
                try
                {
                    string mezua = reader.ReadLine();
                    if (mezua != "/exit")
                    {
                        //Txataren funtzioa
                        Mezuak newMezua = new Mezuak(izena, mezua);
                        txata.mezuak.Add(newMezua);
                        Dispatcher.Invoke(() =>
                        {
                            mainList.Items.Add(newMezua.ToString());
                        });
                        //Mezua bidali bezerorik guztiei
                        foreach (StreamWriter w in txata.writerList)
                        {
                            w.WriteLine(newMezua.ToString());
                        }
                    }
                    else
                    {
                        throw new IOException();
                    }
                }
                catch (IOException)
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            mainList.Items.Add(izena + " deskonektatu da.");
                            UserList.Items.Remove(izena);
                            UserKop.Text = UserList.Items.Count.ToString();
                        });
                    }catch(TaskCanceledException e)
                    {
                        //Itzalita
                    }
                    txata.writerList.Remove(writer);
                    break;
                }
            }
        }
    }
}
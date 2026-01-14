using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Zerbitzaria
{
    public class Zerbitzaria
    {
        public class Bezeroak
        {
            private int playerZnb;
            private List<string> eskua;
            private NetworkStream stream;
            private StreamWriter playerWriter;
            private StreamReader playerReader;
            private TcpClient client;

            public Bezeroak(int playerZnb, TcpClient client)
            {
                this.playerZnb = playerZnb;
                this.client = client;
                eskua = new List<string>();
                stream = client.GetStream();
                playerWriter = new StreamWriter(stream) { AutoFlush = true };
                playerReader = new StreamReader(stream);
            }

            public int PlayerZnb => playerZnb;
            public TcpClient Client => client;
            public List<string> Eskua => eskua;
            public StreamWriter PlayerWriter => playerWriter;
            public StreamReader PlayerReader => playerReader;
        }

        private static List<Bezeroak> bezeroLista = new List<Bezeroak>();
        private static int bezeroak = 0;
        private static readonly object lockObj = new object();
        private static List<string> baraja = new List<string>();
        private static List<string> deskarteBaraja = new List<string>();

        public static void Main(string[] args)
        {
            int port = 13000;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start(4);
            Console.WriteLine("Zerbitzaria irekita, zain...");

            baraja = KartakSortu();
            deskarteBaraja = new List<string>();

            while (bezeroak < 4)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Bezeroa konektatuta.");

                Bezeroak bezeroaObj;
                lock (lockObj)
                {
                    bezeroaObj = new Bezeroak(bezeroak, client);
                    bezeroak++;
                    bezeroLista.Add(bezeroaObj);
                    Console.WriteLine($"Jokalari {bezeroak}/4 konektatuta");
                }

                if (bezeroak == 4)
                {
                    Console.WriteLine("4 jokalari konektatuta. Kartak banatzen...");
                    KartakBanatu(bezeroLista);
                    System.Threading.Thread.Sleep(2000);
                    PartidaHasi(bezeroLista);

                    // Avisar al cliente que la partida ha terminado
                    foreach (var b in bezeroLista)
                    {
                        b.PlayerWriter.WriteLine("END_GAME");
                        b.PlayerWriter.Flush();
                    }
                }
            }

            Console.ReadLine();
        }

        public static void PartidaHasi(List<Bezeroak> bezeroLista)
        {
            Console.WriteLine("Partida hasten da...");
            Random rnd = new Random();
            int eskuaZnb = rnd.Next(0, 4);
            int jokalariarenTxanda = (eskuaZnb == 3) ? 0 : eskuaZnb + 1;

            Bezeroak jokalaria = bezeroLista.Find(b => b.PlayerZnb == jokalariarenTxanda);

            foreach (var bezero in bezeroLista)
            {
                if (bezero.PlayerZnb == jokalariarenTxanda)
                {
                    while (true)
                    {
                        Console.WriteLine($"Jokalari {bezero.PlayerZnb} da txandan.");

                        // Avisar al cliente que es su turno
                        bezero.PlayerWriter.WriteLine("TURN");
                        bezero.PlayerWriter.Flush();

                        string erabakia = bezero.PlayerReader.ReadLine();
                        Console.WriteLine($"Jokalari {bezero.PlayerZnb} erabakia: {erabakia}");

                        if (erabakia == "mus")
                        {
                            // Mus aukeratu badu, bere taldekidearen erabakia eskatu
                            int taldekideaZnb = (jokalariarenTxanda > 1) ? jokalariarenTxanda - 2 : jokalariarenTxanda + 2;
                            Bezeroak taldekidea = bezeroLista.Find(b => b.PlayerZnb == taldekideaZnb);

                            taldekidea.PlayerWriter.WriteLine("TURN");
                            taldekidea.PlayerWriter.Flush();
                            string taldekideErabakia = taldekidea.PlayerReader.ReadLine();
                            Console.WriteLine($"Jokalari {taldekidea.PlayerZnb} erabakia: {taldekideErabakia}");

                            Bezeroak lehenEtsai = null;
                            Bezeroak bigarrenEtsai = null;

                            if (taldekideErabakia == "mus")
                            {
                                int etsaiMus = 0;
                                foreach (var besteBezeroa in bezeroLista)
                                {
                                    if (besteBezeroa.PlayerZnb != jokalariarenTxanda && besteBezeroa.PlayerZnb != taldekideaZnb)
                                    {
                                        if (etsaiMus == 0) lehenEtsai = besteBezeroa;
                                        else bigarrenEtsai = besteBezeroa;

                                        // Avisar que es su turno
                                        besteBezeroa.PlayerWriter.WriteLine("TURN");
                                        besteBezeroa.PlayerWriter.Flush();

                                        string etsaiErabakia = besteBezeroa.PlayerReader.ReadLine();
                                        Console.WriteLine($"Jokalari {besteBezeroa.PlayerZnb} erabakia: {etsaiErabakia}");

                                        if (etsaiErabakia == "mus")
                                        {
                                            etsaiMus++;
                                            if (etsaiMus == 2)
                                            {
                                                Console.WriteLine("Dena mus! Kartak berriro banatzen dira.");
                                                foreach (var b in bezeroLista)
                                                {
                                                    b.PlayerWriter.WriteLine("ALL_MUS");
                                                    b.PlayerWriter.Flush();
                                                }

                                                // Leer descartes de cada jugador
                                                string jokalariDeskarte = jokalaria.PlayerReader.ReadLine();
                                                string taldekideaDeskarte = taldekidea.PlayerReader.ReadLine();
                                                string lehenEtsaiDeskarte = lehenEtsai.PlayerReader.ReadLine();
                                                string bigarrenEtsaiDeskarte = bigarrenEtsai.PlayerReader.ReadLine();

                                                Console.WriteLine($"Jokalari {jokalaria.PlayerZnb} deskartatu du: {jokalariDeskarte}");
                                                Console.WriteLine($"Jokalari {taldekidea.PlayerZnb} deskartatu du: {taldekideaDeskarte}");
                                                Console.WriteLine($"Jokalari {lehenEtsai.PlayerZnb} deskartatu du: {lehenEtsaiDeskarte}");
                                                Console.WriteLine($"Jokalari {bigarrenEtsai.PlayerZnb} deskartatu du: {bigarrenEtsaiDeskarte}");

                                                int jokalariKop = DeskarteKudeaketa(jokalariDeskarte, jokalaria);
                                                int taldekideaKop = DeskarteKudeaketa(taldekideaDeskarte, taldekidea);
                                                int lehenEtsaiKop = DeskarteKudeaketa(lehenEtsaiDeskarte, lehenEtsai);
                                                int bigarrenEtsaiKop = DeskarteKudeaketa(bigarrenEtsaiDeskarte, bigarrenEtsai);

                                                musBanatu(jokalaria, jokalariKop);
                                                musBanatu(taldekidea, taldekideaKop);
                                                musBanatu(lehenEtsai, lehenEtsaiKop);
                                                musBanatu(bigarrenEtsai, bigarrenEtsaiKop);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            break; // No mus, siguiente paso del juego
                        }
                    }
                }
            }
        }

        public static List<string> KartakSortu()
        {
            List<string> kartak = new List<string>();
            string[] paloak = { "oro", "copa", "espada", "basto" };
            string[] balioak = { "1", "2", "3", "4", "5", "6", "7", "10", "11", "12" };

            foreach (var palo in paloak)
            {
                foreach (var balio in balioak)
                {
                    kartak.Add($"{palo}{balio}");
                }
            }

            Random rnd = new Random();
            return kartak.OrderBy(x => rnd.Next()).ToList();
        }

        public static void KartakBanatu(List<Bezeroak> bezeroLista)
        {
            Console.WriteLine("Kartak banatzen jokalari guztiei...");

            lock (lockObj)
            {
                foreach (var bezeroa in bezeroLista)
                {
                    bezeroa.PlayerWriter.WriteLine("CARDS");
                    bezeroa.PlayerWriter.Flush();

                    for (int i = 0; i < 4; i++)
                    {
                        string karta = baraja[0];
                        bezeroa.Eskua.Add(karta);
                        bezeroa.PlayerWriter.WriteLine(karta);
                        bezeroa.PlayerWriter.Flush();
                        baraja.RemoveAt(0);
                    }
                }
            }
        }

        public static int DeskarteKudeaketa(string deskarteString, Bezeroak jokalaria)
        {
            deskarteString = deskarteString.TrimEnd('*');
            if (string.IsNullOrEmpty(deskarteString))
            {
                jokalaria.Eskua.Clear();
                return 4;
            }

            string[] deskartatutakoKartak = deskarteString.Split('-');
            lock (lockObj)
            {
                foreach (var karta in deskartatutakoKartak)
                {
                    if (!string.IsNullOrEmpty(karta))
                    {
                        deskarteBaraja.Add(karta);
                        jokalaria.Eskua.Remove(karta);
                    }
                }
            }
            return deskartatutakoKartak.Count(k => !string.IsNullOrEmpty(k));
        }

        public static void musBanatu(Bezeroak jokalaria, int kopurua)
        {
            lock (lockObj)
            {
                for (int i = 0; i < kopurua; i++)
                {
                    if (baraja.Count == 0)
                    {
                        baraja = deskarteBaraja;
                        deskarteBaraja = new List<string>();
                        Random rnd = new Random();
                        baraja = baraja.OrderBy(x => rnd.Next()).ToList();
                    }

                    string karta = baraja[0];
                    jokalaria.Eskua.Add(karta);
                    baraja.RemoveAt(0);
                }

                foreach (var karta in jokalaria.Eskua)
                {
                    jokalaria.PlayerWriter.WriteLine(karta);
                    jokalaria.PlayerWriter.Flush();
                }
            }
        }
    }
}

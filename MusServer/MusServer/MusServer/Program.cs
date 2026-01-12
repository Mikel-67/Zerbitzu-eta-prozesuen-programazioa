using System.Net;
using System.Net.Sockets;
using static Zerbitzaria.Zerbitzaria;

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
            // Lehenengo zerbitzariaren ip-a eta portua zehaztu
            int port = 13000;

            // socketasortu eta abiarazi
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start(4); //maximo 4 konexio izango ditu
            Console.WriteLine("Zerbitzaria irekita, zain...");

            // Baraja sortu BEHIN bakarrik
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

                // 4 jokalari? Kartak banatu
                if (bezeroak == 4)
                {
                    Console.WriteLine("4 jokalari konektatuta. Kartak banatzen...");
                    KartakBanatu(bezeroLista);
                    //Itxaron segundo batzuk
                    System.Threading.Thread.Sleep(2000);
                    PartidaHasi(bezeroLista);
                }
            }

            Console.ReadLine();
        }

        public static void PartidaHasi(List<Bezeroak> bezeroLista)
        {
            Console.WriteLine("Partida hasten da...");
            // Lehenik random bat egiten dugu 0tik - 3ra eskua zein den jakiteko
            Random rnd = new Random();
            int eskuaZnb = rnd.Next(0, 3);

            //Eskuaren ondorengoa hasiko da partida
            int jokalariarenTxanda;
            if (eskuaZnb == 3)
            {
                jokalariarenTxanda = 0;
            }else
            {
                jokalariarenTxanda = eskuaZnb++;
            }

            Bezeroak jokalaria = null;
            foreach (var bezero in bezeroLista)
            {
                if (bezero.PlayerZnb == jokalariarenTxanda)
                {
                    jokalaria = bezero;
                    break;
                }
            }

            //Jokalaria jakinda, irakurri bere erabakia
            foreach (var bezero in bezeroLista)
            {
                if (bezero.PlayerZnb == jokalariarenTxanda)
                {
                    while (true)
                    {
                        Console.WriteLine($"Jokalari {bezero.PlayerZnb} da txandan.");
                        //Jokalariaren erabakia irakurtzeko kodea
                        string erabakia = bezero.PlayerReader.ReadLine();
                        Console.WriteLine($"Jokalari {bezero.PlayerZnb} erabakia: {erabakia}");
                        if (erabakia == "mus")
                        {
                            //Mus aukeratu badu, bere taldekidearen erabakia eskatu
                            int taldekideaZnb;
                            if (jokalariarenTxanda > 1)
                            {
                                taldekideaZnb = jokalariarenTxanda - 2;
                            }
                            else
                            {
                                taldekideaZnb = jokalariarenTxanda + 2;
                            }
                            //Taldekida aurkitzeko
                            Bezeroak taldekidea = bezeroLista.Find(bezeroa => bezeroa.PlayerZnb == taldekideaZnb);

                            string taldekideErabakia = taldekidea.PlayerReader.ReadLine();
                            Console.WriteLine($"Jokalari {taldekidea.PlayerZnb} erabakia: {taldekideErabakia}");
                            Bezeroak lehenEtsai = null;
                            Bezeroak bigarrenEtsai = null;
                            if (taldekideErabakia == "mus")
                            {
                                int etsaiMus = 0;
                                //Baita mus aukeratzen badu, beste bi jokalarien erabakia eskatu
                                foreach (var besteBezeroa in bezeroLista)
                                {
                                    if (besteBezeroa.PlayerZnb != jokalariarenTxanda && besteBezeroa.PlayerZnb != taldekideaZnb)
                                    {
                                        if (etsaiMus == 0)
                                        {
                                            //Etsaia gordetzeko
                                            lehenEtsai = besteBezeroa;
                                        }
                                        else
                                        {
                                            //Etsaia gordetzeko
                                            bigarrenEtsai = besteBezeroa;
                                        }
                                        string etsaiErabakia = besteBezeroa.PlayerReader.ReadLine();
                                        Console.WriteLine($"Jokalari {besteBezeroa.PlayerZnb} erabakia: {etsaiErabakia}");
                                        if (etsaiErabakia == "mus")
                                        {
                                            //Lehena mus egin du
                                            etsaiMus++;
                                            //Bigarrena mus egin du
                                            if (etsaiMus == 2)
                                            {
                                                Console.WriteLine("Dena mus! Kartak berriro banatzen dira.");
                                                foreach(var bezeroa in bezeroLista)
                                                {
                                                    bezeroa.PlayerWriter.WriteLine("mus");
                                                }
                                                //Jokalariak deskartatu nahi dituzten kartak aukeratu
                                                string jokalariDeskarte = jokalaria.PlayerReader.ReadLine();
                                                string taldekideaDeskarte = taldekidea.PlayerReader.ReadLine();
                                                string lehenEtsaiDeskarte = lehenEtsai.PlayerReader.ReadLine();
                                                string bigarrenEtsaiDeskarte = bigarrenEtsai.PlayerReader.ReadLine();

                                                Console.WriteLine($"Jokalari {jokalaria.PlayerZnb} deskartatzen ditu: {jokalariDeskarte}");
                                                Console.WriteLine($"Jokalari {taldekidea.PlayerZnb} deskartatzen ditu: {taldekideaDeskarte}");
                                                Console.WriteLine($"Jokalari {lehenEtsai.PlayerZnb} deskartatzen ditu: {lehenEtsaiDeskarte}");
                                                Console.WriteLine($"Jokalari {bigarrenEtsai.PlayerZnb} deskartatzen ditu: {bigarrenEtsaiDeskarte}");

                                                //Jokalari bakoitzak zenbat karta deskartatzen duen kalkulatu
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
                            break; //mus ez den beste erabaki bat hartu du
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

            // Nahastu
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
                    Console.WriteLine($"Jokalari {bezeroa.PlayerZnb} kartak:");

                    for (int i = 0; i < 4; i++)
                    {
                        string karta = baraja[0];
                        bezeroa.Eskua.Add(karta);
                        Console.WriteLine($"  -> {karta}");
                        bezeroa.PlayerWriter.WriteLine(karta);
                        baraja.RemoveAt(0);
                    }
                }
            }

            Console.WriteLine($"Kartak geratzen dira barajean: {baraja.Count}");
        }
        public static int DeskarteKudeaketa(string deskarteString, Bezeroak jokalaria)
        {
            //Kendu *, bukaera mrkatzailea
            deskarteString = deskarteString.TrimEnd('*');

            //Hutsik badago, guztiak deskartatu
            if (string.IsNullOrEmpty(deskarteString))
            {
                jokalaria.Eskua.Clear();
                return 4;
            }

            // - bidez zatitu kartak lortzeko
            string[] deskartatutakoKartak = deskarteString.Split('-');

            //Deskartatutako kartak deskarteBarajan gorde
            foreach (var karta in deskartatutakoKartak)
            {
                Console.WriteLine("Deskartatzen da: " + karta);
                if (!string.IsNullOrEmpty(karta))
                {
                    deskarteBaraja.Add(karta);
                    //Jokalariaren eskuatik kendu karta
                    jokalaria.Eskua.Remove(karta);
                }
            }
            return deskartatutakoKartak.Length;
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

                        //Nahastu berriro
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
                }
            }
        }
    }
}
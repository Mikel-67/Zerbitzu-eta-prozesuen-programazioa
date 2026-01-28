using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

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
            private int taldea;

            public Bezeroak(int playerZnb, TcpClient client, int taldea)
            {
                this.playerZnb = playerZnb;
                this.client = client;
                eskua = new List<string>();
                stream = client.GetStream();
                playerWriter = new StreamWriter(stream) { AutoFlush = true };
                playerReader = new StreamReader(stream);
                this.taldea = taldea;
            }

            public int PlayerZnb => playerZnb;
            public TcpClient Client => client;
            public List<string> Eskua => eskua;
            public StreamWriter PlayerWriter => playerWriter;
            public StreamReader PlayerReader => playerReader;
            public int Taldea => taldea;
        }

        private static List<Bezeroak> bezeroLista = new List<Bezeroak>();
        private static int bezeroak = 0;
        private static readonly object lockObj = new object();
        private static List<string> baraja = new List<string>();
        private static List<string> deskarteBaraja = new List<string>();
        private static int talde1Puntuak = 0;
        private static int talde2Puntuak = 0;
        private static int countJuego = 0;

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

                int taldea = (bezeroak % 2) + 1;
                Bezeroak bezeroaObj;
                lock (lockObj)
                {
                    bezeroaObj = new Bezeroak(bezeroak, client, taldea);
                    bezeroak++;
                    bezeroLista.Add(bezeroaObj);
                    Console.WriteLine($"Jokalari {bezeroak}/4 konektatuta");
                    Console.WriteLine($"Jokalari {bezeroaObj.PlayerZnb} taldea: {bezeroaObj.Taldea}");
                }

                if (bezeroak == 4)
                {
                    Console.WriteLine("4 jokalari konektatuta. Kartak banatzen...");
                    KartakBanatu(bezeroLista);
                    System.Threading.Thread.Sleep(2000);
                    PartidaHasi(bezeroLista);

                    Console.WriteLine("Partida amaitu da.");
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

            // 1️⃣ Jokalaria
            Bezeroak jokalaria = bezeroLista.Find(b => b.PlayerZnb == jokalariarenTxanda);

            // 2️⃣ Taldekidea
            int taldekideaZnb = (jokalariarenTxanda > 1) ? jokalariarenTxanda - 2 : jokalariarenTxanda + 2;
            Bezeroak taldekidea = bezeroLista.Find(b => b.PlayerZnb == taldekideaZnb);

            // 3️⃣ Etsaien Bezeroak
            List<Bezeroak> etsaiLista = bezeroLista
                .FindAll(b => b.PlayerZnb != jokalariarenTxanda && b.PlayerZnb != taldekideaZnb);

            if (etsaiLista.Count != 2)
                throw new Exception("Etsaien kopurua ez da 2, begiratu bezeroLista.");

            Bezeroak lehenEtsai = etsaiLista[0];
            Bezeroak bigarrenEtsai = etsaiLista[1];

            Console.WriteLine($"Jokalaria: {jokalaria.PlayerZnb}, Taldekidea: {taldekidea.PlayerZnb}, Etsaienak: {lehenEtsai.PlayerZnb}, {bigarrenEtsai.PlayerZnb}");
            while (talde1Puntuak < 40 || talde2Puntuak < 40)
            {
                Console.WriteLine($"Talde 1 puntuazioa: {talde1Puntuak}, Talde 2 puntuazioa: {talde2Puntuak}");

                while (true)
                {
                    jokalaria.PlayerWriter.WriteLine("TURN");
                    jokalaria.PlayerWriter.Flush();
                    string erabakia = jokalaria.PlayerReader.ReadLine();
                    Console.WriteLine($"Jokalari {jokalaria.PlayerZnb} erabakia: {erabakia}");

                    if (erabakia != "mus") break;

                    taldekidea.PlayerWriter.WriteLine("TURN");
                    taldekidea.PlayerWriter.Flush();
                    string taldekideErabakia = taldekidea.PlayerReader.ReadLine();
                    Console.WriteLine($"Taldekidearen erabakia: {taldekidea.PlayerZnb} erabakia: {taldekideErabakia}");

                    if (taldekideErabakia != "mus") break;

                    int etsaiMus = 0;
                    foreach (var etsai in new Bezeroak[] { lehenEtsai, bigarrenEtsai })
                    {
                        etsai.PlayerWriter.WriteLine("TURN");
                        etsai.PlayerWriter.Flush();
                        string etsaiErabakia = etsai.PlayerReader.ReadLine();
                        Console.WriteLine($"Etsai {etsai.PlayerZnb} erabakia: {etsaiErabakia}");

                        if (etsaiErabakia == "mus")
                        {
                            etsaiMus++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (etsaiMus == 2)
                    {
                        Console.WriteLine("Dena mus! Kartak berriro banatzen dira.");
                        foreach (var b in bezeroLista)
                        {
                            b.PlayerWriter.WriteLine("ALL_MUS");
                        }

                        // Leer descartes de cada jugador
                        string jokalariDeskarte = DeskarteakItxaron(jokalaria);
                        string taldekideaDeskarte = DeskarteakItxaron(taldekidea);
                        string lehenEtsaiDeskarte = DeskarteakItxaron(lehenEtsai);
                        string bigarrenEtsaiDeskarte = DeskarteakItxaron(bigarrenEtsai);

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
                    else
                    {
                        break;
                    }
                }
                // Mus amaitu da, orain Grandes jolasten da
                EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "GRANDES");
                EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "PEQUEÑAS");
                EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "PARES");
                EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "JUEGO");
                if (countJuego == 4)
                {
                    EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "PUNTO");
                }
            }
            if (talde1Puntuak >= 40)
            {
                Console.WriteLine("Talde 1 irabazi du partida!");
            }
            else
            {
                Console.WriteLine("Talde 2 irabazi du partida!");
            }
        }


        public static void EnvidoKudeaketa(Bezeroak jokalaria, Bezeroak taldekidea, Bezeroak etsai1, Bezeroak etsai2, string jokua)
        {
            Console.WriteLine("--------------------------------");
            Console.WriteLine(jokua + ":");

            int totala = 0;
            int azkenEnvido = 0;

            int talde1PasoKop = 0;
            int talde1EnvidoKop = 0;
            int talde2PasoKop = 0;
            int talde2EnvidoKop = 0;

            while (true)
            {
                string e1 = jokalariarenErabakia(jokalaria, jokua);
                Console.WriteLine($"Jokalari {jokalaria.PlayerZnb} erabakia: {e1}");

                if (ProcesarErabakia(e1, jokalaria.Taldea, ref totala, ref azkenEnvido,
                ref talde1PasoKop, ref talde1EnvidoKop,
                ref talde2PasoKop, ref talde2EnvidoKop,
                jokalaria, taldekidea, etsai1, etsai2, taldekidea, jokua)) break;

                string e2 = jokalariarenErabakia(etsai1,jokua);
                Console.WriteLine($"Jokalari {etsai1.PlayerZnb} erabakia: {e2}");

                if (ProcesarErabakia(e2, etsai1.Taldea, ref totala, ref azkenEnvido,
                    ref talde1PasoKop, ref talde1EnvidoKop,
                    ref talde2PasoKop, ref talde2EnvidoKop,
                    jokalaria, taldekidea, etsai1, etsai2, etsai2,jokua)) break;

                string e3 = jokalariarenErabakia(taldekidea, jokua);
                Console.WriteLine($"Jokalari {taldekidea.PlayerZnb} erabakia: {e3}");

                if (ProcesarErabakia(e3, taldekidea.Taldea, ref totala, ref azkenEnvido,
                    ref talde1PasoKop, ref talde1EnvidoKop,
                    ref talde2PasoKop, ref talde2EnvidoKop,
                    jokalaria, taldekidea, etsai1, etsai2, jokalaria, jokua)) break;

                string e4 = jokalariarenErabakia(etsai2, jokua);
                Console.WriteLine($"Jokalari {etsai2.PlayerZnb} erabakia: {e4}");

                if (ProcesarErabakia(e4, etsai2.Taldea, ref totala, ref azkenEnvido,
                    ref talde1PasoKop, ref talde1EnvidoKop,
                    ref talde2PasoKop, ref talde2EnvidoKop,
                    jokalaria, taldekidea, etsai1, etsai2, etsai1, jokua)) break;
            }
            Console.WriteLine(jokua + " amaitu da.");
        }

        static bool ProcesarErabakia(
            string erabakia,
            int taldea,
            ref int totala,
            ref int azkenEnvido,
            ref int talde1PasoKop,
            ref int talde1EnvidoKop,
            ref int talde2PasoKop,
            ref int talde2EnvidoKop,
            Bezeroak jokalaria,
            Bezeroak taldekidea,
            Bezeroak etsai1,
            Bezeroak etsai2,
            Bezeroak bereTaldekide,
            string jokua)
        {
            if (erabakia == "quiero")
            {
                irabazleaRonda(jokalaria, taldekidea, etsai1, etsai2, totala, jokua);
                totala = 0;
                return true;
            }

            if (erabakia == "paso")
            {
                if (taldea == 1)
                {
                    talde1PasoKop++;
                    if (talde1PasoKop == 2)
                    {
                        Console.WriteLine("Talde 1 paso bi aldiz");
                        if (totala != azkenEnvido)
                        {
                            if (jokua == "PARES")
                            {
                                totala = totala + calcularPuntosParesEquipo(new List<List<int>> {
                                    kartakZenbakiraBihurtu(etsai1),
                                    kartakZenbakiraBihurtu(etsai2) });
                            }
                            talde2Puntuak += talde1EnvidoKop > 0 ? totala - azkenEnvido : 1;
                        }
                        else
                        {
                            talde2Puntuak += 1;
                        }

                        int ezkerra1 = talde1Puntuak / 5;

                        int ezkerra2 = talde1Puntuak % 5;

                        Console.WriteLine(ezkerra1);
                        Console.WriteLine(ezkerra2);

                        int eskuina1 = talde2Puntuak / 5;

                        int eskuina2 = talde2Puntuak % 5;

                        Console.WriteLine(eskuina1);
                        Console.WriteLine(eskuina2);

                        foreach (var b in new Bezeroak[] { jokalaria, taldekidea, etsai1, etsai2 })
                        {
                            b.PlayerWriter.WriteLine("PUNTUAKJASO");
                            b.PlayerWriter.Flush();

                            b.PlayerWriter.WriteLine(eskuina1);
                            b.PlayerWriter.WriteLine(eskuina2);
                            b.PlayerWriter.Flush();

                            b.PlayerWriter.WriteLine(ezkerra1);
                            b.PlayerWriter.WriteLine(ezkerra2);
                            b.PlayerWriter.Flush();
                        }
                        return true;
                    }else if (azkenEnvido != 0)
                    {
                        bereTaldekide.PlayerWriter.WriteLine(jokua);
                        bereTaldekide.PlayerWriter.Flush();
                        string bereTaldekideErantzuna = bereTaldekide.PlayerReader.ReadLine();
                        Console.WriteLine($"Taldekidearen erabakia: {bereTaldekide.PlayerZnb} erabakia: {bereTaldekideErantzuna}");
                        return ProcesarErabakia(bereTaldekideErantzuna, bereTaldekide.Taldea, ref totala, ref azkenEnvido,
                            ref talde1PasoKop, ref talde1EnvidoKop,
                            ref talde2PasoKop, ref talde2EnvidoKop,
                            jokalaria, taldekidea, etsai1, etsai2, null, jokua);
                    }
                }
                else
                {
                    talde2PasoKop++;
                    if (talde2PasoKop == 2)
                    {
                        Console.WriteLine("Talde 2 paso bi aldiz");

                        if (totala != azkenEnvido)
                        {
                            if (jokua == "PARES")
                            {
                                totala = totala + calcularPuntosParesEquipo(new List<List<int>> {
                                    kartakZenbakiraBihurtu(jokalaria),
                                    kartakZenbakiraBihurtu(taldekidea) });
                            }
                            talde1Puntuak += talde2EnvidoKop > 0 ? totala - azkenEnvido : 1;
                        }
                        else
                        {
                            talde1Puntuak += 1;
                        }

                        int ezkerra1 = talde2Puntuak / 5;

                        int ezkerra2 = talde2Puntuak % 5;

                        Console.WriteLine(ezkerra1);
                        Console.WriteLine(ezkerra2);

                        int eskuina1 = talde1Puntuak / 5;

                        int eskuina2 = talde1Puntuak % 5;

                        Console.WriteLine(eskuina1);
                        Console.WriteLine(eskuina2);

                        foreach (var b in new Bezeroak[] { jokalaria, taldekidea, etsai1, etsai2 })
                        {
                            b.PlayerWriter.WriteLine("PUNTUAKJASO");
                            b.PlayerWriter.Flush();

                            b.PlayerWriter.WriteLine(eskuina1);
                            b.PlayerWriter.Flush();
                            b.PlayerWriter.WriteLine(eskuina2);
                            b.PlayerWriter.Flush();

                            b.PlayerWriter.WriteLine(ezkerra1);
                            b.PlayerWriter.Flush();
                            b.PlayerWriter.WriteLine(ezkerra2);
                            b.PlayerWriter.Flush();
                        }
                        return true;
                    }
                    else if (azkenEnvido != 0)
                    {
                        bereTaldekide.PlayerWriter.WriteLine(jokua);
                        bereTaldekide.PlayerWriter.Flush();
                        string bereTaldekideErantzuna = bereTaldekide.PlayerReader.ReadLine();
                        Console.WriteLine($"Taldekidearen erabakia: {bereTaldekide.PlayerZnb} erabakia: {bereTaldekideErantzuna}");
                        return ProcesarErabakia(bereTaldekideErantzuna, bereTaldekide.Taldea, ref totala, ref azkenEnvido,
                            ref talde1PasoKop, ref talde1EnvidoKop,
                            ref talde2PasoKop, ref talde2EnvidoKop,
                            jokalaria, taldekidea, etsai1, etsai2, null, jokua);
                    }
                }
                return false;
            }

            // Envido
            totala = kudeaketaPuntuak(erabakia, totala);
            azkenEnvido = int.Parse(erabakia);

            if (taldea == 1) talde1EnvidoKop++;
            else talde2EnvidoKop++;

            return false;
        }

        public static List<int> kartakZenbakiraBihurtu(Bezeroak b)
        {
            List<int> kartak = new List<int>();

            foreach (var karta in b.Eskua)
            {
                string zenbakia = Regex.Match(karta, @"\d+").Value;

                if (!string.IsNullOrEmpty(zenbakia))
                {
                    kartak.Add(int.Parse(zenbakia));
                }
            }
            return kartak;
        }
        public static int kudeaketaPuntuak(string Erabakia, int totala)
        {
            if (Erabakia != "paso")
            {
                int kantitatea = int.Parse(Erabakia);
                totala += kantitatea;
            }
                return totala;
        }

        public static int konparatuEskuak(List<int> a, List<int> b, string jokua)
        {
            switch (jokua)
            {
                case "PEQUEÑAS":
                    for (int i = 0; i < 4; i++)
                    {
                        if (a[i] < b[i]) return 1;
                        if (a[i] > b[i]) return -1;
                    }
                    break;

                case "GRANDES":
                    for (int i = 0; i < 4; i++)
                    {
                        if (a[i] > b[i]) return 1;
                        if (a[i] < b[i]) return -1;
                    }
                    break;

                case "PARES":
                    var pa = analizarPares(a);
                    var pb = analizarPares(b);

                    if (pa.tipo != pb.tipo)
                        return pa.tipo > pb.tipo ? 1 : -1;

                    if (pa.v1 != pb.v1)
                        return pa.v1 > pb.v1 ? 1 : -1;

                    if (pa.v2 != pb.v2)
                        return pa.v2 > pb.v2 ? 1 : -1;

                    break;

                case "JUEGO":
                    int sumaA = a.Sum();
                    int sumaB = b.Sum();

                    int APuntu = juegoKudeaketa(sumaA);
                    int BPuntu = juegoKudeaketa(sumaB);

                    if (APuntu > BPuntu) return 1;
                    if (APuntu < BPuntu) return -1;
                    break;

                case "PUNTO":
                    int sumA = a.Sum();
                    int sumB = b.Sum();

                    if (sumA > sumB) return 1;
                    if (sumA < sumB) return -1;
                    return 0;

                default:
                    break;
            }
            return 0;
        }
        static int juegoKudeaketa(int suma)
        {
            if (suma < 31) return -1; // no hay juego

            if (suma == 31) return 100;  // mejor
            if (suma == 32) return 90;   // segundo mejor

            return suma; // 33–40: cuanto más alto, mejor
        }
        static int puntosPorTipo(int tipo)
        {
            return tipo switch
            {
                1 => 1, // par
                2 => 2, // medias
                3 => 3, // duples
                _ => 0  // sin pares
            };
        }
        static int calcularPuntosParesEquipo(List<List<int>> manosEquipo)
        {
            int puntos = 0;

            foreach (var mano in manosEquipo)
            {
                var resultado = analizarPares(mano);
                puntos += puntosPorTipo(resultado.tipo);
            }

            return puntos;
        }

        public static (int tipo, int v1, int v2) analizarPares(List<int> mano)
        {
            var grupos = mano
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Key)
                .ToList();

            // Duples
            if (grupos.Count(g => g.Count() == 2) == 2)
            {
                var pares = grupos
                    .Where(g => g.Count() == 2)
                    .Select(g => g.Key)
                    .OrderByDescending(x => x)
                    .ToList();

                return (3, pares[0], pares[1]);
            }

            // Medias
            if (grupos[0].Count() == 3)
                return (2, grupos[0].Key, 0);

            // Par
            if (grupos[0].Count() == 2)
                return (1, grupos[0].Key, 0);

            // Sin pares
            return (0, 0, 0);
        }

        public static void irabazleaRonda(Bezeroak jokalaria, Bezeroak taldekidea, Bezeroak etsai1, Bezeroak etsai2, int totala, string jokua)
        {
            List<int> kartaNumJokalaria = kartakZenbakiraBihurtu(jokalaria);
            List<int> kartaNumTaldekidea = kartakZenbakiraBihurtu(taldekidea);
            List<int> kartaNumEtsai1 = kartakZenbakiraBihurtu(etsai1);
            List<int> kartaNumEtsai2 = kartakZenbakiraBihurtu(etsai2);

            //Taldekien hartean konparatu
            List<int> talde1Irabazle = konparatuEskuak(kartaNumJokalaria, kartaNumTaldekidea, jokua) >= 0 ? kartaNumJokalaria : kartaNumTaldekidea;
            List<int> talde2Irabazle = konparatuEskuak(kartaNumEtsai1, kartaNumEtsai2, jokua) >= 0 ? kartaNumEtsai1 : kartaNumEtsai2;

            int konparaketa = konparatuEskuak(talde1Irabazle, talde2Irabazle, jokua);

            if (konparaketa >= 0)
            {
                Console.WriteLine("Talde 1 da irabazlea " + jokua);
                if (jokua == "PARES")
                {
                    totala = totala + calcularPuntosParesEquipo(new List<List<int>> { kartaNumJokalaria, kartaNumTaldekidea });
                }
                talde1Puntuak += totala;
                Console.WriteLine($"Talde 1 puntuazioa: {talde1Puntuak}");
            }
            else
            {
                Console.WriteLine("Talde 2 da irabazlea" + jokua);
                if (jokua == "PARES")
                {
                    totala = totala + calcularPuntosParesEquipo(new List<List<int>> { kartaNumEtsai1, kartaNumEtsai2 });
                }
                talde2Puntuak += totala;
                Console.WriteLine($"Talde 2 puntuazioa: {talde2Puntuak}");
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
        private static string DeskarteakItxaron(Bezeroak b)
        {
            string line = "";
            do
            {
                line = line + b.PlayerReader.ReadLine();
                Console.WriteLine("linea: " + line);
            } while (string.IsNullOrEmpty(line));
            return line;
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
                        Console.WriteLine($"Deskartatuta dagoen karta: {karta}");
                        deskarteBaraja.Add(karta);
                        jokalaria.Eskua.Remove(karta);
                    }
                }
            }
            return deskartatutakoKartak.Count(k => !string.IsNullOrEmpty(k));
        }
        public static string jokalariarenErabakia(Bezeroak b, string jokua)
        {
            if (jokua == "GRANDES" || jokua == "PEQUEÑAS" || jokua == "PUNTO")
            {
                b.PlayerWriter.WriteLine(jokua);
                b.PlayerWriter.Flush();
                return (b.PlayerReader.ReadLine());
            }
            else
            {
                switch (jokua)
                {
                    case "PARES":
                        List<int> kartaNumJokalaria = kartakZenbakiraBihurtu(b);
                        bool badaukaPares = kartaNumJokalaria.GroupBy(x => x)
                            .Any(g => g.Count() >= 2);
                        if (!badaukaPares)
                        {
                            Console.WriteLine($"Jokalari {b.PlayerZnb} ez dauka PARES.");
                            return ("paso");
                        }else
                        {
                            b.PlayerWriter.WriteLine("PARES");
                            b.PlayerWriter.Flush();
                            return (b.PlayerReader.ReadLine());
                        }
                    case "JUEGO":
                        List<int> kartaNumJokalariaJuego = kartakZenbakiraBihurtu(b);
                        bool badaukaJuego = kartaNumJokalariaJuego.Sum() >= 31;
                        if (badaukaJuego)
                        {
                            b.PlayerWriter.WriteLine("JUEGO");
                            b.PlayerWriter.Flush();
                            return (b.PlayerReader.ReadLine());
                        }
                        else
                        {
                            Console.WriteLine($"Jokalari {b.PlayerZnb} ez dauka JUEGO.");
                            countJuego++;
                            return ("paso");
                        }
                    default:
                        return ("paso");
                }
            }
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
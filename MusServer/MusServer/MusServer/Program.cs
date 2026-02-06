using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using static Zerbitzaria.Zerbitzaria;

namespace Zerbitzaria
{
    public class Zerbitzaria
    {
        public class Partida
        {
            public int PartidaId { get; set; }
            public string Codigo { get; set; } // Código de sala privada (null si es pública)
            public bool EsPrivada => !string.IsNullOrEmpty(Codigo);
            public DateTime FechaCreacion { get; set; }
            public List<Bezeroak> BezeroLista { get; set; } = new List<Bezeroak>();
            public int Bezeroak { get; set; } = 0;
            public List<string> Baraja { get; set; }
            public List<string> DeskarteBaraja { get; set; } = new List<string>();
            public int Talde1Puntuak { get; set; } = 0;
            public int Talde2Puntuak { get; set; } = 0;
            public int CountJuego { get; set; } = 0;
            public object LockObj { get; set; } = new object();

            public Partida(int id, string codigo = null)
            {
                PartidaId = id;
                Codigo = codigo;
                Baraja = KartakSortu();
                FechaCreacion = DateTime.Now;
            }
        }
        public class Bezeroak
        {
            private int playerZnb;
            private string id;
            private List<string> eskua;
            private NetworkStream stream;
            private StreamWriter playerWriter;
            private StreamReader playerReader;
            private TcpClient client;
            private int taldea;

            public Bezeroak(int playerZnb, TcpClient client, int taldea, string id)
            {
                this.playerZnb = playerZnb;
                this.client = client;
                eskua = new List<string>();
                stream = client.GetStream();
                playerWriter = new StreamWriter(stream) { AutoFlush = true };
                playerReader = new StreamReader(stream);
                this.taldea = taldea;
                this.id = id;
            }

            public int PlayerZnb => playerZnb;
            public TcpClient Client => client;
            public List<string> Eskua => eskua;
            public StreamWriter PlayerWriter => playerWriter;
            public StreamReader PlayerReader => playerReader;
            public int Taldea { get => taldea; set => taldea = value; }
            public string Id => id;
        }
        public class Pareja
        {
            private int kodea { get; set; }
            public List<Bezeroak> bezeroak { get; set; } = new List<Bezeroak>();

            public Pareja(int kodea)
            {
                this.kodea = kodea;
            }
        }
        private static Dictionary<int, Partida> partidas = new Dictionary<int, Partida>();
        private static Dictionary<string, Partida> partidasPorCodigo = new Dictionary<string, Partida>();
        private static Random random = new Random(); //Kodigoak sortzeko
        private static int nextPartidaId = 0;
        private static object partidasLock = new object();
        private static int partidaKop = 0;

        private static Partida CrearPartida(string codigo = null)
        {
            lock (partidasLock)
            {
                int id = nextPartidaId++;
                var partida = new Partida(id, codigo);
                partidas[id] = partida;

                if (!string.IsNullOrEmpty(codigo))
                {
                    partidasPorCodigo[codigo] = partida;
                    Console.WriteLine($"Partida pribatua sortuta, kodea: {codigo}");
                }

                return partida;
            }
        }
        private static string GenerarCodigoUnico()
        {
            lock (partidasLock)
            {
                string codigo;
                do
                {
                    codigo = random.Next(1000, 9999).ToString();
                } while (partidasPorCodigo.ContainsKey(codigo));

                return codigo;
            }
        }
        private static Partida BuscarPartidaPorCodigo(string codigo)
        {
            lock (partidasLock)
            {
                if (partidasPorCodigo.TryGetValue(codigo, out Partida partida))
                {
                    if (partida.Bezeroak < 4)
                    {
                        return partida;
                    }
                    else
                    {
                        return null; // Partida beteta dago
                    }
                }
                return null; // Kodea ez da existitzen
            }
        }
        private static Partida BilatuPartidaById(int id)
        {
            lock (partidasLock)
            {
                foreach (var partida in partidas.Values)
                {
                    if (partida.PartidaId == id)
                    {
                        if (partida.Bezeroak <= 1)
                        {
                            return partida;
                        }
                    }
                }
                return null;
            }
        }
        private static void ProcesarNuevoJugador(TcpClient client, ref Partida partidaPublicaActual)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                StreamReader reader = new StreamReader(stream);

                string tipoSala = reader.ReadLine();
                Console.WriteLine($"📩 Cliente eligió: {tipoSala}");

                Partida partidaAsignada = null;
                Pareja parejaAsignada = null;

                switch (tipoSala)
                {
                    case "PUBLICA":
                        // (lógica original de Main, movida aquí)
                        lock (partidasLock)
                        {
                            partidaAsignada = partidaPublicaActual;

                            int taldea = 0;
                            string id = reader.ReadLine();
                            var bezeroaObj = new Bezeroak(partidaAsignada.Bezeroak, client, taldea, id);
                            partidaAsignada.BezeroLista.Add(bezeroaObj);
                            partidaAsignada.Bezeroak++;

                            writer.WriteLine("OK");
                            writer.Flush();

                            Console.WriteLine($"[Partida {partidaAsignada.PartidaId} - PÚBLICA] Jokalari {partidaAsignada.Bezeroak}/4");

                            if (partidaAsignada.Bezeroak == 4)
                            {
                                Partida p = partidaAsignada;
                                jokalarienTaldeakSortu(p);
                                Task.Run(() => IniciarPartida(p));
                                partidaPublicaActual = CrearPartida();
                                partidaKop++;
                            }
                        }
                        break;

                    case "CREAR_PRIVADA":
                        string codigoNuevo = GenerarCodigoUnico();
                        partidaAsignada = CrearPartida(codigoNuevo);

                        lock (partidasLock)
                        {
                            int taldea = (partidaAsignada.Bezeroak % 2) + 1;
                            string id = reader.ReadLine();
                            var bezeroaObj = new Bezeroak(partidaAsignada.Bezeroak, client, taldea, id);
                            partidaAsignada.BezeroLista.Add(bezeroaObj);
                            partidaAsignada.Bezeroak++;

                            writer.WriteLine("CODIGO");
                            writer.Flush();
                            writer.WriteLine(codigoNuevo);
                            writer.Flush();

                            Console.WriteLine($"[Partida {partidaAsignada.PartidaId} - PRIVADA 🔒 {codigoNuevo}] Creador (1/4)");

                            if (partidaAsignada.Bezeroak == 4)
                            {
                                Partida p = partidaAsignada;
                                Task.Run(() => IniciarPartida(p));
                            }
                        }
                        break;

                    case "UNIRSE_PRIVADA":
                        writer.WriteLine("PEDIR_CODIGO");
                        writer.Flush();

                        string codigoIngresado = reader.ReadLine();
                        Console.WriteLine($"🔑 Cliente intentando código: {codigoIngresado}");

                        partidaAsignada = BuscarPartidaPorCodigo(codigoIngresado);

                        if (partidaAsignada != null)
                        {
                            lock (partidasLock)
                            {
                                int taldea = (partidaAsignada.Bezeroak % 2) + 1;
                                string id = reader.ReadLine();
                                var bezeroaObj = new Bezeroak(partidaAsignada.Bezeroak, client, taldea, id);
                                partidaAsignada.BezeroLista.Add(bezeroaObj);
                                partidaAsignada.Bezeroak++;

                                writer.WriteLine("OK");
                                writer.Flush();

                                Console.WriteLine($"[Partida {partidaAsignada.PartidaId} - PRIVADA 🔒] Jokalari {partidaAsignada.Bezeroak}/4");

                                if (partidaAsignada.Bezeroak == 4)
                                {
                                    Partida p = partidaAsignada;
                                    Task.Run(() => IniciarPartida(p));
                                }
                            }
                        }
                        else
                        {
                            writer.WriteLine("ERROR_CODIGO");
                            writer.Flush();
                            client.Close();
                            Console.WriteLine("❌ Código inválido o sala llena");
                        }
                        break;

                    case "ID_ESKATU":
                        int ID = partidaPublicaActual.PartidaId;

                        Partida partidaBerria = BilatuPartidaById(ID);

                        if (partidaBerria == null)
                        {
                            for (int i = 0; i < partidaKop; i++)
                            {
                                ID++;
                                partidaBerria = BilatuPartidaById(ID);
                            }
                            if (partidaBerria == null)
                            {
                                partidaBerria = CrearPartida();
                                partidaKop++;
                            }
                        }
                        lock (partidasLock)
                        {
                            int taldea = 1;
                            string id = reader.ReadLine();

                            var bezeroaObj = new Bezeroak(partidaBerria.Bezeroak, client, taldea, id);
                            partidaBerria.BezeroLista.Add(bezeroaObj);
                            partidaBerria.Bezeroak++;

                            writer.WriteLine("OK");
                            writer.Flush();

                            Console.WriteLine($"[Partida {partidaBerria.PartidaId} - PÚBLICA] Jokalari {partidaBerria.Bezeroak}/4");

                            if (partidaBerria.Bezeroak == 4)
                            {
                                Partida p = partidaBerria;
                                jokalarienTaldeakSortu(p);
                                Task.Run(() => IniciarPartida(p));
                                partidaPublicaActual = CrearPartida();
                            }
                        }
                        break;
                    default:
                        writer.WriteLine("ERROR");
                        writer.Flush();
                        client.Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error procesando jugador: {ex.Message}");
                try { client.Close(); } catch { }
            }
        }

        public static void Main(string[] args)
        {
            int port = 13000;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Zerbitzaria irekita, zain...");

            Partida partidaPublicaActual = CrearPartida();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Bezeroa konektatuta.");

                Task.Run(() => ProcesarNuevoJugador(client, ref partidaPublicaActual));
            }
        }
        private static void jokalarienTaldeakSortu(Partida partida)
        {
            int Talde0Kop = 0;
            int laguntzaile = 0;
            foreach (var jokalaria in partida.BezeroLista)
            {
                if (jokalaria.Taldea == 0)
                {
                    Talde0Kop++;
                }
            }
            if (Talde0Kop < 4)
            {
                foreach(var jokalari in partida.BezeroLista)
                {
                    if(jokalari.Taldea == 0)
                    {
                        jokalari.Taldea = 2;
                    }
                }
            }
            else
            {
                foreach(var jokalariak in partida.BezeroLista)
                {
                    jokalariak.Taldea = (laguntzaile % 2) + 1;
                    laguntzaile ++;
                }
            }
        }
        private static void IniciarPartida(Partida partida)
        {
            try
            {
                foreach (var b in partida.BezeroLista)
                {
                    string info = "";
                    foreach (var o in partida.BezeroLista)
                    {
                        info = info + o.Id + o.Taldea + ",";
                    }
                    info = info.TrimEnd(',');
                    b.PlayerWriter.WriteLine("INFO:" + info);
                    b.PlayerWriter.Flush();
                }
                Console.WriteLine($"[Partida {partida.PartidaId}] Kartak banatzen...");
                KartakBanatu(partida);
                Thread.Sleep(2000);
                PartidaHasi(partida);

                Console.WriteLine($"[Partida {partida.PartidaId}] Amaitu da.");

                foreach (var b in partida.BezeroLista)
                {
                    b.PlayerWriter.WriteLine("END_GAME");
                    b.PlayerWriter.Flush();
                    b.Client.Close();
                }

                lock (partidasLock)
                {
                    partidas.Remove(partida.PartidaId);

                    if (partida.EsPrivada)
                    {
                        partidasPorCodigo.Remove(partida.Codigo);
                        Console.WriteLine($"🔓 Sala privada {partida.Codigo} eliminada");
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[Partida {partida.PartidaId}] Jugador desconectado: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Partida {partida.PartidaId}] Error: {ex.Message}");
            }
            finally
            {
                foreach (var b in partida.BezeroLista)
                {
                    try { b.Client.Close(); } catch { }
                }

                lock (partidasLock)
                {
                    partidas.Remove(partida.PartidaId);
                    Console.WriteLine($"[Partida {partida.PartidaId}] Eliminada del servidor");
                    partidaKop--;
                }
            }
        }

        public static void PartidaHasi(Partida partida)
        {
            Console.WriteLine("Partida hasten da...");
            Random rnd = new Random();
            int eskuaZnb = rnd.Next(0, 4);
            int jokalariarenTxanda = (eskuaZnb == 3) ? 0 : eskuaZnb + 1;

            // 1️⃣ Jokalaria
            Bezeroak jokalaria = partida.BezeroLista.Find(b => b.PlayerZnb == jokalariarenTxanda);

            // 2️⃣ Taldekidea
            int taldekideaZnb = (jokalariarenTxanda > 1) ? jokalariarenTxanda - 2 : jokalariarenTxanda + 2;
            Bezeroak taldekidea = partida.BezeroLista.Find(b => b.PlayerZnb == taldekideaZnb);

            // 3️⃣ Etsaien Bezeroak
            List<Bezeroak> etsaiLista = partida.BezeroLista
                .FindAll(b => b.PlayerZnb != jokalariarenTxanda && b.PlayerZnb != taldekideaZnb);

            if (etsaiLista.Count != 2)
                throw new Exception("Etsaien kopurua ez da 2, begiratu bezeroLista.");

            Bezeroak lehenEtsai = etsaiLista[0];
            Bezeroak bigarrenEtsai = etsaiLista[1];

            Console.WriteLine($"Jokalaria: {jokalaria.PlayerZnb}, Taldekidea: {taldekidea.PlayerZnb}, Etsaienak: {lehenEtsai.PlayerZnb}, {bigarrenEtsai.PlayerZnb}");
            while (partida.Talde1Puntuak < 40 && partida.Talde2Puntuak < 40)
            {
                Console.WriteLine($"Talde 1 puntuazioa: {partida.Talde1Puntuak}, Talde 2 puntuazioa: {partida.Talde2Puntuak}");

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
                        foreach (var b in partida.BezeroLista)
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

                        int jokalariKop = DeskarteKudeaketa(jokalariDeskarte, jokalaria, partida);
                        int taldekideaKop = DeskarteKudeaketa(taldekideaDeskarte, taldekidea, partida);
                        int lehenEtsaiKop = DeskarteKudeaketa(lehenEtsaiDeskarte, lehenEtsai, partida);
                        int bigarrenEtsaiKop = DeskarteKudeaketa(bigarrenEtsaiDeskarte, bigarrenEtsai, partida);

                        musBanatu(jokalaria, jokalariKop, partida);
                        musBanatu(taldekidea, taldekideaKop, partida);
                        musBanatu(lehenEtsai, lehenEtsaiKop, partida);
                        musBanatu(bigarrenEtsai, bigarrenEtsaiKop, partida);
                    }
                    else
                    {
                        break;
                    }
                }
                // Mus amaitu da, orain Grandes jolasten da
                EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "GRANDES", partida);
                if (partida.Talde1Puntuak >= 99999 || partida.Talde2Puntuak >= 99999) goto CheckBukatu;
                EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "PEQUEÑAS", partida);
                if (partida.Talde1Puntuak >= 99999 || partida.Talde2Puntuak >= 99999) goto CheckBukatu;
                EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "PARES", partida);
                if (partida.Talde1Puntuak >= 99999 || partida.Talde2Puntuak >= 99999) goto CheckBukatu;
                partida.CountJuego = 0;
                EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "JUEGO", partida);
                if (partida.Talde1Puntuak >= 99999 || partida.Talde2Puntuak >= 99999) goto CheckBukatu;
                if (partida.CountJuego == 4)
                {
                    partida.CountJuego = 0;
                    EnvidoKudeaketa(jokalaria, taldekidea, lehenEtsai, bigarrenEtsai, "PUNTO", partida);
                }
                partida.CountJuego = 0;
                goto CheckBukatu;
            }
        CheckBukatu:
            if (partida.Talde1Puntuak >= 40)
            {
                Console.WriteLine("Talde 1 irabazi du partida!");
            }
            else
            {
                Console.WriteLine("Talde 2 irabazi du partida!");
            }
        }


        public static void EnvidoKudeaketa(Bezeroak jokalaria, Bezeroak taldekidea, Bezeroak etsai1, Bezeroak etsai2, string jokua, Partida partida)
        {
            Console.WriteLine("--------------------------------");
            Console.WriteLine(jokua + ":");

            int totala = 0;
            int azkenEnvido = 0;

            int talde1PasoKop = 0;
            int talde1EnvidoKop = 0;
            int talde2PasoKop = 0;
            int talde2EnvidoKop = 0;
            bool talde1Jokua = false;
            bool talde2Jokua = false;

            while (true)
            {
                bool lehenEnvido = false;
                string e1 = null;
                string e2 = null;
                string e3 = null;
                string e4 = null;

                // ✅ Para PARES y JUEGO, primero verificar quién tiene juego válido
                if (jokua == "PARES" || jokua == "JUEGO")
                {
                    e1 = jokalariarenErabakia(jokalaria, jokua, partida);
                    if (e1 == "jokuaDaukat")
                    {
                        talde1Jokua = true;
                    }

                    e2 = jokalariarenErabakia(etsai1, jokua, partida);
                    if (e2 == "jokuaDaukat")
                    {
                        talde2Jokua = true;
                    }

                    e3 = jokalariarenErabakia(taldekidea, jokua, partida);
                    if (e3 == "jokuaDaukat")
                    {
                        talde1Jokua = true;
                    }

                    e4 = jokalariarenErabakia(etsai2, jokua, partida);
                    if (e4 == "jokuaDaukat")
                    {
                        talde2Jokua = true;
                    }
                }

                // ✅ JOKALARIA - Solo preguntar si tiene juego o si es GRANDES/PEQUEÑAS/PUNTO
                if (jokua == "PARES" || jokua == "JUEGO")
                {
                    if (e1 == "ezJuego")
                    {
                        // No hacer nada, ya sabemos que no tiene
                        if (ProcesarErabakia(e1, jokalaria.Taldea, ref totala, ref azkenEnvido,
                            ref talde1PasoKop, ref talde1EnvidoKop,
                            ref talde2PasoKop, ref talde2EnvidoKop,
                            jokalaria, taldekidea, etsai1, etsai2, taldekidea, jokua, lehenEnvido, partida)) break;

                        goto CheckEtsai1; // Saltar al siguiente jugador
                    }
                    else if (e1 == "jokuaDaukat")
                    {
                        if (talde1Jokua && talde2Jokua)
                        {
                            Console.WriteLine("Talde biak jokua dauka");
                            jokalaria.PlayerWriter.WriteLine(jokua);
                            jokalaria.PlayerWriter.Flush();
                            e1 = jokalaria.PlayerReader.ReadLine();
                        }
                        else
                        {
                            Console.WriteLine("Talde bakarra jokua dauka");
                            e1 = "quiero";
                        }
                    }
                }
                else
                {
                    // Para GRANDES, PEQUEÑAS, PUNTO - preguntar siempre
                    e1 = jokalariarenErabakia(jokalaria, jokua, partida);
                }

                Console.WriteLine($"Jokalari {jokalaria.PlayerZnb} erabakia: {e1}");

                if (ProcesarErabakia(e1, jokalaria.Taldea, ref totala, ref azkenEnvido,
                    ref talde1PasoKop, ref talde1EnvidoKop,
                    ref talde2PasoKop, ref talde2EnvidoKop,
                    jokalaria, taldekidea, etsai1, etsai2, taldekidea, jokua, lehenEnvido, partida)) break;

                CheckEtsai1:
                // ✅ ETSAI1
                if (jokua == "PARES" || jokua == "JUEGO")
                {
                    if (e2 == "ezJuego")
                    {
                        if (ProcesarErabakia(e2, etsai1.Taldea, ref totala, ref azkenEnvido,
                            ref talde1PasoKop, ref talde1EnvidoKop,
                            ref talde2PasoKop, ref talde2EnvidoKop,
                            jokalaria, taldekidea, etsai1, etsai2, etsai2, jokua, lehenEnvido, partida)) break;

                        goto CheckTaldekidea;
                    }
                    else if (e2 == "jokuaDaukat")
                    {
                        if (talde1Jokua && talde2Jokua)
                        {
                            Console.WriteLine("Talde biak jokua dauka");
                            etsai1.PlayerWriter.WriteLine(jokua);
                            etsai1.PlayerWriter.Flush();
                            e2 = etsai1.PlayerReader.ReadLine();
                        }
                        else
                        {
                            Console.WriteLine("Talde bakarra jokua dauka");
                            e2 = "quiero";
                        }
                    }
                }
                else
                {
                    e2 = jokalariarenErabakia(etsai1, jokua, partida);
                }

                Console.WriteLine($"Jokalari {etsai1.PlayerZnb} erabakia: {e2}");

                if (ProcesarErabakia(e2, etsai1.Taldea, ref totala, ref azkenEnvido,
                    ref talde1PasoKop, ref talde1EnvidoKop,
                    ref talde2PasoKop, ref talde2EnvidoKop,
                    jokalaria, taldekidea, etsai1, etsai2, etsai2, jokua, lehenEnvido, partida)) break;

                CheckTaldekidea:
                // ✅ TALDEKIDEA
                if (jokua == "PARES" || jokua == "JUEGO")
                {
                    if (e3 == "ezJuego")
                    {
                        if (ProcesarErabakia(e3, taldekidea.Taldea, ref totala, ref azkenEnvido,
                            ref talde1PasoKop, ref talde1EnvidoKop,
                            ref talde2PasoKop, ref talde2EnvidoKop,
                            jokalaria, taldekidea, etsai1, etsai2, jokalaria, jokua, lehenEnvido, partida)) break;

                        goto CheckEtsai2;
                    }
                    else if (e3 == "jokuaDaukat")
                    {
                        if (talde1Jokua && talde2Jokua)
                        {
                            Console.WriteLine("Bi taldeek jokua daukate");
                            taldekidea.PlayerWriter.WriteLine(jokua);
                            taldekidea.PlayerWriter.Flush();
                            e3 = taldekidea.PlayerReader.ReadLine();
                        }
                        else
                        {
                            Console.WriteLine("Talde bakarra jokua dauka");
                            e3 = "quiero";
                        }
                    }
                }
                else
                {
                    e3 = jokalariarenErabakia(taldekidea, jokua, partida);
                }

                Console.WriteLine($"Jokalari {taldekidea.PlayerZnb} erabakia: {e3}");

                if (ProcesarErabakia(e3, taldekidea.Taldea, ref totala, ref azkenEnvido,
                    ref talde1PasoKop, ref talde1EnvidoKop,
                    ref talde2PasoKop, ref talde2EnvidoKop,
                    jokalaria, taldekidea, etsai1, etsai2, jokalaria, jokua, lehenEnvido, partida)) break;

                CheckEtsai2:
                // ✅ ETSAI2
                if (jokua == "PARES" || jokua == "JUEGO")
                {
                    if (e4 == "ezJuego")
                    {
                        if (ProcesarErabakia(e4, etsai2.Taldea, ref totala, ref azkenEnvido,
                            ref talde1PasoKop, ref talde1EnvidoKop,
                            ref talde2PasoKop, ref talde2EnvidoKop,
                            jokalaria, taldekidea, etsai1, etsai2, etsai1, jokua, lehenEnvido, partida)) break;

                        continue; // Volver al inicio del bucle
                    }
                    else if (e4 == "jokuaDaukat")
                    {
                        if (talde1Jokua && talde2Jokua)
                        {
                            Console.WriteLine("Bi taldeek jokua daukate");
                            etsai2.PlayerWriter.WriteLine(jokua);
                            etsai2.PlayerWriter.Flush();
                            e4 = etsai2.PlayerReader.ReadLine();
                        }
                        else
                        {
                            Console.WriteLine("Talde bakarra jokua dauka");
                            e4 = "quiero";
                        }
                    }
                }
                else
                {
                    e4 = jokalariarenErabakia(etsai2, jokua, partida);
                }

                Console.WriteLine($"Jokalari {etsai2.PlayerZnb} erabakia: {e4}");

                if (ProcesarErabakia(e4, etsai2.Taldea, ref totala, ref azkenEnvido,
                    ref talde1PasoKop, ref talde1EnvidoKop,
                    ref talde2PasoKop, ref talde2EnvidoKop,
                    jokalaria, taldekidea, etsai1, etsai2, etsai1, jokua, lehenEnvido, partida)) break;
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
            string jokua,
            bool lehenEnvido,
            Partida partida)
        {
            if (erabakia == "ordago")
            {
                azkenEnvido = 9999999;
                totala = 9999999;
                // Bidali ordago mezua jokalari guztiei
                foreach (var b in new Bezeroak[] { jokalaria, taldekidea, etsai1, etsai2 })
                {
                    b.PlayerWriter.WriteLine("ORDAGO");
                    b.PlayerWriter.Flush();
                }
                return false;
            }
            if (erabakia == "quiero")
            {
                irabazleaRonda(jokalaria, taldekidea, etsai1, etsai2, totala, jokua, partida);
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
                                    kartakZenbakiraBihurtu(etsai1,jokua),
                                    kartakZenbakiraBihurtu(etsai2,jokua) });
                            }else if (jokua == "PUNTO")
                            {
                                totala++;
                            }else if (jokua == "JUEGO")
                            {
                                totala = totala + calcularPuntosJuegoEquipo(new List<List<int>> {
                                    kartakZenbakiraBihurtu(etsai1,jokua),
                                    kartakZenbakiraBihurtu(etsai2,jokua) });
                            }
                                partida.Talde2Puntuak += talde1EnvidoKop > 0 ? totala - azkenEnvido : 1;
                        }
                        else
                        {
                            if (jokua == "PARES")
                            {
                                totala = 0;
                                totala = totala + calcularPuntosParesEquipo(new List<List<int>> {
                                    kartakZenbakiraBihurtu(etsai1,jokua),
                                    kartakZenbakiraBihurtu(etsai2,jokua) });
                                partida.Talde2Puntuak += totala;
                            }else if (jokua == "JUEGO")
                            {
                                totala = 0;
                                totala = totala + calcularPuntosJuegoEquipo(new List<List<int>> {
                                    kartakZenbakiraBihurtu(etsai1,jokua),
                                    kartakZenbakiraBihurtu(etsai2,jokua) });
                                partida.Talde2Puntuak += totala;
                            }
                            else
                            {
                                if (jokua == "PUNTO")
                                {
                                    partida.Talde2Puntuak += 2;
                                }
                                else
                                {
                                    partida.Talde2Puntuak += 1;
                                }
                            }
                        }

                        int ezkerra1 = partida.Talde1Puntuak / 5;

                        int ezkerra2 = partida.Talde1Puntuak % 5;

                        Console.WriteLine(ezkerra1);
                        Console.WriteLine(ezkerra2);

                        int eskuina1 = partida.Talde2Puntuak / 5;

                        int eskuina2 = partida.Talde2Puntuak % 5;

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
                    }else if (azkenEnvido != 0)
                    {
                        bereTaldekide.PlayerWriter.WriteLine(jokua);
                        bereTaldekide.PlayerWriter.Flush();
                        string bereTaldekideErantzuna = bereTaldekide.PlayerReader.ReadLine();
                        Console.WriteLine($"Taldekidearen erabakia: {bereTaldekide.PlayerZnb} erabakia: {bereTaldekideErantzuna}");
                        return ProcesarErabakia(bereTaldekideErantzuna, bereTaldekide.Taldea, ref totala, ref azkenEnvido,
                            ref talde1PasoKop, ref talde1EnvidoKop,
                            ref talde2PasoKop, ref talde2EnvidoKop,
                            jokalaria, taldekidea, etsai1, etsai2, null, jokua, lehenEnvido, partida);
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
                                    kartakZenbakiraBihurtu(jokalaria,jokua),
                                    kartakZenbakiraBihurtu(taldekidea,jokua) });
                            }else if (jokua == "PUNTO")
                            {
                                totala++;
                            }else if (jokua == "JUEGO")
                            {
                                totala = totala + calcularPuntosJuegoEquipo(new List<List<int>> {
                                    kartakZenbakiraBihurtu(jokalaria,jokua),
                                    kartakZenbakiraBihurtu(taldekidea,jokua) });
                            }
                                partida.Talde1Puntuak += talde2EnvidoKop > 0 ? totala - azkenEnvido : 1;
                        }
                        else
                        {
                            if (jokua == "PARES")
                            {
                                totala = 0;
                                totala = totala + calcularPuntosParesEquipo(new List<List<int>> {
                                    kartakZenbakiraBihurtu(jokalaria,jokua),
                                    kartakZenbakiraBihurtu(taldekidea,jokua) });
                                partida.Talde1Puntuak += totala;
                            }else if (jokua == "JUEGO")
                            {
                                totala = 0;
                                totala = totala + calcularPuntosJuegoEquipo(new List<List<int>> {
                                    kartakZenbakiraBihurtu(jokalaria,jokua),
                                    kartakZenbakiraBihurtu(taldekidea,jokua) });
                                partida.Talde1Puntuak += totala;
                            }
                            else
                            {
                                if (jokua == "PUNTO")
                                {
                                    partida.Talde1Puntuak += 2;
                                }
                                else
                                {
                                    partida.Talde1Puntuak += 1;
                                }
                            }
                        }

                        int ezkerra1 = partida.Talde2Puntuak / 5;

                        int ezkerra2 = partida.Talde2Puntuak % 5;

                        Console.WriteLine(ezkerra1);
                        Console.WriteLine(ezkerra2);

                        int eskuina1 = partida.Talde1Puntuak / 5;

                        int eskuina2 = partida.Talde1Puntuak % 5;

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
                            jokalaria, taldekidea, etsai1, etsai2, null, jokua, lehenEnvido, partida);
                    }
                }
                return false;
            }

            // Envido
            totala = kudeaketaPuntuak(erabakia, totala);
            if (erabakia != "ezJuego")
            {
                azkenEnvido = int.Parse(erabakia);
                //Bidali ENVIDO mezua jokalari guztiei
                if (!lehenEnvido)
                {
                    foreach (var b in new Bezeroak[] { jokalaria, taldekidea, etsai1, etsai2 })
                    {
                        b.PlayerWriter.WriteLine("ENVIDO");
                        b.PlayerWriter.Flush();
                    }
                }
            }

            if (taldea == 1) talde1EnvidoKop++;
            else talde2EnvidoKop++;
            if (partida.CountJuego == 4)
            {
                return true;
            }
            return false;
        }

        public static List<int> kartakZenbakiraBihurtu(Bezeroak b,string jokua)
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
            if (jokua == "GRANDES")
            {
                kartak.Sort((a, b) => b.CompareTo(a));
            }else if (jokua == "PEQUEÑAS")
            {
                kartak.Sort();
            }
            return kartak;
        }
        public static int kudeaketaPuntuak(string Erabakia, int totala)
        {
            if (Erabakia != "paso")
            {
                if (Erabakia != "ezJuego")
                {
                    int kantitatea = int.Parse(Erabakia);
                    totala += kantitatea;
                }
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
        static int calcularPuntosJuegoEquipo(List<List<int>> manosEquipo)
        {
            int puntos = 0;
            foreach (var mano in manosEquipo)
            {
                int suma = mano.Sum();
                if (suma == 31)
                {
                    puntos += 3;
                }
                else if (suma < 41 && suma > 31)
                {
                    puntos += 2;
                }
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

        public static void irabazleaRonda(Bezeroak jokalaria, Bezeroak taldekidea, Bezeroak etsai1, Bezeroak etsai2, int totala, string jokua, Partida partida)
        {
            List<int> kartaNumJokalaria = kartakZenbakiraBihurtu(jokalaria, jokua);
            List<int> kartaNumTaldekidea = kartakZenbakiraBihurtu(taldekidea, jokua);
            List<int> kartaNumEtsai1 = kartakZenbakiraBihurtu(etsai1, jokua);
            List<int> kartaNumEtsai2 = kartakZenbakiraBihurtu(etsai2, jokua);

            //JUEGO edo PUNTO kasuan, balioak 10 baino handiagoak badira, 10 balio dute
            if (jokua == "JUEGO" || jokua == "PUNTO")
            {
                kartaNumJokalaria = kartaNumJokalaria
                    .Select(x => x > 10 ? 10 : x)
                    .ToList();
                kartaNumTaldekidea = kartaNumTaldekidea
                    .Select(x => x > 10 ? 10 : x)
                    .ToList();
                kartaNumEtsai1 = kartaNumEtsai1
                    .Select(x => x > 10 ? 10 : x)
                    .ToList();
                kartaNumEtsai2 = kartaNumEtsai2
                    .Select(x => x > 10 ? 10 : x)
                    .ToList();
            }

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
                }else if (jokua == "JUEGO")
                {
                    totala = totala + calcularPuntosJuegoEquipo(new List<List<int>> { kartaNumJokalaria, kartaNumTaldekidea });
                }else if (jokua == "PUNTO")
                {
                    totala++;
                }
                partida.Talde1Puntuak += totala;
                Console.WriteLine($"Talde 1 puntuazioa: {partida.Talde1Puntuak}");
            }
            else
            {
                Console.WriteLine("Talde 2 da irabazlea" + jokua);
                if (jokua == "PARES")
                {
                    totala = totala + calcularPuntosParesEquipo(new List<List<int>> { kartaNumEtsai1, kartaNumEtsai2 });
                }
                partida.Talde2Puntuak += totala;
                Console.WriteLine($"Talde 2 puntuazioa: {partida.Talde2Puntuak}");
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

        public static void KartakBanatu(Partida partida)
        {
            Console.WriteLine("Kartak banatzen jokalari guztiei...");

            lock (partida.LockObj)
            {
                foreach (var bezeroa in partida.BezeroLista)
                {
                    bezeroa.PlayerWriter.WriteLine("CARDS");
                    bezeroa.PlayerWriter.Flush();

                    for (int i = 0; i < 4; i++)
                    {
                        string karta = partida.Baraja[0];
                        bezeroa.Eskua.Add(karta);
                        bezeroa.PlayerWriter.WriteLine(karta);
                        bezeroa.PlayerWriter.Flush();
                        partida.Baraja.RemoveAt(0);
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

        public static int DeskarteKudeaketa(string deskarteString, Bezeroak jokalaria, Partida partida)
        {
            deskarteString = deskarteString.TrimEnd('*');
            if (string.IsNullOrEmpty(deskarteString))
            {
                jokalaria.Eskua.Clear();
                return 4;
            }

            string[] deskartatutakoKartak = deskarteString.Split('-');
            lock (partida.LockObj)
            {
                foreach (var karta in deskartatutakoKartak)
                {
                    if (!string.IsNullOrEmpty(karta))
                    {
                        Console.WriteLine($"Deskartatuta dagoen karta: {karta}");
                        partida.DeskarteBaraja.Add(karta);
                        jokalaria.Eskua.Remove(karta);
                    }
                }
            }
            return deskartatutakoKartak.Count(k => !string.IsNullOrEmpty(k));
        }
        public static string jokalariarenErabakia(Bezeroak b, string jokua, Partida partida)
        {
            if (jokua == "GRANDES" || jokua == "PEQUEÑAS" || jokua == "PUNTO")
            {
                Console.WriteLine("Sartu da");
                b.PlayerWriter.WriteLine(jokua);
                b.PlayerWriter.Flush();
                return (b.PlayerReader.ReadLine());
            }
            else
            {
                switch (jokua)
                {
                    case "PARES":
                        List<int> kartaNumJokalaria = kartakZenbakiraBihurtu(b, jokua);
                        Console.WriteLine("Karta zenbakiak PARES: " + string.Join(", ", kartaNumJokalaria));
                        bool badaukaPares = kartaNumJokalaria.GroupBy(x => x)
                            .Any(g => g.Count() >= 2);
                        if (!badaukaPares)
                        {
                            Console.WriteLine($"Jokalari {b.PlayerZnb} ez dauka PARES.");
                            partida.CountJuego++;
                            return ("ezJuego");
                        }else
                        {
                            return ("jokuaDaukat");
                        }
                    case "JUEGO":
                        List<int> kartaNumJokalariaJuego = kartakZenbakiraBihurtu(b, jokua);
                        // Zenbakia 10 baino gorakoa den kartak 10 balio du
                        List<int> kartaNumJokalariaJuegoMod = kartaNumJokalariaJuego
                            .Select(x => x > 10 ? 10 : x)
                            .ToList();
                        Console.WriteLine("Karta zenbakiak JUEGO: " + string.Join(", ", kartaNumJokalariaJuego));
                        bool badaukaJuego = kartaNumJokalariaJuegoMod.Sum() >= 31;
                        if (badaukaJuego)
                        {
                            return ("jokuaDaukat");
                        }
                        else
                        {
                            Console.WriteLine($"Jokalari {b.PlayerZnb} ez dauka JUEGO.");
                            partida.CountJuego++;
                            return ("ezJuego");
                        }
                    default:
                        return ("paso");
                }
            }
        }

        public static void musBanatu(Bezeroak jokalaria, int kopurua, Partida partida)
        {
            lock (partida.LockObj)
            {
                for (int i = 0; i < kopurua; i++)
                {
                    if (partida.Baraja.Count == 0)
                    {
                        partida.Baraja = partida.DeskarteBaraja;
                        partida.DeskarteBaraja = new List<string>();
                        Random rnd = new Random();
                        partida.Baraja = partida.Baraja.OrderBy(x => rnd.Next()).ToList();
                    }

                    string karta = partida.Baraja[0];
                    jokalaria.Eskua.Add(karta);
                    partida.Baraja.RemoveAt(0);
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace komunikazioa
{
    class Zerbitzaria
    {
        public class Jokalaria
        {
            public string Izena { get; set; }
            public bool Prest { get; set; } = false;
            public StreamWriter Writer { get; set; }
            public StreamReader Reader { get; set; }
        }

        public static List<Jokalaria> JokalariLista { get; set; } = new List<Jokalaria>();
        public static object bloqueoa = new object();
        public static int? zenbakia = null;
        public static bool partidaAmaituta = false;

        static void Main(string[] args)
        {
            TcpListener serbidorea = new TcpListener(IPAddress.Any, 13000);
            serbidorea.Start();
            Console.WriteLine("Zain...");

            while (true)
            {
                TcpClient bezeroa = serbidorea.AcceptTcpClient();
                Thread bezeroHaria = new Thread(() => KudeatuBezeroa(bezeroa));
                bezeroHaria.Start();
            }
        }

        static void KudeatuBezeroa(TcpClient bezeroa)
        {
            using (NetworkStream stream = bezeroa.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                writer.WriteLine("Zein da zure izena?");
                string izena = reader.ReadLine();

                var jokalaria = new Jokalaria { Izena = izena, Writer = writer, Reader = reader };

                lock (bloqueoa)
                {
                    JokalariLista.Add(jokalaria);
                }

                writer.WriteLine($"Kaixo {izena}! Idatzi 'PRESTATUTA' prest zaudenean.");

                while (true)
                {
                    string mezua = reader.ReadLine();
                    if (mezua == null) return;

                    if (mezua.Equals("PRESTATUTA", StringComparison.OrdinalIgnoreCase))
                    {
                        jokalaria.Prest = true;
                        writer.WriteLine("Prestatuta zaude, itxaron beste jokalariak.");

                        if (denakPrest())
                        {
                            lock (bloqueoa)
                            {
                                if (zenbakia == null)
                                {
                                    zenbakia = new Random().Next(1, 101);
                                    partidaAmaituta = false;
                                    Console.WriteLine($"[DEBUG] Zenbakia aukeratua: {zenbakia}");
                                    BidaliMezuak("Jokoa hasiko da! Zenbaki bat aukeratu da 1 eta 100 artean.");
                                    ThreadPool.QueueUserWorkItem(_ => PartidaHasi());
                                }
                            }
                        }
                    }
                }
            }
        }

        static bool denakPrest()
        {
            lock (bloqueoa)
            {
                return JokalariLista.Count > 0 && JokalariLista.All(j => j.Prest);
            }
        }

        static void BidaliMezuak(string mezua)
        {
            lock (bloqueoa)
            {
                foreach (var j in JokalariLista)
                {
                    try { j.Writer.WriteLine(mezua); } catch { }
                }
            }
        }

        static void PartidaHasi()
        {
            while (!partidaAmaituta)
            {
                lock (bloqueoa)
                {
                    foreach (var j in JokalariLista)
                    {
                        try
                        {
                            if (j.Reader.Peek() >= 0)
                            {
                                string mezua = j.Reader.ReadLine();
                                if (int.TryParse(mezua, out int saiakera))
                                {
                                    if (saiakera == zenbakia)
                                    {
                                        BidaliMezuak($"🎉 Jokalaria {j.Izena} zenbakia asmatu du: {zenbakia}!");
                                        partidaAmaituta = true;
                                        BidaliMezuak("Jokoa amaitu da. Eskerrik asko parte hartzeagatik!");
                                        break;
                                    }
                                    else
                                    {
                                        j.Writer.WriteLine("Zenbakia ez da zuzena, saiatu berriro.");
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}

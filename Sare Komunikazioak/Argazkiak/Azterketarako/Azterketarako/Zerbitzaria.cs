using System.Net;
using System.Net.Sockets;

namespace Zerbitzaria
{
    public class Zerbitzaria
    {
        public static Object blokeoa = new Object();
        static Jokua jokua = new Jokua(new Random().Next(1, 101));
        public static void Main(string[] args)
        {
            string ip = "127.0.0.1";
            int portua = 13000;

            IPAddress ipaddress = IPAddress.Parse(ip);

            TcpListener listener = new TcpListener(ipaddress, portua);
            listener.Start(10);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Bezero berria konektatu da.");

                Thread haria = new Thread(() => funtzioa(client, jokua));
                haria.Start();
            }
        }
        private static void funtzioa(TcpClient client, Jokua jokua)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream);
                using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                writer.WriteLine("Ongi etorri jokura! Sartu zune izena:");
                string izena = reader.ReadLine();
                Jokalariak jokalaria = new Jokalariak(izena, writer);
                bool hasita = false;

                while (!hasita)
                {
                    lock (blokeoa)
                    {
                        jokua.gehituJokalaria(jokalaria);
                        if (jokua.denakPrest())
                        {
                            hasita = true;
                            deneiMezuaBidali(jokua.jokalariak, "Jokoa hasiko da! saiatu asmatzen zenbakia");
                            jokua.jokuaHasi();
                        }
                    }
                }
                

            }catch (Exception ex)
            {
                Console.WriteLine("Errorea harian: " + ex.Message);
            }
        }
        private static void deneiMezuaBidali(List<Jokalariak> jokalariak, string mezua)
        {
            foreach (var jokalaria in jokalariak)
            {
                jokalaria.writer.WriteLine(mezua);
            }
        }
    }
    public class Jokua
    {
        public List<Jokalariak> jokalariak;
        private string irabazlea;
        private int zenbakiEzkututa;
        public Jokua(int zenbakiEzkututa)
        {
            this.zenbakiEzkututa = zenbakiEzkututa;
            this.irabazlea = null;
        }
        public void gehituJokalaria(Jokalariak jokalaria)
        {
            jokalariak.Add(jokalaria);
        }
        public bool denakPrest()
        {
            return jokalariak.All(j => !string.IsNullOrEmpty(j.izena));
        }
        public void jokuaHasi()
        {
            // Jokoaren logika hemen
        }
    }
    public class Jokalariak
    {
        public string izena { get; set;}
        public StreamWriter writer { get; set; }
        public Jokalariak(string izena, StreamWriter writer)
        {
            this.izena = izena;
            this.writer = writer;
        }
    }
}
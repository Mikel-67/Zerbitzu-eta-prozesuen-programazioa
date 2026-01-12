using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace komunikazioa
{
    class Zerbitzaria
    {
        static void Main(string[] args)
        {
            TcpListener serbidorea;
            try
            {
                serbidorea = new TcpListener(IPAddress.Any, 13000);
                serbidorea.Start();
                Console.WriteLine("Zain...");

                while (true)
                {
                    using (TcpClient bezeroa = serbidorea.AcceptTcpClient())
                    using (NetworkStream stream = bezeroa.GetStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        Console.WriteLine("Bezeroa konektatuta.");
                        string mezua = reader.ReadLine();
                        Console.WriteLine("Bezeroak bidalitako mezua: " + mezua);
                        if (mezua == "AMAIERA")
                        {
                            Console.WriteLine("Saioa amaituta.");
                            break;
                        }
                        mezua = mezua.ToLower();
                        int counBokalak = 0;
                        for (int i = 0; i <mezua.Length; i++)
                        {
                            char letra = mezua[i];
                            if ("aeiou".Contains(letra))
                            {
                                counBokalak++;
                            }
                        }
                        writer.WriteLine("Mezuan dauden bokalen kopurua: " + counBokalak);
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Errorea: " + e.Message);
            }
        }
    }
}
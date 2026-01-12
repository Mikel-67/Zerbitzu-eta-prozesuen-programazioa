// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Threading;

// 1. new TcpListener
// 2. listener.Start()
// 3. listener.AcceptTcpClient()
// 4. Tread haria = new Thread(() => Funtzioa(client))
// 5. haria.Start()
// 6. using NetworkStream stream = client.GetStream();
// 7. using StreamReader reader = new StreamReader(stream);
// 8. using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

namespace Zerbitzaria
{
    public class  Zerbitzaria
    {
        public static void Main(string[] args)
        {
            // Lehenengo zerbitzariaren ip-a eta portua zehaztu
            string zerbitzariaIp = "127.0.0.1";
            IPAddress iPAddress = IPAddress.Parse(zerbitzariaIp);
            int port = 13000;

            // socketasortu eta abiarazi
            TcpListener listener = new TcpListener(iPAddress, port);
            listener.Start(5); //maximo 5 konexio izango ditu
            Console.WriteLine("Zerbitzaria irekita, zain...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient(); //blokeatzen da bezero bat konektatu arte
                Console.WriteLine("Bezeroa konektatuta.");

                // Bezero ugari kudeatzeko hari bat sortu
                Thread haria = new Thread(() => BezeroakKudeatu(client));
                haria.Start();
            }
        }

        private static void BezeroakKudeatu(TcpClient client)
        {
            try
            {
                // Bezeroarekin komunikazioa hemen kudeatu
                using NetworkStream stream = client.GetStream();
                //Readerra sortu
                using StreamReader reader = new StreamReader(stream);
                //Writerrra sortu
                using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                writer.WriteLine("Ongi etorri zerbitzarira!");

                // Programazioa hasi
                while (true)
                {
                    //Bezeroan idazteko writer erabili
                    writer.WriteLine("1.MendiArgazkia || 2.HondartzaArgazkia || 3.HiriArgazkia || 4.Irten");
                    //Bezeroaren erantzuna irakurtzeko reader erabili
                    string mezua= reader.ReadLine();
                    Console.WriteLine("Bezeroa aukeratutakoa: " + mezua);

                    if (mezua == "4")
                    {
                        writer.WriteLine("Agur!");
                        break;
                    }

                    string path;
                    switch (mezua)
                    {
                        case "1":
                            //Mendi argazkia
                            path = "../../../Argazkiak/Mendia.jpg";
                            break;
                        case "2":
                            //Hondartza argazkia
                            path = "../../../Argazkiak/Hondartza.jpg";
                            break;
                        case "3":
                            //Hiri argazkia
                            path = "../../../Argazkiak/Hiria.jpg";
                            break;
                        default:
                            path = "";
                            writer.WriteLine("Aukera ez balioduna, saiatu berriro.");
                            break;
                    }
                    if (path != "")
                    {
                        //Argazkia bytetan bihurtu
                        byte[] ArgazkiaBytes = File.ReadAllBytes(path);
                        int tamaina = ArgazkiaBytes.Length;

                        writer.WriteLine(tamaina.ToString());

                        //Argazkia bezeroari bidali
                        stream.Write(ArgazkiaBytes, 0, ArgazkiaBytes.Length);
                        stream.Flush();
                        Console.WriteLine("Argazkia bidalia bezeroari: " + path);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errorea bezeroa kudeatzean: " + ex.Message);
            }
            finally
            {
                client.Close();
                Console.WriteLine("Bezeroaren konexioa itxi da.");
            }
        }
    }
}


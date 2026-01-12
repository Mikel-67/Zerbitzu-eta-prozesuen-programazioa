using System;
using System.IO;
using System.Net.Sockets;

// 1. new TcpClient
// 2. client.GetStream()
// 3. using NetworkStream stream = client.GetStream();
// 4. using StreamReader reader = new StreamReader(stream);
// 5. using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

class Bezeroa
{
    static void Main(string[] args)
    {
        //Zerbitzariaren ip-a eta portua zehaztu
        string ZerbitzariaIp = "127.0.0.1";
        int port = 13000;

        try
        {
            //Socketa sortu eta konektatu
            using TcpClient client = new TcpClient(ZerbitzariaIp, port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);
            using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            Console.WriteLine(reader.ReadLine());

            while (true)
            {
                Console.WriteLine(reader.ReadLine());
                string aukera = Console.ReadLine();
                writer.WriteLine(aukera);
                if (aukera == "4")
                {
                    Console.WriteLine(reader.ReadLine());
                    break;
                }

                // Zerbitzaritik irakurri argazkiaren datuak
                string tamainaStr = reader.ReadLine();
                int tamaina = int.Parse(tamainaStr);

                Console.WriteLine($"Irudiaren tamaina: {tamaina} byte.");

                byte[] buffer = new byte[tamaina];
                int irakurritak = 0;
                while (irakurritak < tamaina)
                {
                    int n = stream.Read(buffer, irakurritak, tamaina - irakurritak);
                    if (n == 0) break;
                    irakurritak += n;
                }

                string carpetaDescargas = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );

                string fitxategia = Path.Combine(carpetaDescargas, $"Irudia_{aukera}.jpg");

                File.WriteAllBytes(fitxategia, buffer);

                Console.WriteLine("Irudia jaso eta gordeta: " + fitxategia);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Errorea konektatzean: " + ex.Message);
        }
    }
}

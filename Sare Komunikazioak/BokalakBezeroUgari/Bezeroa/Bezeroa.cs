using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Bezeroa
{
    static void Main()
    {
        string servitzailea = "127.0.0.1";
        int portua = 13000;

        try
        {
            using (TcpClient bezeroa = new TcpClient(servitzailea, portua))
            using (NetworkStream stream = bezeroa.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                Thread th = new Thread(() =>
                {
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        Console.WriteLine(line);
                    }
                });
                th.Start();

                while (true)
                {
                    string mezua = Console.ReadLine();
                    writer.WriteLine(mezua);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Errorea: " + e.Message);
        }
    }
}

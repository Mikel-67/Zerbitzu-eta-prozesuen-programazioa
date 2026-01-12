using System;
using System.Net.Sockets;
using System.Text;

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
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (true)
                {
                    Console.Write("Zein da zure izena? ");
                    string mezua = Console.ReadLine();
                    writer.WriteLine(mezua);
                    var list = reader.ReadLine();
                    Console.WriteLine(list);
                }

            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Errorea: " + e.Message);
        }
    }
}
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
                    Console.Write("Sartu mezua bidaltzeko (irten nahi baduzu, Idatzi AMAIERA): ");
                    string mezua = Console.ReadLine();
                    if (mezua == "AMAIERA")
                    {
                        writer.WriteLine(mezua);
                        break;
                    }
                    writer.WriteLine(mezua);
                    var count =reader.ReadLine();
                    Console.WriteLine("Bidalitako mezua: " + count + " bokal dauzka");
                }
                
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Errorea: " + e.Message);
        }
    }
}
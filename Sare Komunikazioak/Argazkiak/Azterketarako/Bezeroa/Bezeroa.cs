using System.Net.Sockets;

class Bezeroa
{
    static void Main(string[] args)
    {
        string ip = "127.0.0.1";
        int portua = 13000;

        try
        {
            TcpClient client = new TcpClient(ip, portua);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);
            using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Errorea bezeroan: " + ex.Message);
        }
    }
}
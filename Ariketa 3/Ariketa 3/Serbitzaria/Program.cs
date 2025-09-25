using System;
using System.IO.Pipes;
using System.IO;

class Zerbitzaria
{
    static void Main()
    {
        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("AzaleraKalkPipe", PipeDirection.InOut))
        {
            pipeServer.WaitForConnection();
            Console.WriteLine("Konexioa Konektatuta.");

            using (StreamReader reader = new StreamReader(pipeServer))
            using (StreamWriter writer = new StreamWriter(pipeServer) { AutoFlush = true })
            {
                string? bezeroMezua;
                while ((bezeroMezua = reader.ReadLine()) != null)
                {
                    if (bezeroMezua == "-1") break;
                    string[] datuak = bezeroMezua.Split(' ');
                    int figura = int.Parse(datuak[0]);
                    double znb1 = double.Parse(datuak[1]);
                    double znb2 = double.Parse(datuak[2]);

                    double emaitza = 0;

                    switch (figura)
                    {
                        case 1: emaitza = (znb1*znb1) * znb2 ; break;
                        case 2: emaitza = (znb1 * znb2) /2; break;
                        case 3: emaitza = znb1 * znb2; break;
                        case 4:
                            emaitza = (5* znb2 * znb1) /2;
                            break;
                        default: writer.WriteLine("Ez da opzioa existitzen"); continue;
                    }

                    Console.WriteLine($"Emaitza: {emaitza}");
                    writer.WriteLine(emaitza);
                }
            }
        }
        Console.WriteLine("Zerbitzaria itzalita.");
    }
}

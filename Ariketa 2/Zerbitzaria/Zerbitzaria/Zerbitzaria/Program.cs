using System;
using System.IO.Pipes;
using System.IO;

class Zerbitzaria
{
    static void Main()
    {
        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("CalculadoraPipe", PipeDirection.InOut))
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
                    string[] partes = bezeroMezua.Split(' ');
                    int operazioa = int.Parse(partes[0]);
                    double znb1 = double.Parse(partes[1]);
                    double znb2 = double.Parse(partes[2]);

                    double emaitza = 0;

                    switch (operazioa)
                    {
                        case 1: emaitza = znb1 + znb2; break;
                        case 2: emaitza = znb1 - znb2; break;
                        case 3: emaitza = znb1 * znb2; break;
                        case 4:
                            if (znb2 != 0) emaitza = znb1 / znb2;
                            else { writer.WriteLine("Error: ezin da zati 0 egin"); continue; }
                            break;
                        case 5: emaitza = Math.Pow(znb1, znb2); break;
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

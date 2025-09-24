using System;
using System.IO.Pipes;
using System.IO;

class Bezeroa
{
    static void Main()
    {
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "CalculadoraPipe", PipeDirection.InOut))
        {
            Console.WriteLine("Zerbitzariarekin konektatzen...");
            pipeClient.Connect();

            using (StreamReader reader = new StreamReader(pipeClient))
            using (StreamWriter writer = new StreamWriter(pipeClient) { AutoFlush = true })
            {
                int operazioa = 0;

                while (operazioa != -1)
                {
                    Console.WriteLine("\nOperacion a realizar: \n" +
                        "1 - Suma\n" +
                        "2 - Resta\n" +
                        "3 - Multiplicar\n" +
                        "4 - Dividir\n" +
                        "5 - Potencia\n" +
                        "(-1) Salir");

                    Console.Write("Introduzca la operacion: ");
                    operazioa = Convert.ToInt32(Console.ReadLine());

                    if (operazioa == -1)
                    {
                        writer.WriteLine("-1");
                        break;
                    }

                    Console.Write("Sartu lehenengo zenbakia: ");
                    double znb1 = Convert.ToDouble(Console.ReadLine());
                    Console.Write("Sartu bigarren zenbakia: ");
                    double znb2 = Convert.ToDouble(Console.ReadLine());

                    writer.WriteLine($"{operazioa} {znb1} {znb2}");

                    string? emaitza = reader.ReadLine();
                    if (emaitza == null)
                    {
                        Console.WriteLine("Errorea: ez da emaitzarik jaso zerbitzaritik.");
                        break;
                    }
                    Console.WriteLine("Servitzariaren emaitza: " + emaitza);
                }
            }
        }
    }
}

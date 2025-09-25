// See https://aka.ms/new-console-template for more information
using System;
using System.IO.Pipes;
using System.IO;

class Bezeroa
{
    static void Main()
    {
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "AzaleraKalkPipe", PipeDirection.InOut))
        {
            Console.WriteLine("Zerbitzariarekin konektatzen...");
            pipeClient.Connect();

            using (StreamReader reader = new StreamReader(pipeClient))
            using (StreamWriter writer = new StreamWriter(pipeClient) { AutoFlush = true })
            {
                int figura = 0;

                while (figura != -1)
                {
                    Console.WriteLine("\nZein figura geometrikoa daukazu: \n" +
                        "1 - Zirkulua\n" +
                        "2 - Triangelua\n" +
                        "3 - Laukizuzena\n" +
                        "4 - Pentagonoa\n" +
                        "(-1) Salir");

                    Console.Write("Sartu figuraren zenbakia: ");
                    figura = Convert.ToInt32(Console.ReadLine());

                    double znb1;
                    double znb2;

                    if (figura == -1)
                    {
                        writer.WriteLine("-1");
                        break;
                    }else if (figura == 4)
                    {
                        Console.Write("Sartu pentagonoaren apotema: ");
                        znb1 = Convert.ToDouble(Console.ReadLine());
                        Console.Write("Sartu pentagonoaren aldea: ");
                        znb2 = Convert.ToDouble(Console.ReadLine());
                    }else if (figura == 1)
                    {
                        Console.Write("Sartu zirkuluaren erradioa: ");
                        znb1 = Convert.ToDouble(Console.ReadLine());
                        znb2 = Math.PI;
                    }
                    else
                    {
                        Console.Write("Sartu figuraren altuera: ");
                        znb1 = Convert.ToDouble(Console.ReadLine());
                        Console.Write("Sartu figuraren oinarria: ");
                        znb2 = Convert.ToDouble(Console.ReadLine());
                    }

                    writer.WriteLine($"{figura} {znb1} {znb2}");

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

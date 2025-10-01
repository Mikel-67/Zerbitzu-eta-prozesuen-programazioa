// See https://aka.ms/new-console-template for more information


class progam
{
    static void Main(string[] args)
    {
        Thread haria1 = new Thread(Aimar);
        Thread haria2 = new Thread(Nerea);
        Thread haria3 = new Thread(Jurgi);

        haria1.Start();
        haria2.Start();
        haria3.Start();

        haria1.Join();
        haria2.Join();
        haria3.Join();

        Console.WriteLine("Main amaitu da");
    }
    static void Aimar()
    {
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine("Aimar "+ (i+1) +".aldia");
            Thread.Sleep(300);
        }
    }

    static void Nerea()
    {
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine("Nerea " + (i + 1) + ".aldia");
            Thread.Sleep(1000);
        }
    }

    static void Jurgi()
    {
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine("Jurgi " + (i + 1) + ".aldia");
            Thread.Sleep(500);
        }
    }
}


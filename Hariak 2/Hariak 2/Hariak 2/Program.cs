// See https://aka.ms/new-console-template for more information

class program
{
    static void Main(string[] args)
    {
        int a = 5;
        int b = 10;
        Thread batuketaHaria = new Thread(() => batuketa(a, b));
    }

    static void batuketa(int a, int b)
    {
        int emaitza = a + b;
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine(emaitza);
        }
    }
}
// See https://aka.ms/new-console-template for more information
class Program
{
    static object bloqueoa = new object();
    static int ahulkiak = 4;
    static object Zerbitzen = new object();
    static object Paseoa = new object();
    static void Main(string[] args)
    {
        Thread[] Ipotxak = new Thread[7];
            for (int i = 0; i < Ipotxak.Length; i++)
            {
                Ipotxak[i] = new Thread(new ipotxak().ahulkianEseri);
                Ipotxak[i].Start();
            }
    }
    public class ipotxak
    {
        public void ahulkianEseri()
        {
            while (true)
            {

                bool eserita = false;
                while (!eserita)
                {
                    lock (bloqueoa)
                    {
                        if (ahulkiak > 0)
                        {
                            ahulkiak--;
                            Console.WriteLine("Ipotxa ahulki batean eserita. Ahulkiak: " + ahulkiak);
                            eserita = true;
                        }
                    }
                    if (!eserita)
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        bool zerbitzatua = false;
                        while (!zerbitzatua)
                        {
                            lock (Zerbitzen)
                            {
                                //zerbitzen
                                Thread.Sleep(1000);
                                zerbitzatua = true;
                            }
                            if (zerbitzatua)
                            {
                                janEgin();
                            }
                        }
                    }
                }
                if (ahulkiak >= 4)
                {
                    lock (Paseoa)
                    {
                        Thread paseo = new Thread(() =>
                        {
                            Console.WriteLine("Edurnezuri paseoan doa...");
                            int tiempo = new Random().Next(1000, 5000);
                            Thread.Sleep(tiempo);
                            Console.WriteLine("Edurnezuri paseoa amaitu du");
                        });
                        paseo.Start();
                    }
                }
                lanEgin();
            }
        }
        public void lanEgin()
        {
            Console.WriteLine("Ipotxa lan egiten...");
            int tiempo = new Random().Next(10000, 15000);
            Thread.Sleep(tiempo);
            Console.WriteLine("Ipotxa lana amaitu du");
        }
        public void janEgin()
        {
            Console.WriteLine("Ipotxa janaria jaten...");
            int tiempo = new Random().Next(100, 500);
            Thread.Sleep(tiempo);
            Console.WriteLine("Ipotxa janaria amaitu du");
            lock (bloqueoa)
            {
                ahulkiak++;
                Console.WriteLine("Ipotxa ahulki bat utzi du. Ahulkiak: " + ahulkiak);
            }
        }
    }
}
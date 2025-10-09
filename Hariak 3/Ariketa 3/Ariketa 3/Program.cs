// See https://aka.ms/new-console-template for more information
using System;
using System.Threading;
class Program
{
    static void Main(string[] args)
    {
        Banco b = new Banco(1000);
        Console.WriteLine("El saldo inicial del banco es " + b.GetSaldo());
        Thread t1 = new Thread(() =>
        {
            b.Depositar(500);
            b.Retirar(200);
        });
        Thread t2 = new Thread(() =>
        {
            b.Retirar(300);
            b.Depositar(100);
        });
        Thread t3 = new Thread(() =>
        {
            b.Retirar(400);
            b.Depositar(200);
        });
        t1.Start();
        t2.Start();
        t3.Start();
        t1.Join();
        t2.Join();
        t3.Join();
        Console.WriteLine("El saldo final del banco es " + b.GetSaldo());
    }
}
class Banco
{
    private decimal saldo;
    private object bloqueo = new object();
    public Banco(decimal saldoInicial)
    {
        saldo = saldoInicial;
    }
    public void Depositar(decimal monto)
    {
        lock (bloqueo)
        {
            saldo += monto;
        }
    }
    public void Retirar(decimal monto)
    {
        lock (bloqueo)
        {
            saldo -= monto;
        }
    }
    public decimal GetSaldo()
    {
        return saldo;
    }
}
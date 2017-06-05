using System;

namespace QuoteReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var asl = new AsynchronousSocketListener();
            asl.StartListening();
        }
    }
}

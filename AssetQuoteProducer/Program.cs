using System;
using System.Threading;

namespace AssetQuoteProducer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Console.WriteLine("Starting...");

            var aqp = new AssetQuoteProducer();
            aqp.InitialiseAssets();
            aqp.PublishAllAssets();

            aqp.RunTasks();
            //aqp.RunTasks();
            Thread.Sleep(5000);
            aqp.EndPublishing();
            Thread.Sleep(5000);
        }
    }
}

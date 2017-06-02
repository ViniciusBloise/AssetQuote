using System;

namespace AssetQuoteProducer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var aqp = new AssetQuoteProducer();
            aqp.InitializeAssets();
        }
    }
}

using System;

namespace AssetQuoteProducer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var a = new QuoteTick()
            {
                Asset = "GOLD",
                Price = 122.4,
                TransactionDate = DateTime.Now
            };

            Console.WriteLine(a.ToString());

            var serial = a.ToString();
            var qt = new QuoteTick().FromString(serial);

            var aqp = new AssetQuoteProducer();
            aqp.InitialiseAssets();
        }
    }
}

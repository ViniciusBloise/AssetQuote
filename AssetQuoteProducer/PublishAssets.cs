using System;
namespace AssetQuoteProducer
{
	public delegate void OnPublish(Asset asset);

	public class PublishAssets
    {
        public PublishAssets()
        {
        }

        static void PublishToConsole(Asset asset)
        {
            Console.WriteLine(asset.ToString());
        }

        static void PublishToListener(Asset asset)
        {
            
        }

        static void PublishToPort(Asset asset)
        {
            
        }
    }
}

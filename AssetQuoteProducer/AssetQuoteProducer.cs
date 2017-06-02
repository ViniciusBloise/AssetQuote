using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssetQuoteProducer
{
    public class AssetQuoteProducer
    {
        private IDictionary<string, Asset> _listOfAssets;
        private IDictionary<string, Task> _listOfTasks;

        private const string INITIAL_ASSETS_FILE = @"./Resources/InitialAssets.txt";

        private static object lock1 = new object();

        public AssetQuoteProducer()
        {
        }

        /// <summary>
        /// Initialises the assets, reading from resources file (INITIAL_ASSETS_FILE)
        /// </summary>
        public void InitialiseAssets()
        {
            _listOfAssets = new Dictionary<string, Asset>();

            try
            {
                var fileStream = new FileStream(INITIAL_ASSETS_FILE, FileMode.Open);
                using (var reader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        //Debug
                        Debug.WriteLine(line);
                        if (String.IsNullOrEmpty(line) || line.StartsWith("/")) //Comments
                            continue;

                        //Process the line into its elements
                        Debug.WriteLine("Found:" + line);
                        var asset = new Asset();
                        asset.FromString(line);

                        _listOfAssets.Add(asset.Name, asset);

                        Debug.WriteLine(asset.ToString());
                    }

                    Debug.WriteLine("Number of Assets found " + _listOfAssets.Count);
                 }
            }
            catch(IOException e)
            {
                Debug.WriteLine("Could not read from file " + INITIAL_ASSETS_FILE);
                Debug.WriteLine("Error Trace: " + e.ToString());
                throw;
            }
            catch(Exception e2)
            {
                Debug.WriteLine("Unknown exception at InitialiseAssets");
                Debug.WriteLine("Error Trace: " + e2.ToString());
                throw;
            }
        }

        /// <summary>
        /// Publishes the assets. Create a list of threads for every asset and
        /// update their values according to volatility and liquidity parameters
        /// </summary>
        public void PublishAssets()
        {
            //See if there's a list of assets
            if(_listOfAssets != null || _listOfTasks.Count > 0)
            {
                foreach (var kpv in _listOfAssets)
                {
                    Task t = new Task(() =>
                    {
                        var asset = kpv.Value;

                        Random rnd = new Random();
                        //Generate a range between -1.0 and 1.0;
                        double variation = (rnd.NextDouble() - 0.5) * 2.0;
                        //Calculate Asset new Price
                        double newPrice = asset.Price + asset.Volatility * variation;

                        //Here is the critical zone. Send the new price to the listeners


                        //Generate a random time between 0 and 2 x avg transactions per sec (liquidity)
                        int nextTrade = (int) Math.Round(rnd.NextDouble() * 2 * 1000.0 / asset.Liquitidy);

                        //Sleep until another
                        Thread.Sleep(nextTrade);
                    });
                }
            }
        }
    }
}

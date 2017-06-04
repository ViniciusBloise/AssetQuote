using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Quote;
using QuoteSender;

namespace AssetQuoteProducer
{
    public class AssetQuoteProducer
    {
        private IDictionary<string, Asset> _listOfAssets;
        private IDictionary<string, Thread> _listOfTasks;
        private volatile bool _finish = false;
        private string _exchangeName;
        public string ExchangeName
        {
            get
            {
                return _exchangeName;
            }

            set
            {
                _exchangeName = value;
                _qsf.CreateQueue(value);
            }
        }

        private const string INITIAL_ASSETS_FILE = @"./Resources/InitialAssets.txt";

        private QuoteSenderFactory _qsf = null;

        public AssetQuoteProducer()
        {
            _qsf = QuoteSenderFactory.Instance;
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
        /// To call this method you must first call InitialiseAssets
        /// </summary>
        public void PublishAllAssets()
        {
            //See if there's a list of assets
            if (_listOfAssets != null)
            {
                _listOfTasks = new Dictionary<string, Thread>();

                foreach (var kpv in _listOfAssets)
                {
                    var t = new Thread(() =>
                    {
                        do
                        {
                            var asset = kpv.Value;

                            Random rnd = new Random();
                            //Generate a range between -1.0 and 1.0;
                            double variation = (rnd.NextDouble() - 0.5) * 2.0;
                            //Calculate Asset new Price
                            double newPrice = asset.Price + asset.Volatility * variation;

                            asset.Price = newPrice;
                            asset.TransactionDate = DateTime.Now;

                            var quote = new QuoteTick() { Asset = asset.Name, Price = asset.Price, TransactionDate = asset.TransactionDate };

                            //Here is the critical zone. Send the new price to the listeners
                            Publish<QuoteTick>(quote);

                            //Generate a random time between 0 and 2 x avg transactions per sec (liquidity)
                            int nextTrade = (int)Math.Round(rnd.NextDouble() * 2 * 1000.0 / asset.Liquitidy);

                            //Sleep until another
                            Thread.Sleep(nextTrade);
                        } while (!_finish);

                    });
                    _listOfTasks.Add(kpv.Key, t);
                }

                //Parallel.ForEach(_listOfTasks, (KeyValuePair<string, Task> obj) => obj.Value.Start());
                //foreach (var t in _listOfTasks) t.Value.Start();
            }
        }

        public void EndPublishing()
        {
            this._finish = true;
        }

        public void RunTasks()
        {
            Parallel.ForEach(_listOfTasks, (KeyValuePair<string, Thread> obj) => obj.Value.Start());
        }

        public void InitialisePublisher()
        {
            
        }

        public void Publish<Q>(Q item)
        {
            Console.WriteLine(item.ToString());
            _qsf.SendMessage(this.ExchangeName, item.ToString());
        }

        public void PublishAsset(Asset asset)
        {
            Console.WriteLine(asset.ToString());
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace AssetQuoteProducer
{
    public class AssetQuoteProducer
    {
        private IDictionary<string, Asset> _listOfAssets;
        private const string INITIAL_ASSETS_FILE = @"./Resources/InitialAssets.txt";

        public AssetQuoteProducer()
        {
        }

        public void InitializeAssets()
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
                        //Console
                        Console.WriteLine(line);
                        if (String.IsNullOrEmpty(line) || line.StartsWith("/")) //Comments
                            continue;

                        //Process the line into its elements
                        Console.WriteLine("Found:" + line);
                        var asset = new Asset();
                        asset.FromString(line);

                        _listOfAssets.Add(asset.Name, asset);

                        Console.WriteLine(asset.ToString());
                    }

                    Console.WriteLine("Number of Assets found " + _listOfAssets.Count);
                 }
            }
            catch(IOException e)
            {
                Console.WriteLine("Could not read from file " + INITIAL_ASSETS_FILE);
                Console.WriteLine("Error Trace: " + e.ToString());
            }
        }

    }
}

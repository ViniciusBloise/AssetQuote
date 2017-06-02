using System;
namespace AssetQuoteProducer
{
    public class Asset
    {
        public Asset()
        {
        }
        public string Name { get; set; }
        public double Price { get; set; }
        public double Volatility { get; set; }
        public double Liquitidy { get; set; }
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// From the string. Expected format is Name;Price;Volatility in Percent;Liquidity
        /// </summary>
        /// <param name="serialized">Serialized.</param>
        /// <example>GOLD;100.2;1%;1</example>
        public void FromString(string serialized)
        {
            var elements = serialized.Split(';');

            var len = elements.Length;
            //Name
            if (len > 0)
                this.Name = elements[0];
            //Price
            if (len > 1)
            {
                double price;
                if (double.TryParse(elements[1], out price))
                {
                    this.Price = price;
                };
            }
            //Volatility in percent (10.3%)
            if (len > 2)
            {
                double volat;
                if (double.TryParse(elements[2].TrimEnd(new char[] { '%', ' ' }), out volat))
                {
                    this.Volatility = volat / 100.0;
                };
            }
            //Liquidity
            if (len > 3)
            {
                double liquid;
                if (double.TryParse(elements[3], out liquid))
                {
                    this.Liquitidy = liquid;
                }
            }
            this.TransactionDate = DateTime.Now;
        }
        public override string ToString()
        {
            return string.Format("[Asset: Name={0}, Price={1}, Volatility={2}%, Liquitidy={3}, TransactionDate={4}]", Name, Price, Volatility * 100, Liquitidy, TransactionDate);
        }
    }
}

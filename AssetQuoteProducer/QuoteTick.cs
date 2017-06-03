using System;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AssetQuoteProducer
{
    public class QuoteTick
    {
        public QuoteTick()
        {
        }

        public string Asset { get; set; }
        private double _price;
        //Rounds the price to 2 decimal places
        public double Price
        {
            get
            {
                return _price;
            }
            set
            {
                _price = Math.Round(value, 2);
            }
        }
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:AssetQuoteProducer.QuoteTick"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:AssetQuoteProducer.QuoteTick"/>.</returns>
        public override string ToString()
        {
            return string.Format("[QuoteTick: Asset={0}, Price={1}, TransactionDate={2}]", Asset, Price, TransactionDate.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK"));
        }

        private const string UTC_RegEx = "\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(?:\\.\\d{2,3})?Z";

        /// <summary>
        /// Create and return a QuoteTick that was serialized using ToString
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="serializedQuote">Serialized quote.</param>
        public QuoteTick FromString(string serializedQuote)
        {
            string pattern = String.Format("\\[QuoteTick: Asset=(\\w+), Price=(\\d+\\.\\d+), TransactionDate=({0})\\]", UTC_RegEx);
            //pattern = UTC_RegEx;
            var match = Regex.Match(serializedQuote, pattern);
            if (match.Success)
            {
                this.Asset = match.Groups[1].Value;
                this.Price = double.Parse(match.Groups[2].Value);
                this.TransactionDate =
                        DateTime.ParseExact(match.Groups[3].Value, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            }
            return this;
        }
    }
}

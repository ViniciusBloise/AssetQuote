using System;
namespace QuoteReceiver
{
    /// <summary>
    /// Quote receiver factory.
    /// </summary>
    public sealed class QuoteReceiverFactory : IReceiverBuilder
    {
        private static readonly QuoteReceiverFactory _instance = new QuoteReceiverFactory();

        public static QuoteReceiverFactory Instance
        {
            get
            {
                return _instance;
            }
        }
        static QuoteReceiverFactory() { }

        private QuoteReceiverFactory()
        {
        }


        public void Start()
        {
            throw new NotImplementedException();
        }
    }
}

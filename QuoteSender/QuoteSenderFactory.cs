using System;
using RabbitMQ.Client;
using System.Text;

namespace QuoteSender
{
    public sealed class QuoteSenderFactory
    {
        private static readonly QuoteSenderFactory _instance = new QuoteSenderFactory();

        #region RabbitMQ factory
        private ConnectionFactory _connFactory = null;
        private IConnection _connection = null;
        private IModel _model = null;
        #endregion

        private static readonly object lock1 = new object();

        public QuoteSenderFactory()
        {
            _connFactory = new ConnectionFactory() { HostName = "localhost" };
            _connection = _connFactory.CreateConnection();
            _model = _connection.CreateModel();
        }

        public static QuoteSenderFactory Instance
        {
            get
            {
                return _instance;
            }
        }

        public void CreateQueue(string queue)
        {
            var resp = _model.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false, arguments: null);
		}

        public void SendMessage(string queue, string message)
        {
			var body = Encoding.UTF8.GetBytes(message);

            lock (lock1)
            {
                _model.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: body);
            }
        }
    }
}

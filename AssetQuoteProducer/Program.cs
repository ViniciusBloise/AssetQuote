using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using QuoteSender;

namespace AssetQuoteProducer
{
    class Program
    {
        [MTAThread]
        static void Main(string[] args)
        {
  
            
        }

        [MTAThread]
        static void Main3(string[] args)
        {
            Console.WriteLine("Opening localendpoint for listening");
            var ipEndPoint = TCPSocketUtil.GetIPEndpoint("localhost", 11000);
            var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listenSocket.Bind(ipEndPoint);
                listenSocket.Listen(100);
                
            }
            catch(SocketException e)
            {
                Console.WriteLine("Socket error e =" + e.ToString());
            }

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ipEndPoint);

            ManualResetEvent syncListen = new ManualResetEvent(false);
            var task = new Task(() =>
            {
                while (true)
                {
                    try
                    {
                        var line = socket.ReadString();

                        Console.WriteLine(line);
                    }
                    catch(SocketException se)
                    {
                        Console.WriteLine("Socket exception se = " + se.ToString());
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Common exception e = " + e.ToString());
                    }
                    finally
                    {
                        syncListen.Set();
                    }

                    Thread.Sleep(1000);
                }
            });
            task.Start();

            //var publishSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            try
            {
                //syncListen.WaitOne();
                socket.SendString("Test number 2.");
            }
            catch(Exception e)
            {
                Console.WriteLine("Error on listening. e=" + e.ToString());
            }
        }

        static void Main2(string[] args)
        {
            Console.WriteLine("Hello World!");

            Console.WriteLine("Starting...");

            var aqp = new AssetQuoteProducer();
            aqp.ExchangeName = "CME";
            aqp.InitialiseAssets();
            aqp.PublishAllAssets();

            aqp.RunTasks();
            //aqp.RunTasks();
            Thread.Sleep(5000);
            aqp.EndPublishing();
            Thread.Sleep(5000);
        }

        /*
         * 
            var client = new TcpClient();

            var connectionTask = client
                .ConnectAsync("localhost", 11000).ContinueWith(task1 =>
                {
                    if (task1.IsFaulted)
                        Console.WriteLine(task1.Exception.ToString());
                    return task1.IsFaulted ? null : client;
                }, TaskContinuationOptions.ExecuteSynchronously);
            var timeoutTask = Task.Delay(5000)
                .ContinueWith<TcpClient>(task1 => null, TaskContinuationOptions.ExecuteSynchronously);
            var resultTask = Task.WhenAny(connectionTask, timeoutTask).Unwrap();
            resultTask.Wait();

            var resultTcpClient = resultTask.Result;

         */

    }
}

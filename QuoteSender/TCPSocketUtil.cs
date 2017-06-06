using System;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Linq;

namespace QuoteSender
{
    public static class TCPSocketUtil
    {
        public static IPEndPoint GetIPEndpoint(string ipOrHostName, int port)
        {
			IPAddress ipAddress = GetIPAddress(ipOrHostName);
			IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            return ipEndPoint;
        }
        public static Socket GetTCPSocket(IPEndPoint ipEndPoint)
        {
            // Create a TCP/IP socket.  
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //socket.Bind(localEndPoint);

            return socket;
        }

		/// <summary>
		/// Gets the IPAddress structure from Ip or host name.
		/// </summary>
		/// <param name="ipOrHostName"></param>
		/// <returns>The IPAddress structure.</returns>
        public static IPAddress GetIPAddress(string ipOrHostName)
		{
			// Establish the local endpoint for the socket.
			#region Local endpoint discovery area
			var hosts = new ConcurrentDictionary<string, IPHostEntry>();

			// Use this synchronization to await for the async call inside the block.
			// As soon as the async call finishes we release the lock at the finally block
			ManualResetEvent syncBlock = new ManualResetEvent(false);
			var block = new Action<string>(
				async ip =>
				{
					try
					{
						var host = await Dns.GetHostEntryAsync(ip);
						if (!String.IsNullOrWhiteSpace(host.HostName))
						{
							hosts[ip] = host;
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("Error in getting Dns value. " + e.ToString());
						return;
					}
					finally
					{
						syncBlock.Set();
					}
				});
			block.Invoke(ipOrHostName);
			syncBlock.WaitOne();

			//Gets the first IPv4 address of the localhost
			IPAddress ipAddress = hosts[ipOrHostName].AddressList.
								  Where((a) => a.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
			#endregion
			return ipAddress;
		}

		// Send a string, length-prefixed, to a socket.
		public static void SendString(this Socket socket, string str)
		{
			byte[] dataBuffer = Encoding.UTF8.GetBytes(str);
			Console.WriteLine("SendStringToSocket: " + dataBuffer.Length);
            byte[] lengthBuffer = BitConverter.GetBytes(dataBuffer.Length);
                
			byte[] overallBuffer = new byte[dataBuffer.Length + lengthBuffer.Length];

			for (int b = 0; b < lengthBuffer.Length; ++b)
				overallBuffer[b] = lengthBuffer[b];

			for (int d = 0; d < dataBuffer.Length; ++d)
				overallBuffer[d + lengthBuffer.Length] = dataBuffer[d];

			Console.WriteLine("SendStringToSocket: Sending " + overallBuffer.Length);
			socket.Send(overallBuffer);
			Console.WriteLine("SendStringToSocket: Complete");
		}

		// Read a length-prefixed string from a socket.
		public static string ReadString(this Socket socket)
		{
			byte[] buffer = new byte[1024];
			bool bReadLength = false;
			int nStrLen = -1;
			MemoryStream memStream = new MemoryStream(buffer.Length);
			while (true)
			{
                try
                {
                    Console.WriteLine("Socket.ReadString: Reading...");
                    int nRead = socket.Receive(buffer, SocketFlags.None);
                    if (nRead == 0)
                        break;

                    int nOffset = 0;
                    if (!bReadLength)
                    {
                        byte[] lenBuffer = new byte[sizeof(int)];
                        if (nRead < lenBuffer.Length)
                            throw new Exception("Reading string length failed.");

                        for (int b = 0; b < lenBuffer.Length; ++b)
                            lenBuffer[b] = buffer[b];

                        nStrLen = BitConverter.ToInt32(lenBuffer, 0);
                        Console.WriteLine("Socket.ReadStringt: Length: " + nStrLen);
                        if (nStrLen < 0)
                            throw new Exception($"Invalid string length: {nStrLen} - be sure to convert from host to network");

                        bReadLength = true;
                        nOffset = lenBuffer.Length;

                        if (nStrLen == 0)
                        {
                            Console.WriteLine("Socket.ReadString: Complete with no length");
                            if (nRead != lenBuffer.Length)
                                throw new Exception("Zero length string has more data sent than expected.");
                            return "";
                        }
                    }

                    memStream.Write(buffer, nOffset, nRead - nOffset);

                    if (memStream.Length > nStrLen)
                        throw new Exception("More string data sent than expected.");

                    if (memStream.Length == nStrLen)
                        break;
                }
                catch(SocketException se)
                {
                    Console.WriteLine("Socket exception: " + se.Message);
                    return "";
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error while reading from socket. e=" + e.ToString());
                    throw;
                }
			}

            Console.WriteLine($"Socket.ReadString: Finished with {memStream.Length} bytes");
            if (memStream.TryGetBuffer(out ArraySegment<byte> buff))
                return Encoding.UTF8.GetString(buff.Array, 0, (int)memStream.Length);
            else
                return "";
		}
    }
}

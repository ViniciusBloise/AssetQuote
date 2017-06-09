﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;

/// <summary>
/// State object holds the current state of the client socket.
/// </summary>
public class AsyncServerState
{
    // Client  socket.  
    public Socket Client = null;
    public SocketAsyncEventArgs ReadEventArgs = new SocketAsyncEventArgs();
    public bool DataSizeReceived = false;
    public int DataSize = 0;
    // Size of receive buffer.  
    public const int BUFFER_SIZE = 1024;
    // Receive buffer.  
    public byte[] Buffer = new byte[BUFFER_SIZE];
    // Received data string.  
    //public StringBuilder Data = new StringBuilder();
    public MemoryStream Data = new MemoryStream(); //place where data is stored

}

public class AsynchronousSocketListener
{
    //Const declarations
    private const int PREFIX_SIZE = sizeof(int);
    private const string LOCALHOST = "localhost";
    private const int LOCALPORT = 11000;

    // Thread signal.  
    private static ManualResetEvent _syncAccept = new ManualResetEvent(false);

    public AsynchronousSocketListener() {}
 
    /// <summary>
    /// Gets the local endpoint.
    /// </summary>
    /// <returns>The local endpoint.</returns>
    private IPAddress GetLocalEndpoint()
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
        block.Invoke(LOCALHOST);
        syncBlock.WaitOne();

        //Gets the first IPv4 address of the localhost
        IPAddress ipAddress = hosts[LOCALHOST].AddressList.
                                              Where((a) => a.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
        #endregion
        return ipAddress;
    }

    /// <summary>
    /// Starts the listening.
    /// </summary>
#pragma warning disable RECS0135 // Function does not reach its end or a 'return' statement by any of possible execution paths
    public void StartListening()
#pragma warning restore RECS0135 // Function does not reach its end or a 'return' statement by any of possible execution paths
    {
        // Data buffer for incoming data.  
        byte[] bytes = new Byte[AsyncServerState.BUFFER_SIZE];

        IPAddress ipAddress = GetLocalEndpoint();
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, LOCALPORT);

        // Create a TCP/IP socket.  
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
 
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);

            while (true)
            {
                // Set the event to nonsignaled state.  
                _syncAccept.Reset();

                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a connection...");

                var acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += Accept_Completed;
                acceptEventArgs.AcceptSocket = null;
                if (!listener.AcceptAsync(acceptEventArgs)) //operation completed synchronously
                {
                    Accept_Completed(null, acceptEventArgs);
                }

                // Wait until a connection is made before continuing.  
                _syncAccept.WaitOne();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error while listening to the port. " + e.ToString());
        }
    }

    /// <summary>
    /// Accept completion handler.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">E.</param>
    private void Accept_Completed(object sender, SocketAsyncEventArgs e)
    {
        //Signal the main thread to continue.  
        _syncAccept.Set();

        //Verifies if even completed with success
        if (e.SocketError == SocketError.Success)
        {
            //Creates a readEvtArgs (SocketAsyncEventArgs)
            //and binds it to a client Socket, and the state (AsyncServerState)
            //which also has a reference to client socket and the readEvtArgs
            Socket client = e.AcceptSocket;
            var readEvtArgs = new SocketAsyncEventArgs()
            {
                AcceptSocket = client
            };
            readEvtArgs.Completed += IO_Completed;

            var state = new AsyncServerState()
            {
                ReadEventArgs = readEvtArgs,
                Client = client
            };
            readEvtArgs.UserToken = state;
            readEvtArgs.SetBuffer(state.Buffer, 0, state.Buffer.Length);

            if (!client.ReceiveAsync(readEvtArgs))
            {   //call completed synchonously
                ProcessReceive(readEvtArgs);
            }
        }
    }

    /// <summary>
    /// Generic I/O completition handler
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">E.</param>
    private void IO_Completed(object sender, SocketAsyncEventArgs e)
    {
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Receive:
                ProcessReceive(e);
                break;
            case SocketAsyncOperation.Send:
                ProcessSend(e);
                break;
            default:
                throw new NotImplementedException("The code will handle only receive and send operations");
        }
    }

    /// <summary>
    /// Process send logic.
    /// </summary>
    /// <param name="e">E.</param>
    private void ProcessSend(SocketAsyncEventArgs e) { }


    /// <summary>
    /// Server receive logic.
    /// </summary>
    /// <param name="e">E.</param>
    private void ProcessReceive(SocketAsyncEventArgs e)
    {
        //single message can be received using several receive operation
        AsyncServerState state = e.UserToken as AsyncServerState;

        if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success) 
        { 
            CloseConnection(e);
            return;
        }

        int dataOffset = 0; int restOfData = 0;
        int dataRead = e.BytesTransferred; 
        while (dataRead > 0)
        {
            if (!state.DataSizeReceived)
            {
                //there is already some data in the buffer
                if (state.Data.Length > 0)
                {
                    restOfData = PREFIX_SIZE - (int)state.Data.Length;
                    state.Data.Write(state.Buffer, dataOffset, restOfData);
                    dataRead -= restOfData;
                    dataOffset += restOfData;
                }
                else if (dataRead >= PREFIX_SIZE)
                {   //store whole data size prefix
                    state.Data.Write(state.Buffer, dataOffset, PREFIX_SIZE);
                    dataRead -= PREFIX_SIZE;
                    dataOffset += PREFIX_SIZE;
                }
                else
                {   //store only part of the size prefix
                    state.Data.Write(state.Buffer, dataOffset, dataRead);
                    dataRead = 0;
                    dataOffset += dataRead;
                }

                if (state.Data.Length == PREFIX_SIZE)
                {   //we received data size prefix
                    if (state.Data.TryGetBuffer(out ArraySegment<byte> size))
                    {
                        state.DataSize = BitConverter.ToInt32(size.Array, 0);
                        state.DataSizeReceived = true;
                        state.Data.Position = 0;
                        state.Data.SetLength(0);
                    }
                }
                else
                {   //we received just part of the header information issue another read
                    if (!state.Client.ReceiveAsync(state.ReadEventArgs))
                        ProcessReceive(state.ReadEventArgs);
                    return;
                }
            }

            //at this point we know the size of the pending data
            if ((state.Data.Length + dataRead) >= state.DataSize)
            {   //we have all the data for this message

                restOfData = state.DataSize - (int)state.Data.Length;

                state.Data.Write(state.Buffer, dataOffset, restOfData);
                Console.WriteLine("Data message received. Size: {0}", state.DataSize);

                state.Data.Flush();
                state.Data.Position = 0; //Prepare to read

                var messageBuff = new byte[restOfData];
                int nRead = state.Data.Read(messageBuff, 0, restOfData);
                var message = System.Text.Encoding.UTF8.GetString(messageBuff, 0, messageBuff.Length);
                Console.WriteLine(message);

                dataOffset += restOfData;
                dataRead -= restOfData;

                state.DataSize = 0;
                state.DataSizeReceived = false;
                state.Data.Position = 0;
                state.Data.SetLength(0);

                if (dataRead == 0)
                {
                    if (!state.Client.ReceiveAsync(state.ReadEventArgs))
                        ProcessReceive(state.ReadEventArgs);
                    return;
                }
                else
                    continue;
            }
            else
            {   //there is still data pending, store what we've received and issue another ReceiveAsync
                state.Data.Write(state.Buffer, dataOffset, dataRead);

                if (!state.Client.ReceiveAsync(state.ReadEventArgs))
                    ProcessReceive(state.ReadEventArgs);

                dataRead = 0;
            }
        }
    }

    private void CloseConnection(SocketAsyncEventArgs e)
    {
        AsyncServerState state = e.UserToken as AsyncServerState;

        try
        {
            state.Client.Shutdown(SocketShutdown.Send);
        }
        catch (Exception) { }
    }
}
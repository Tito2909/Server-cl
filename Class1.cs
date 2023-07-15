using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Windows;


namespace server
{
    public class Server
    {
        private static TcpListener tcpListener;
        private static readonly IPAddress ipAddress = IPAddress.Parse("192.168.113.92");
        private static readonly int port = 2909;
        private static Dictionary<TcpClient, byte[]> clientBuffers = new Dictionary<TcpClient, byte[]>();
        private static TcpClient thirdSideClient;
        private static NetworkStream thirdSideStream;
        private static byte[] thirdSideBuffer = new byte[4096];
        public event EventHandler<EventData> SendData;

        public static void Main(string[] args)
        {
            Server server = new Server();
            server.SendData += SendToThirdSide;
            server.Start();
           
        }
        public void Start()
        {
            tcpListener = new TcpListener(ipAddress, port);
            tcpListener.Start();
            Console.WriteLine("Server started.");
            // Start a separate thread to receive data from the third side
            Thread receiveFromThirdSideThread = new Thread(new ThreadStart(ReceiveFromThirdSide));
            receiveFromThirdSideThread.Start();

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                if (client != null)
                {
                    clientBuffers.Add(client, new byte[4096]);
                    Server server = new Server();
                 
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start();
                }
                else
                {
                    Console.WriteLine("no client connected");
                }

                

               
               
            }
        }
          // method to receive from third side and send the data to the clients
        private static void ReceiveFromThirdSide()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = thirdSideStream.Read(thirdSideBuffer, 0, buffer.Length);
                    string receivedData = Encoding.ASCII.GetString(thirdSideBuffer, 0, bytesRead);
                    Console.WriteLine("Received from third side: " + receivedData);

                    // Send the received data to all connected clients
                   
                    foreach (TcpClient connectedClient in clientBuffers.Keys)
                    {
                        NetworkStream connectedClientStream = connectedClient.GetStream();
                        connectedClientStream.Write(thirdSideBuffer, 0, bytesRead);
                        connectedClientStream.Flush();


                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving data from the third side: " + ex.Message);
                    break;
                }
            }
        }
        // method to send the clients  data stored in the buffer to the third side 
        static void SendToThirdSide(object source, EventData args)
        {
            string thirdSideIPAddress = "ThirdSideIPAddress";
            int thirdSidePort = 2808;
            thirdSideClient = new TcpClient();

            try
            {
                thirdSideClient.Connect(thirdSideIPAddress, thirdSidePort);
                thirdSideStream = thirdSideClient.GetStream();

                foreach (byte[] buffer in clientBuffers.Values)
                {
                    // Send the buffer to the third side
                    thirdSideStream.Write(buffer, 0, buffer.Length);
                }

                Console.WriteLine("Buffers sent to the third side.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending buffers to the third side: " + ex.Message);
            }
            finally
            {
                thirdSideClient.Close();

            }
        }


        // method to handle the client data , send and receive.
        void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;

            NetworkStream clientStream = client.GetStream();

            byte[] buffer = clientBuffers[client]; // Get the client's buffer
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received from client: " + data);


                byte[] response = Encoding.ASCII.GetBytes(data);
                // Send the response to all connected clients 
                foreach (TcpClient connectedClient in clientBuffers.Keys)
                {
                    NetworkStream connectedClientStream = connectedClient.GetStream();
                    connectedClientStream.Write(response, 0, response.Length);
                    connectedClientStream.Flush();
                }
                SendData?.Invoke(this, new EventData(data));

            }

            client.Close();
            clientBuffers.Remove(client);
        }
    }


    public class EventData : EventArgs
    {
        public string data { get; }
        public EventData(string data)
        {

           this. data = data;
        }
    }
}





using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Runtime.CompilerServices;

namespace server
{
    public class Server
    {
        private static TcpListener tcpListener;
        private static readonly IPAddress ipAddress = IPAddress.Parse("192.168.0.105");
        private static readonly int port = 2909;
        private static Dictionary<TcpClient, byte[]> clientBuffers = new Dictionary<TcpClient, byte[]>();
        private static TcpClient thirdSideClient;
        private static NetworkStream thirdSideStream;
        private static byte[] thirdSideBuffer = new byte[4096];
        public event EventHandler<string> SendData;
        // public event EventHandler<EventData> DataToClients;
        // Define the event for data received from the third side
        public event EventHandler<string> ThirdSideDataReceived;
        public static void Main(string[] args)
        {
            Server server = new Server();
            server.SendData += SendToThirdSide;

            server.ThirdSideDataReceived += SendDataToClients;
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


                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start();
                }
                else
                {
                    Console.WriteLine("no client connected");
                }


                //  string receivedData


            }
        }
        // method to receive from third side and send the data to the clients
        private void ReceiveFromThirdSide()
        {
            while (true)
            {
                try
                {
                    if (thirdSideClient != null && thirdSideClient.Connected && thirdSideStream != null)
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead = thirdSideStream.Read(thirdSideBuffer, 0, buffer.Length);
                        string receivedDataFromThird = Encoding.ASCII.GetString(thirdSideBuffer, 0, bytesRead);
                        Console.WriteLine("Received from third side: " + receivedDataFromThird);
                        // Raise the event to send the received data to all connected clients

                        ThirdSideDataReceived?.Invoke(this, receivedDataFromThird);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving data from the third side: " + ex.Message);
                    break;
                }


            }

            // string ReceivedData = ReceiveFromThirdSide(); 
            //return ReceivedData;    
        }
        // Send the received data to all connected clients
        public static void SendDataToClients(Object source, string receivedDataFromThird)
        {
            foreach (TcpClient connectedClient in clientBuffers.Keys)
            {
                NetworkStream connectedClientStream = connectedClient.GetStream();
                connectedClientStream.Write(Encoding.ASCII.GetBytes(receivedDataFromThird), 0, int.Parse(receivedDataFromThird));
                connectedClientStream.Flush();


            }
        }
        // method to send the clients  data stored in the buffer to the third side 
        static void SendToThirdSide(object source, string data)
        {
            IPAddress thirdSideIPAddress = IPAddress.Parse("192.168.0.105");
            int thirdSidePort = 2808;
            thirdSideClient = new TcpClient();

            try
            {
                thirdSideClient.Connect(thirdSideIPAddress, thirdSidePort);
                thirdSideStream = thirdSideClient.GetStream();

                foreach (byte[] buffer in clientBuffers.Values)
                {
                    // Send the buffer to the third side
                    thirdSideStream.Write(Encoding.ASCII.GetBytes(data), 0, data.Length);
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

            if (client == null)
            {
                Console.WriteLine("Invalid client object.");
                return;
            }
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
                SendData?.Invoke(this, data);

                byte[] response = Encoding.ASCII.GetBytes(data);
                // Send the response to all connected clients 
                foreach (TcpClient connectedClient in clientBuffers.Keys)
                {
                    NetworkStream connectedClientStream = connectedClient.GetStream();
                    connectedClientStream.Write(response, 0, response.Length);
                    connectedClientStream.Flush();
                }
               

            }

            client.Close();
            clientBuffers.Remove(client);
        }

    }


    // public class EventData : EventArgs
    //  {
    //    public string ReceivedData { get; }
    // public EventData(string receivedData)
    // {

    //    ReceivedData = receivedData;
    //  }

}

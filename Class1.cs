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
        private static readonly IPAddress ipAddress = IPAddress.Parse("192.168.113.92");
        private static readonly int port = 2909;
        private static Dictionary<TcpClient, byte[]> clientBuffers = new Dictionary<TcpClient, byte[]>();
        public event EventHandler<string> SendTheClientData;


        public static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();

        }
        public void Start()
        {
            tcpListener = new TcpListener(ipAddress, port);
            tcpListener.Start();
            Console.WriteLine("Server started.");



            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                if (client != null)
                {
                    clientBuffers.Add(client, new byte[4096]);


                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start(client);
                }
                else
                {
                    Console.WriteLine("no client connected");
                }
            }
        }


        // method to handle the client data , send and receive.
        public void HandleClient(object clientObject)
        {
            TcpClient client = (TcpClient)clientObject;

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
                //fire the event SendData and take the string data.
                SendTheClientData.Invoke(this, data);

                byte[] response = Encoding.ASCII.GetBytes(data);
                // Send the response to all connected clients to check if all data was received.
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

}

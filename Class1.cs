using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
namespace server
{
    public class Server
    {   //public Server() {

        // } 
        private static TcpListener tcpListener;
        private static readonly IPAddress ipAddress = IPAddress.Parse("192.168.1.102");
        private static readonly int port = 2909;
        private static List<TcpClient> clients = new List<TcpClient>();

       public static void Main(string[] args)
        {
            tcpListener = new TcpListener(ipAddress, port);
            tcpListener.Start();
            Console.WriteLine("Server started.");

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                clients.Add(client);

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);
            }
        }

        private static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream clientStream = client.GetStream();

            byte[] buffer = new byte[4096];
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
                clientStream.Write(response, 0, response.Length);
                clientStream.Flush();
            }

            client.Close();
            clients.Remove(client);
        }
    }
}
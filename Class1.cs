using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;

namespace server
{
    public class Server
    {
        private static TcpListener tcpListener;
        private static readonly IPAddress ipAddress = IPAddress.Parse("192.168.113.92");
        private static readonly int port = 2909;
        private static Dictionary<TcpClient, byte[]> clientBuffers = new Dictionary<TcpClient, byte[]>();
        public event EventHandler<byte[]> SendTheClientData;



        public void Start()
        {  // listen for the incomming Connnections.
            try
            {
                tcpListener = new TcpListener(ipAddress, port);
                tcpListener.Start();
                Console.WriteLine("Server started.");



                while (true)
                { // Accept the incomming Connections.
                    TcpClient client = tcpListener.AcceptTcpClient();
                    if (client != null)
                    {
                        try
                        {
                            clientBuffers.Add(client, new byte[4096]);
                        }
                        catch (ArgumentException ex)
                        {
                            // Handle exception when a client with duplicate key already exists in the dictionary.
                            Console.WriteLine("Client already exists in clientBuffers: " + ex.Message);
                             continue; // Move on to the next iteration of the loop.
                        }
                        //Start a thread  for each connected Client.
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                        clientThread.Start(client);
                    }
                    else
                    {
                        Console.WriteLine("no client connected");
                    }
                }
            }
            catch (SocketException ex)
            {
                // Handle specific socket-related exceptions
                Console.WriteLine("SocketException occurred: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handle any other exceptions that might occur during server startup.
                Console.WriteLine("Error occurred during server startup: " + ex.Message);

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

                byte[] response = Encoding.ASCII.GetBytes(data);
                //Fire the event SendTheClientData and take the data as an array of byte.
                SendTheClientData.Invoke(this, response);

                // Send the response to all connected clients to check if all data was received.
                foreach (TcpClient connectedClient in clientBuffers.Keys)
                {    // Get the client's buffer
                    byte[] buffer2 = clientBuffers[connectedClient];
                    try
                    {
                        NetworkStream connectedClientStream = connectedClient.GetStream();
                        connectedClientStream.Write(response, 0, response.Length);
                        connectedClientStream.Flush();
                    }
                    catch (IOException ex)
                    {
                        // Handle exception when there's an issue with the client's network stream (e.g., client disconnects).
                        Console.WriteLine("IOException occurred when sending data to client: " + ex.Message);

                        //  remove the disconnected client from the clientBuffers dictionary:
                        clientBuffers.Remove(connectedClient);

                        // Continue with the next client
                        continue;
                    }
                    catch (Exception ex)
                    {
                        // Handle any other exceptions that might occur during the write operation.
                        Console.WriteLine("Error occurred when sending data to client: " + ex.Message);
                    }
                }


            }

            client.Close();
            clientBuffers.Remove(client);
        }
        public void Stop()
        {
            tcpListener.Stop();
            foreach (TcpClient client in clientBuffers.Keys)
            {
                try
                {
                    NetworkStream clientStream = client.GetStream();
                    clientStream.Close();
                    client.Close();
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that might occur during closing the client connections.
                  
                    Console.WriteLine("Error closing client connection: " + ex.Message);
                }
            }

            // Clear the clientBuffers dictionary to remove all clients.
            clientBuffers.Clear();
        }

    }

}

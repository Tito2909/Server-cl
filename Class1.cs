using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

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
        public static void Main(string[] args)
        {
            tcpListener = new TcpListener(ipAddress, port);
            tcpListener.Start();
            Console.WriteLine("Server started.");

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                clientBuffers.Add(client, new byte[4096]);

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);

                Thread sendToThirdSideThread = new Thread(new ThreadStart(SendToThirdSide));
                sendToThirdSideThread.Start();

                // Start a separate thread to receive data from the third side
                Thread receiveFromThirdSideThread = new Thread(new ThreadStart(ReceiveFromThirdSide));
                receiveFromThirdSideThread.Start();
            }
        }

        private static void ReceiveFromThirdSide()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = thirdSideStream.Read(buffer, 0, buffer.Length);
                    string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
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
                static void SendToThirdSide()
                {
                    TcpClient thirdSideClient = new TcpClient();

                    try
                    {
                        string thirdSideIPAddress = "ThirdSideIPAddress";
                        int thirdSidePort = 2808;
                        thirdSideClient.Connect("ThirdSideIPAddress", thirdSidePort);

                        foreach (byte[] buffer in clientBuffers.Values)
                        {
                            // Send the buffer to the third side
                            thirdSideClient.GetStream().Write(buffer, 0, buffer.Length);
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
                static void HandleClient(object obj)
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
                    }

                    client.Close();
                    clientBuffers.Remove(client);
                }
            }
        }
    

 

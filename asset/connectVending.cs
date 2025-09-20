using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System;

public class connectVending : MonoBehaviour
{
    public string command;
    public int itemCheck = 0, countItem1 = -1, countItem2 = -1, countItem3 = -1;
    public float StockCheckDelay;
    public static IPAddress vendronIP = IPAddress.Parse("127.0.0.1");

    public class stateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 256;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    public static string response = string.Empty;
    public static Socket sender;
    public static Thread thread;
    public string[] ProductforsaleID;

    private string tmpString = "";


    // connect vending
    private void Start()
    {
        ConnectThread();
    }

    void ConnectThread()
    {
        thread = new Thread(new ThreadStart(connect))
        {
            IsBackground = true
        };
        thread.Start();
    }

    void stop()
    {
        thread.Abort();
    }

    void disConnect()
    {
        if (sender != null)
        {
            sender.Close();
        }
    }

    private void connect()
    {
        try
        {
            //socketconnection = new TcpClient("127.0.0.1", 63388);
            sender = new Socket(vendronIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(vendronIP, 63388);
            byte[] bytes = new byte[512 * 1024];
            while (true)
            {
                using (NetworkStream stream = new NetworkStream(sender))
                {
                    int length;
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incommingData = new byte[length];
                        Array.Copy(bytes, 0, incommingData, 0, length);
                        string serverMessage = Encoding.ASCII.GetString(incommingData);
                        processingData(serverMessage);
                        command = serverMessage;
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket Exception : " + socketException);
        }
    }

    void processingData(string serverMessage)
    {
        string[] msg = serverMessage.Split('#');
        for (var i = 0; i < msg.Length; i++)
        {
            var serverMessageItem = msg[i];
            string tmp = "";
            Debug.Log(serverMessageItem);
            if (serverMessageItem.StartsWith("External Sale Ui Plugin;AvailableStock;result="))
            {
                tmp = serverMessage.Replace("External Sale Ui Plugin;AvilableStock;result=", "");
                switch (itemCheck)
                {
                    case 1:
                        countItem1 = Convert.ToInt32(tmp);
                        StockCheckDelay = 0;// check time out user play = 0
                        break;
                    case 2:
                        countItem2 = Convert.ToInt32(tmp);
                        StockCheckDelay = 0;// check time out user play = 0
                        break;
                    case 3:
                        countItem3 = Convert.ToInt32(tmp);
                        StockCheckDelay = 0;// check time out user play = 0
                        break;
                    default: Debug.Log("Error"); break;
                }

                tmpString = tmp;
            }
        }
    }

    void send(string cmd)
    {
        //Debug.Log(cmd);
        if (sender == null)
        {
            return;
        }
        try
        {
            NetworkStream stream = new NetworkStream(sender);
            if (stream.CanWrite)
            {
                string clientMessage = "External Sale UI;GetProductsForSale;#";
                byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception : " + socketException);
        }
    }


    // control vending
    Socket getScoket()
    {
        var ip = vendronIP;
        var ipEndPoint = new IPEndPoint(ip, 63388);
        try
        {
            sender = new Socket(vendronIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(vendronIP, 63388);
            return sender;
        }
        catch (SocketException socketException)
        {
            Debug.Log("on connect vendron " + socketException);
            return null;
        }
    }

    static void closeConnection(Socket sender)
    {
        if (sender != null)
        {
            sender.Disconnect(false);
            sender.Shutdown(SocketShutdown.Both);
            sender.Dispose();
            sender = null;
        }
    }

    static ManualResetEvent connectDone = new ManualResetEvent(false);
    static ManualResetEvent sendDone = new ManualResetEvent(false);
    static ManualResetEvent receiveDonw = new ManualResetEvent(false);

    public void sendMsgToVending(string message)
    {
        Socket sender = getScoket();
        try
        {
            byte[] bytes = new byte[5096];
            byte[] msg = Encoding.ASCII.GetBytes(message);
            int bytesSent = sender.Send(msg);
            int bytesRec = sender.Receive(bytes);
        }
        catch (Exception ex)
        {
            Debug.Log("Error " + ex);
        }
        finally
        {
            //closeConnection(sender);
        }
    }

    static void receive(Socket client)
    {
        try
        {
            // Create the state object;
            stateObject state = new stateObject
            {
                workSocket = client
            };
            // Begin receiving the data from the remote device.
            client.BeginReceive(state.buffer, 0, stateObject.BufferSize, 0, new AsyncCallback(receiveCallback), state);
        }
        catch (Exception ex)
        {
            Debug.Log("Error " + ex);
        }
    }

    static void receiveCallback(IAsyncResult ar)
    {
        try
        {
            String content = String.Empty;
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            stateObject state = (stateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                // check for end-of-file tag. If it is not there, read
                // more data.
                content = state.sb.ToString();
                Debug.Log(content);
                if (content.IndexOf("<EOR>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.
                    Debug.Log("Read " + content.Length + " bytes from socket. \n Data : " + content);
                    // Echo the data back to the client
                    send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, stateObject.BufferSize, 0, new AsyncCallback(receiveCallback), state);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Error " + ex);
        }
    }

    static void send(Socket client, String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data);
        // Begin sending the data to the remote device.
        client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(sendCallback), client);
    }

    static void sendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;
            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);
            Debug.Log("Sent " + bytesSent + " bytes to server.");
            // Signal that all bytes have been sent.
            sendDone.Set();
        }
        catch (Exception ex)
        {
            Debug.Log("Error " + ex);
        }
    }

    static void connectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;
            // Complete the connection.
            client.EndConnect(ar);
            Debug.Log("Socket connected to " + client.RemoteEndPoint.ToString());
            // Signal that the connection has been made.
            connectDone.Set();
        }
        catch (Exception ex)
        {
            Debug.Log("Error " + ex);
        }
    }

    //private void OnGUI()
    //{
    //        GUI.Label(new Rect(10, 10, 500, 20), "command : " + command);
    //        GUI.Label(new Rect(10, 30, 500, 20), "sender : " + sender.ToString());
    //        GUI.Label(new Rect(10, 50, 500, 20), "tmp : " + tmpString);
      
    //}
}

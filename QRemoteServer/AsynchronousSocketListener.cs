using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace QRemoteServer
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public static int BufferSize = 256;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public Image img;
        public int quality;
    }
    public class AsynchronousSocketListener
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static ManualResetEvent sendIntDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static IProgress<string> log;

        public AsynchronousSocketListener()
        {
        }
        public async static void StartListening(TextBox logTextBox, TextBox ipTextBox,string ip = "auto", int port = 11000)
        {
            var log_tb = new Progress<string>(text => logTextBox.Text = DateTime.Now + ": " + text + "\r\n" + logTextBox.Text);
            var ip_tb = new Progress<string>(text => ipTextBox.Text += text);
            await Task.Factory.StartNew<bool>(
                () => AsynchronousSocketListener.StartListening(log_tb, ip_tb, ip, port), TaskCreationOptions.LongRunning);
        }
        private static bool StartListening(IProgress<string> log, IProgress<string> ip_tb, string ip, int port)
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            if (ip == "auto")
                ip = Utils.GetLocalIPAddress();
            IPAddress ipAddress = IPAddress.Parse(ip);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            AsynchronousSocketListener.log = log;
            ip_tb.Report(ipAddress + ":" + port);
            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    log.Report("Ожидание соединения...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                log.Report(e.Message);
            }

            return true;

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();
            log.Report("Соединение установлено");
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }
        public static void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);
            try
            {
                if (bytesRead > 0)
                {
                    //log.Report("Получено "+ bytesRead+" байт");
                    string str = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                    //Console.WriteLine(str);
                    //log.Report(str);
                    if (str.Contains("scrSize="))
                    {
                        state.quality = Convert.ToInt32(str.Split('=')[1]);
                        state.img = Utils.ImageFromScreen(true);
                        int size = Utils.ImageToByte(state.img, state.quality).Length;
                        //log.Report(size.ToString());
                        Console.WriteLine(size);
                        sendIntDone.Reset();
                        Send(handler, size);
                        sendIntDone.WaitOne();
                    }
                    else if (str == "fuckOff")
                    {
                        log.Report("Клиент отключен от сервера");
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        return;
                    }
                    else if (str == "scr")
                    {
                        sendDone.Reset();
                        Send(handler, state.img, state.quality);
                        sendDone.WaitOne();
                        state.img.Dispose();
                    }
                    state.buffer = new byte[StateObject.BufferSize];
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
            }
            catch(Exception e)
            {
                log.Report(e.Message);
            }
        }

        private static void Send(Socket handler, Image data, int quality)
        {
            sendDone.Reset();
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Utils.ImageToByte(data, quality);
            // Begin sending the data to the remote device.  
            try
            {
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendImgCallback), handler);
            }
            catch (Exception e)
            {
                log.Report(e.ToString());
            }
        }
        private static void Send(Socket handler, int data)
        {
            sendIntDone.Reset();
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = BitConverter.GetBytes(data);
            // Begin sending the data to the remote device.
            try
            {
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendIntCallback), handler);
            }
            catch(Exception e)
            {
                log.Report(e.Message);
            }
        }

        private static void SendImgCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                //log.Report("Отправлено " + bytesSent + " байт");
                sendDone.Set();
            }
            catch (Exception e)
            {
                log.Report(e.Message);
            }
        }
        private static void SendIntCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                //log.Report("Отправлено " + bytesSent + " байт");
                sendIntDone.Set();
            }
            catch (Exception e)
            {
                log.Report(e.Message);
            }
        }
    }
}

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
    // Объект для асинхронного чтения данных клиента
    public class StateObject
    {
        // Клиентский сокет
        public Socket workSocket = null;
        // Размер буффера  
        public static int BufferSize = 256;
        // Буффер
        public byte[] buffer = new byte[BufferSize];
        // Изображение, которое будет отправлено клиенту по запросу
        public Image img;
        // Качество изображения
        public int quality;
    }
    public static class AsynchronousSocketListener
    {
        // Сигнал для потока 
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        // Вывод строки
        private static IProgress<string> log;
        
        /// <summary>
        /// Асинхронный запуск прослушивания
        /// </summary>
        /// <param name="logTextBox">TextBox для вывода лога</param>
        /// <param name="ipTextBox">TextBox для вывода данных для коннекта</param>
        /// <param name="ip">IP адрес</param>
        /// <param name="port">Порт</param>
        public async static void StartListening(TextBox logTextBox, TextBox ipTextBox,string ip = "auto", int port = 11000)
        {
            var log_tb = new Progress<string>(text => logTextBox.Text = DateTime.Now + ": " + text + "\r\n" + logTextBox.Text);
            var ip_tb = new Progress<string>(text => ipTextBox.Text += text);
            await Task.Factory.StartNew<bool>(
                () => AsynchronousSocketListener.StartListening(log_tb, ip_tb, ip, port), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Запуск прослушивания
        /// </summary>
        /// <param name="log">IProgress для вывода сообщений в лог</param>
        /// <param name="ip_tb">IProgress для вывода данных для коннекта</param>
        /// <param name="ip">IP</param>
        /// <param name="port">Порт</param>
        /// <returns></returns>
        private static bool StartListening(IProgress<string> log, IProgress<string> ip_tb, string ip, int port)
        {
            // Разбираемся с IP
            if (ip == "auto")
                ip = Utils.GetLocalIPAddress();
            IPAddress ipAddress = IPAddress.Parse(ip);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            AsynchronousSocketListener.log = log;
            ip_tb.Report(ipAddress + ":" + port);
            // Создаем сокет  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            // Запускаем прослушивание 
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Сбрасываем событие
                    allDone.Reset();

                    log.Report("Ожидание соединения...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Ждем соедининия, преждем чем продолжить ждать следующее
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
            // Сигнал главному потоку продолжиться
            allDone.Set();
            log.Report("Соединение установлено");
            // Получаем сокет
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            // Создаем новый стейтмент
            StateObject state = new StateObject();
            state.workSocket = handler;
            // Начинаем прием данных от клиента
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }
        public static void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            // Получаем данные от клиента
            int bytesRead = handler.EndReceive(ar);
            try
            {
                // Определяем, что требуется клиенту
                if (bytesRead > 0)
                {
                    string str = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                    if (str.Contains("scrSize=")) // Размер скриншота экрана
                    {
                        state.quality = Convert.ToInt32(str.Split('=')[1]);
                        if (state.img != null)
                            state.img.Dispose();
                        state.img = Utils.ImageFromScreen(true);
                        int size = Utils.ImageToByte(state.img, state.quality).Length;
                        Send(state, size);
                    }
                    else if (str == "fuckOff") // Клиент отключается
                    {
                        log.Report("Клиент отключен от сервера");
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        return;
                    }
                    else if (str == "scr") // Скриншот экрана
                    {
                        Send(state, state.img, state.quality);
                        state.img.Dispose();
                    }
                    else if (str == "qhi") // Проверка состояния сервера
                    {
                        log.Report("Кто-то проверил состояние сервера");
                        Send(state, "qhi");
                    }
                    state.buffer = new byte[StateObject.BufferSize]; // Очистка буффера
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
            }
            catch(Exception e)
            {
                log.Report(e.Message);
            }
        }

        /// <summary>
        /// Отправка изображения
        /// </summary>
        /// <param name="state">Объект с информацией о соединении</param>
        /// <param name="data">Изображение</param>
        /// <param name="quality">Качество</param>
        private static void Send(StateObject state, Image data, int quality)
        {
            byte[] byteData = Utils.ImageToByte(data, quality);
            Send(state, byteData);
        }

        /// <summary>
        /// Отправка целого числа
        /// </summary>
        /// <param name="state">Объект с информацией о соединении</param>
        /// <param name="data">Целое число</param>
        private static void Send(StateObject state, int data)
        { 
            byte[] byteData = BitConverter.GetBytes(data);
            Send(state, byteData);
        }

        /// <summary>
        /// Отправка строки
        /// </summary>
        /// <param name="state">Объект с информацией о соединении</param>
        /// <param name="data">Строка</param>
        private static void Send(StateObject state, string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            Send(state, byteData);
        }
        /// <summary>
        /// Отправка массива байт
        /// </summary>
        /// <param name="state">Объект с информацией о соединении</param>
        /// <param name="byteData">Массив байт</param>
        private static void Send(StateObject state, byte[] byteData)
        {
            state.workSocket.Send(byteData, 0, byteData.Length, 0);
        }
    }
}

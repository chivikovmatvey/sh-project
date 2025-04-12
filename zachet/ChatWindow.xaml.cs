using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace WpfChatApp
{
    public partial class ChatWindow : Window
    {
        private readonly string clientName;
        private readonly int listenPort;
        private readonly int sendPort;
        private Socket listenSocket;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning;

        public ChatWindow(string clientName, int listenPort, int sendPort)
        {
            InitializeComponent();

            this.clientName = clientName;
            this.listenPort = listenPort;
            this.sendPort = sendPort;
            this.isRunning = true;

            Title = $"Чат - {clientName}";

            InitializeChat();
        }

        private void InitializeChat()
        {
            try
            {
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), listenPort));
                listenSocket.Listen(1);

                cancellationTokenSource = new CancellationTokenSource();
                System.Threading.Tasks.Task.Run(StartListening, cancellationTokenSource.Token);

                AppendMessage($"{clientName} готов к общению");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}");
            }
        }

        private void StartListening()
        {
            while (isRunning && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                Socket client = null;
                try
                {
                    var asyncResult = listenSocket.BeginAccept(null, null);

                    // Ждем подключения с возможностью отмены
                    WaitHandle.WaitAny(new[] { asyncResult.AsyncWaitHandle, cancellationTokenSource.Token.WaitHandle });

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    client = listenSocket.EndAccept(asyncResult);
                    var buffer = new byte[1024];
                    var received = client.Receive(buffer);

                    if (received > 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, received);
                        AppendMessage($"Собеседник: {message}");
                    }
                }
                catch (Exception) when (cancellationTokenSource.Token.IsCancellationRequested || !isRunning)
                {
                    // Нормальное завершение
                    break;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show($"Ошибка приема: {ex.Message}"));
                    break;
                }
                finally
                {
                    client?.Close();
                }
            }
        }

        private void SendMessage()
        {
            var message = messageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), sendPort));
                    var data = Encoding.UTF8.GetBytes(message);
                    socket.Send(data);
                }

                AppendMessage($"Вы: {message}");
                messageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}");
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void AppendMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                chatTextBox.AppendText($"{message}\n");
                chatTextBox.ScrollToEnd();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            isRunning = false;
            cancellationTokenSource?.Cancel();

            try
            {
                listenSocket?.Close();
                listenSocket?.Dispose();
            }
            catch { }

            cancellationTokenSource?.Dispose();
            base.OnClosed(e);
        }
    }
}
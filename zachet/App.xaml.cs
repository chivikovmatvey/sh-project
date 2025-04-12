using System.Windows;

namespace WpfChatApp
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var chatWindow1 = new ChatWindow("Клиент 1", 8001, 8002);
            var chatWindow2 = new ChatWindow("Клиент 2", 8002, 8001);

            chatWindow1.Show();
            chatWindow2.Show();
        }
    }
}
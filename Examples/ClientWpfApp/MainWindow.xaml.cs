using IpcAnonymousPipes;
using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace ClientWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PipeClient pipeClient;

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;

            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            pipeClient = new PipeClient(args[0], args[1], ReceiveAction);
            pipeClient.Disconnected += PipeClient_Disconnected;
            pipeClient.RunAsync();
        }

        private void PipeClient_Disconnected(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            pipeClient.Dispose();
        }

        private void ReceiveAction(BlockingReadStream stream)
        {
            string text = Encoding.UTF8.GetString(stream.ReadToEnd());
            Application.Current.Dispatcher.Invoke(() => log.AppendText(text + "\n"));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            pipeClient.Send(Encoding.UTF8.GetBytes(messageToSend.Text));
        }
    }
}

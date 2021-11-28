using IpcAnonymousPipes;
using System;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace ServerWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PipeServer pipeServer;

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;

            pipeServer = new PipeServer(ReceiveAction);
            pipeServer.Disconnected += PipeServer_Disconnected;
            Process.Start("ClientWpfApp.exe", string.Join(" ", pipeServer.ClientInputHandle, pipeServer.ClientOutputHandle));
            pipeServer.RunAsync();
        }

        private void PipeServer_Disconnected(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            pipeServer.Dispose();
        }

        private void ReceiveAction(BlockingReadStream stream)
        {
            string text = Encoding.UTF8.GetString(stream.ReadToEnd());
            Application.Current.Dispatcher.Invoke(() => log.AppendText(text + "\n"));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            pipeServer.Send(Encoding.UTF8.GetBytes(messageToSend.Text));
        }
    }
}

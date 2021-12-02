using IpcAnonymousPipes;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace ServerWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // ServerWpfApp project does not reference ClientWpfApp. This is just to demonstrate they are totally independent in 2 different AppDomains.
#if DEBUG
        const string ClientWpfAppPath = @"..\..\..\..\ClientWpfApp\bin\Debug\netcoreapp3.1\ClientWpfApp.exe";
#else
        const string ClientWpfAppPath = @"..\..\..\..\ClientWpfApp\bin\Release\netcoreapp3.1\ClientWpfApp.exe";
#endif

        PipeServer pipeServer;

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;

            if (File.Exists(ClientWpfAppPath))
            {
                pipeServer = new PipeServer(ReceiveAction);
                pipeServer.Disconnected += PipeServer_Disconnected;
                Process.Start(ClientWpfAppPath, string.Join(" ", pipeServer.ClientInputHandle, pipeServer.ClientOutputHandle));
                pipeServer.RunAsync();
                pipeServer.WaitForClient(TimeSpan.FromSeconds(15));
            }
            else
            {
                MessageBox.Show("Please build ClientWpfApp before starting ServerWpfApp!", "Build ClientWpfApp", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }

        private void PipeServer_Disconnected(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            pipeServer?.Dispose();
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

﻿using System;
using System.Text;
using System.Windows;
using IpcAnonymousPipes;

namespace ClientWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PipeClient Client { get; }

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;

            // If you want to debug comment out this line, rebuild the solution,
            // start ServerWpfApp in debug mode, then attach the debugger to ClientWpfApp in 15 seconds
            //System.Threading.Thread.Sleep(15000);

            // Create pipe client
            // The empty constructor parses command line arguments to get the pipe handles.
            Client = new PipeClient();
            Client.Disconnected += PipeClient_Disconnected;
            Client.ReceiveAsync(ReceiveAction);
        }

        private void PipeClient_Disconnected(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Client.Dispose();
        }

        private void ReceiveAction(PipeMessageStream stream)
        {
            string text = Encoding.UTF8.GetString(stream.ReadToEnd());
            Application.Current.Dispatcher.Invoke(() => log.AppendText(text + "\n"));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Client.Send(Encoding.UTF8.GetBytes(messageToSend.Text));
        }
    }
}

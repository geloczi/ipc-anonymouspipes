﻿using IpcAnonymousPipes;
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

        PipeServer Server { get; }

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
            IsEnabled = false;

            if (File.Exists(ClientWpfAppPath))
            {
                // Create pipe server
                Server = new PipeServer();
                Server.Connected += PipeServer_Connected;
                Server.Disconnected += PipeServer_Disconnected;

                // Start client process with command line arguments
                Process.Start(ClientWpfAppPath, Server.GetClientArgs());

                // Receiving on background thread
                Server.ReceiveAsync(stream =>
                {
                    string text = Encoding.UTF8.GetString(stream.ReadToEnd());
                    Application.Current.Dispatcher.Invoke(() => log.AppendText(text + "\n"));
                });
            }
            else
            {
                MessageBox.Show("Please build ClientWpfApp before starting ServerWpfApp!", "Build ClientWpfApp", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }

        private void PipeServer_Connected(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Title = Title.Replace(" (waiting for client...)", "");
                IsEnabled = true;
            }));
        }

        private void PipeServer_Disconnected(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            Server?.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Server.Send(Encoding.UTF8.GetBytes(messageToSend.Text));
        }
    }
}

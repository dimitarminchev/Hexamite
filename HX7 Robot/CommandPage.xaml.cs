using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HX7_Robot
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CommandPage : Page
    {
        // Socket
        private SocketSettings _settings;
        private SocketInterface _socket;

        // Constructor
        public CommandPage()
        {
            this.InitializeComponent();
        }

        // Landing
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _settings = e.Parameter as SocketSettings;
            if (_socket != null)
            {
                _socket.Close();
                _socket.OnDataRecived -= socket_OnDataRecived;
                _socket = null;
            }
            _socket = new SocketInterface(_settings.ip, _settings.port);
            _socket.Connect();
            _socket.OnDataRecived += socket_OnDataRecived;
            _socket.OnError += _socket_OnError;

        }

        // Receive
        private void socket_OnDataRecived(string data)
        {
            BoxReceive.Text += data;
        }

        // Error
        private void _socket_OnError(string message)
        {
            BoxReceive.Text += "Error: " + message + Environment.NewLine;
        }

        // Send
        private void Send_Button_Click(object sender, RoutedEventArgs e)
        {
            string cmd = CheckSum(BoxSend.Text);
            _socket.Send(cmd);
        }

        // HX7 Accepted Check Sum
        private string CheckSum(string input)
        {
            int checksum = 0; // CheckSum
            foreach (var item in input) // Compute the CheckSum of the string
                checksum += (int)item; // Accumulate ASCII code to the CheckSum
            string hex = checksum.ToString("X"); // Convert CheckSum in hexadecimal format                        
            return input + '/' + hex + '\n';
        }

        // Back
        private void Back_Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}

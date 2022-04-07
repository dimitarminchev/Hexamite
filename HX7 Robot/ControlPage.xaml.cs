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
    public sealed partial class ControlPage : Page
    {
        // Socket
        private SocketSettings _settings;
        private SocketInterface _socket;

        // Left & Right
        private char L = '^';
        private char R = '^';

        // Constructor
        public ControlPage()
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
                _socket = null;
            }
            _socket = new SocketInterface(_settings.ip, _settings.port);
            _socket.Connect();
        }

        // Left
        private void Left_Value_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            double temp = Left.Value;
            if (temp > 0) L = (char)(temp + 96);
            if (temp < 0) L = (char)(Math.Abs(temp) + 64);
            string cmd = CheckSum(L.ToString() + R.ToString());
            _socket.Send(cmd);
        }

        // Right
        private void Right_Value_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            double temp = Right.Value;
            if (temp > 0) R = (char)(temp + 96);
            if (temp < 0) R = (char)(Math.Abs(temp) + 64);
            string cmd = CheckSum(L.ToString() + R.ToString());
            _socket.Send(cmd);
        }

        // Stop Button
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            Left.Value = Right.Value = 0; // Set Sliders to Zero
            string cmd = CheckSum("^^"); // Send Full Stop
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HX7_Render
{
    /// <summary>
    /// Interaction logic for ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        // Serial
        private SerialInterface _port;
        public delegate void PortSelected(SerialInterface port);
        public event PortSelected onPortSelected;

        // Constructor
        public ConnectWindow()
        {
            InitializeComponent();

            // Initialize Serial Interface
            _port = new SerialInterface();
            SerialSettings mySerialSettings = _port.CurrentSerialSettings;
            SerialPortBox.ItemsSource = mySerialSettings.PortNameCollection;
            BaudRateBox.ItemsSource = mySerialSettings.BaudRateCollection;
        }

        // Connect Button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            onPortSelected(_port);
            Close();
        }
    }
}

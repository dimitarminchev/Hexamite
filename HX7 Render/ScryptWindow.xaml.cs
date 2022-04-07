using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
    /// Interaction logic for ScryptWindow.xaml
    /// </summary>
    public partial class ScryptWindow : Window
    {
        // Serial Interface
        public SerialInterface _port;

        // File Path
        private string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

        // Constructor
        public ScryptWindow()
        {
            InitializeComponent();

            // Load Scrypt
            Scrypt.Text = System.IO.File.ReadAllText(path + "\\Scrypt.txt");
        }

        // Execute Button
        private void Execute_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] lines = Regex.Split(Scrypt.Text, "\r\n");
                foreach (var line in lines)
                {                    
                    if (line.Length == 0) continue; // empty line                    
                    if (line.Substring(0, 1) == "#") continue; // comment line
                    string command = CheckSum(line,'\n');
                    _port.Send(command); 
                    // Debug.Write(command);
                    Thread.Sleep(200); 
                }
                MessageBox.Show("Script Send Successful!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception Error)
            {
                MessageBox.Show(Error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// HX accepted checksum
        /// </summary>
        /// <param name="input">Input string to form HX accepted checksum.</param>
        /// <param name="terminator">Devices: HX19 = "\r", HX7 = "\n"</param>
        /// <returns>Input string and HX accepted checksum.</returns>
        private string CheckSum(string input, char terminator)
        {
            int checksum = 0; // CheckSum
            foreach (var item in input) // Compute the CheckSum of the string
                checksum += (int)item; // Accumulate ASCII code to the CheckSum
            string hex = checksum.ToString("X"); // Convert CheckSum in hexadecimal format                        
            return input + "/" + hex + terminator;
        }

        // Exit Button
        private void Exit_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

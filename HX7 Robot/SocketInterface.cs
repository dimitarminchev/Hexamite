using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace HX7_Robot
{
    // Socket Settings
    public class SocketSettings
    {
        // data
        public string ip;
        public int port;

        // constructor
        public SocketSettings(string _ip, int _port)
        {
            this.ip = _ip;
            this.port = _port;
        }
    }

    // Socket Interface
    public class SocketInterface
    {
        private readonly string _ip;
        private readonly int _port;
        private StreamSocket _socket;
        private DataWriter _writer;
        private DataReader _reader;

        public delegate void Error(string message);
        public event Error OnError;

        public delegate void DataRecived(string data);
        public event DataRecived OnDataRecived;

        public string Ip { get { return _ip; } }
        public int Port { get { return _port; } }

        // Constructor
        public SocketInterface(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        // Connect
        public async void Connect()
        {
            try
            {
                var hostName = new HostName(Ip);
                _socket = new StreamSocket();
                await _socket.ConnectAsync(hostName, Port.ToString());
                _writer = new DataWriter(_socket.OutputStream);
                Read();
            }
            catch (Exception ex)
            {
                if (OnError != null) OnError(ex.Message);
            }
        }

        // Send
        public async void Send(string message)
        {
            _writer.WriteString(message);
            try
            {
                await _writer.StoreAsync();
                await _writer.FlushAsync();
            }
            catch (Exception ex)
            {
                if (OnError != null) OnError(ex.Message);
            }
        }


        // Read
        private async void Read()
        {
            _reader = new DataReader(_socket.InputStream);
            try
            {
                while (true)
                {
                    uint _size = await _reader.LoadAsync(sizeof(byte));
                    uint _count = _reader.UnconsumedBufferLength;
                    if (OnDataRecived != null) OnDataRecived(_reader.ReadString(_count));
                }
            }
            catch (Exception ex)
            {
                if (OnError != null) OnError(ex.Message);
            }
        }

        // Close
        public void Close()
        {
            _writer.DetachStream();
            _writer.Dispose();
            _reader.DetachStream();
            _reader.Dispose();
            _socket.Dispose();
        }
    }

}

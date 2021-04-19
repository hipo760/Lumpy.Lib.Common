using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Lumpy.Lib.Common.Connection.Tcp
{
    public class RxTcpClient
    {
        private readonly Subject<byte[]> _responseBroker;
        private readonly Subject<byte[]> _requestBroker;
        private readonly Subject<Exception> _connectionException;
        private TcpClient _tcpClient;
        private CancellationTokenSource _cts;
        private IDisposable _requestSub;

        public string Ip { get; private set; }
        public int Port { get; private set; }
        public int BufferLength { get; private set; }

        public IObservable<byte[]> ResponseBroker => _responseBroker;
        public IObserver<byte[]> RequestBroker => _requestBroker;
        public IObservable<Exception> ConnectionException => _connectionException;


        public RxTcpClient(string ip, int port, int bufferLength = 1024)
        {
            Ip = ip;
            Port = port;
            BufferLength = bufferLength;
            Log.Logger.Verbose("Ip: {ip}, Port: {port}", Ip, Port);
            _requestBroker = new Subject<byte[]>();
            _responseBroker = new Subject<byte[]>();
            _connectionException = new Subject<Exception>();
        }


        public void Connect()
        {
            _tcpClient = new TcpClient();
            _cts = new CancellationTokenSource();
            try
            {
                _tcpClient.Connect(Ip, Port);
                _requestSub = _requestBroker.Subscribe(Send);
                Listen();
            }
            catch (Exception e)
            {
                Log.Logger.Error("Exception: {e}", e);
                _connectionException.OnNext(e);
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            try
            {
                _requestSub?.Dispose();
                _tcpClient?.Close();
            }
            catch (Exception e)
            {
                Log.Logger.Error("Exception: {e}", e);
                _connectionException.OnNext(e);
            }
        }

        public void Reconnect(TimeSpan waitInterval)
        {
            Disconnect();
            Task.Delay(waitInterval).Wait();
            Connect();
        }


        private void Send(byte[] data)
        {
            try
            {
                _tcpClient?.GetStream().Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Log.Logger.Error("Exception: {e}", e);
            }
        }


        private void Listen()
        {
            Task.Run(() =>
            {
                try
                {
                    var ns = _tcpClient.GetStream();
                    IEnumerable<byte> completeArray = new byte[0];
                    //StringBuilder myCompleteMessage = new StringBuilder();
                    while (ns.CanRead)
                    {
                        var myReadBuffer = new byte[BufferLength];
                        var dataEvent = completeArray as byte[] ?? completeArray.ToArray();
                        do
                        {
                            var numberOfBytesRead = ns.Read(myReadBuffer, 0, myReadBuffer.Length);
                            completeArray = dataEvent.Concat(myReadBuffer.Take(numberOfBytesRead).ToArray());
                        } while (ns.DataAvailable);

                        if (!dataEvent.Any()) continue;
                        _responseBroker.OnNext(dataEvent);
                        completeArray = new byte[0];
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.Error("Listen Exception: {e}", e);
                }
            },_cts.Token);
        }
    }
}
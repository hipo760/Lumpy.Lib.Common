using System;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lumpy.Lib.Common.Broker;
using Serilog;

namespace Lumpy.Lib.Common.Connection.Ws
{
    public class RxWsClient
    {
        private readonly ILogger _log;
        private ClientWebSocket _wsClient;
        protected CancellationTokenSource Cts;
        public WebSocketState WebSocketState => _wsClient.State;
        private IDisposable _requestSub;

        public string RemoteUrl { get; set; }
        public int BufferSize { get; }
        public Action<Exception> ExceptionEvent { get; set; }
        public Subject<string> RequestBroker { get; }
        public Subject<string> ResponseBroker { get; }

        public RxWsClient(ILogger log, string remote,int bufferSize = 1024)
        {
            _log = log;
            RemoteUrl = remote;
            BufferSize = bufferSize;
            _wsClient = new ClientWebSocket();
            Cts = new CancellationTokenSource();
            RequestBroker = new Subject<string>();
            ResponseBroker = new Subject<string>();
        }

        public virtual Task Connect()
        {
            _log.Information("Connecting...");
            return Task.Run(() =>
            {
                var serverUri = new Uri(RemoteUrl);
                Cts = new CancellationTokenSource();
                _wsClient = new ClientWebSocket();
                //_wsClient.Options.RemoteCertificateValidationCallback += (sender, certificate, chain, errors) => true;
                _wsClient.ConnectAsync(serverUri, Cts.Token).Wait();
            }).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    _log.Information("Connecting...done, listing...");
                    _requestSub = RequestBroker
                        .SubscribeOn(NewThreadScheduler.Default)
                        .Subscribe(Send);
                    _log.Information("Ready for request.");
                    Task.Run(Echo, Cts.Token);
                }
                else if (t.IsFaulted && t.Exception != null)
                {
                    _log.Error("Exception {e}", t.Exception.Message);
                }
            });
        }
        public virtual Task Disconnect() =>
            Task.Run(() =>
            {
                //_log.Debug("Cancel token");
                _log.Information("Disconnect...");
                _requestSub?.Dispose();
                if (_wsClient.State == WebSocketState.Open)
                {
                    _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "", Cts.Token).Wait();
                }
                Cts?.Cancel();
                _wsClient?.Dispose();
                _log.Information("Disconnect...Done");
            });

        private async void Send(string request)
        {
            _log.Verbose("Send request: {request}",request);
            try
            {
                var encoded = Encoding.UTF8.GetBytes(request);
                var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
                await _wsClient.SendAsync(buffer, WebSocketMessageType.Text, true, Cts.Token);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }

        private async Task Echo()
        {
            try
            {
                var buffer = new byte[BufferSize];
                var offset = 0;
                var free = buffer.Length;

                while (_wsClient.State == WebSocketState.Open)
                {
                    var bytesReceived = new ArraySegment<byte>(buffer, offset, free);
                    var result = await _wsClient.ReceiveAsync(bytesReceived, CancellationToken.None);


                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _log.Information("result.MessageType: {MessageType}", result.MessageType);
                        _log.Information("Websocket close, stop listing.");
                        break;
                    }
                    
                    
                    offset += result.Count;
                    free -= result.Count;

                    if (result.EndOfMessage)
                    {
                        var str = Encoding.UTF8.GetString(buffer, 0, offset);
                        ResponseBroker.OnNext(str);
                        buffer = new byte[BufferSize];
                        offset = 0;
                        free = buffer.Length;
                    }

                    if (free != 0) continue;
                    var newSize = buffer.Length + BufferSize;
                    var newBuffer = new byte[newSize];
                    Array.Copy(buffer, 0, newBuffer, 0, offset);
                    buffer = newBuffer;
                    free = buffer.Length - offset;
                }
            }
            catch (Exception e)
            {
                _log.Error("Exception: {e}", e.Message);
                _log.Debug("State: {state}", _wsClient.State);
                ExceptionEvent?.Invoke(e);
            }
        }
    }
}
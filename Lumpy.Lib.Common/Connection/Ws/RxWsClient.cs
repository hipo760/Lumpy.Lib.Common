using System;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lumpy.Lib.Common.Broker;
using Serilog;

namespace Lumpy.Lib.Common.Connection.Ws
{
    public class RxWsClient
    {
        private ILogger _log;
        private ClientWebSocket _wsClient;
        protected CancellationTokenSource Cts;

        private IDisposable _requestSub;

        public string RemoteUrl { get; set; }
        public Action<Exception> ExceptionEvent { get; set; }
        public DataEventBroker<string> RequestBroker { get; }
        public DataEventBroker<string> ResponseBroker { get; }

        public RxWsClient(ILogger log, string remote)
        {
            _log = log;
            RemoteUrl = remote;
            _wsClient = new ClientWebSocket();
            Cts = new CancellationTokenSource();
            RequestBroker = new DataEventBroker<string>();
            ResponseBroker = new DataEventBroker<string>();
        }

        public virtual Task Connect()
        {
            _log.Information("Connecting...");
            return Task.Run(() =>
            {
                var serverUri = new Uri(RemoteUrl);
                Cts = new CancellationTokenSource();
                _wsClient = new ClientWebSocket();
                _wsClient.Options.RemoteCertificateValidationCallback += (sender, certificate, chain, errors) => true;
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
                _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "", Cts.Token).Wait();
                Cts?.Cancel();
                _wsClient?.Dispose();
                _log.Information("Disconnect...Done");
            });

        public void Reconnect(TimeSpan waitInterval)
        {
            _log.Information("Reconnect...");
            Disconnect().Wait();
            Task.Delay(waitInterval).Wait();
            Connect().Wait();
            _log.Information("Reconnect...Done");
        }
        private async void Send(string request)
        {
            _log.Information("Send request: {request}",request);
            var encoded = Encoding.UTF8.GetBytes(request);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await _wsClient.SendAsync(buffer, WebSocketMessageType.Text, true, Cts.Token);
        }

        private async Task Echo()
        {
            try
            {
                var bufferSize = 1000;
                var buffer = new byte[bufferSize];
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
                        //TryParseAndPublish(str);
                        ResponseBroker.Publish(str);
                        bufferSize = 1000;
                        buffer = new byte[bufferSize];
                        offset = 0;
                        free = buffer.Length;
                    }

                    if (free != 0) continue;
                    var newSize = buffer.Length + bufferSize;
                    var newBuffer = new byte[newSize];
                    Array.Copy(buffer, 0, newBuffer, 0, offset);
                    buffer = newBuffer;
                    free = buffer.Length - offset;
                }
            }
            catch (Exception e)
            {
                _log.Error("Exception: {e}", e.Message);
                _log.Debug("CloseState: {state}", _wsClient.CloseStatus);
                _log.Debug("CloseStateDescription: {state}", _wsClient.CloseStatusDescription);
                ExceptionEvent?.Invoke(e);
            }
        }
    }
}
using System;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Lumpy.Lib.Common.Connection.Ws
{
    public class RxWsClient
    {
        protected readonly ILogger Log;
        
        private ClientWebSocket _wsClient;
        protected CancellationTokenSource Cts;
        private readonly Subject<Exception> _exceptionEvent;
        private readonly Subject<string> _connectionEvent;
        private IDisposable _requestSub;
        private readonly Subject<string> _requestBroker;
        private Subject<string> _responseBroker;

        public IObservable<Exception> ExceptionEvent => _exceptionEvent;
        public IObservable<string> ConnectionEvent => _connectionEvent;
        public IObserver<string> RequestBroker => _requestBroker;
        public IObservable<string> ResponseBroker => _responseBroker;
        public WebSocketState WebSocketState => _wsClient.State;
        public string RemoteUrl { get; set; }
        public int BufferSize { get; }


        public RxWsClient(string remote, int bufferSize = 1024)
        {
            Log = Serilog.Log.Logger;
            RemoteUrl = remote;
            BufferSize = bufferSize;
            _wsClient = new ClientWebSocket();
            Cts = new CancellationTokenSource();
            _requestBroker = new Subject<string>();
            _responseBroker = new Subject<string>();

            _exceptionEvent = new Subject<Exception>();
            _connectionEvent = new Subject<string>();
        }

        public virtual Task Connect()
        {
            Log.Information("Connecting...");
            return Task.Run(() =>
            {
                var serverUri = new Uri(RemoteUrl);
                Cts = new CancellationTokenSource();
                _wsClient = new ClientWebSocket();
                //_wsClient.Options.RemoteCertificateValidationCallback += (sender, certificate, chain, errors) => true;
                _wsClient.ConnectAsync(serverUri, Cts.Token).Wait();
                
            }).ContinueWith(t =>
            {
#if NET5_0
                if (t.IsCompletedSuccessfully)
                {
                    Log.Information("Connecting...done, listing...");
                    _requestSub = _requestBroker
                        .SubscribeOn(NewThreadScheduler.Default)
                        .Subscribe(Send);
                    Log.Information("Ready for request.");
                    _connectionEvent.OnNext("Connected");
                    Task.Run(Echo, Cts.Token);
                }
                else if (t.IsFaulted && t.Exception != null)
                {
                    Log.Error("Exception {e}", t.Exception.Message);
                    _exceptionEvent.OnNext(t.Exception);
                }
#elif NETSTANDARD2_0
                if (t.IsCompleted)
                {
                    Log.Information("Connecting...done, listing...");
                    _requestSub = RequestBroker
                        .SubscribeOn(NewThreadScheduler.Default)
                        .Subscribe(Send);
                    Log.Information("Ready for request.");
                    _connectionEvent.OnNext("Connected");
                    Task.Run(Echo, Cts.Token);
                }
                else if (t.IsFaulted && t.Exception != null)
                {
                    Log.Error("Exception {e}", t.Exception.Message);
                    _exceptionEvent.OnNext(t.Exception);
                }
#else
#error This code block does not match csproj TargetFrameworks list
#endif
            });
        }
        public virtual Task Disconnect() =>
            Task.Run(() =>
            {
                try
                {
                    Log.Information("Disconnect...");
                    _requestSub?.Dispose();
                    if (_wsClient.State == WebSocketState.Open)
                    {
                        _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "", Cts.Token).Wait();
                    }
                    Cts?.Cancel();
                    _wsClient?.Dispose();
                    Log.Information("Disconnect...Done");    //_log.Debug("Cancel token");
                    _connectionEvent.OnNext("Disconnected");
                }
                catch (Exception e)
                {
                    Log.Error("Exception {e}",e);
                    _exceptionEvent.OnNext(e);
                }
                
            });

        private async void Send(string request)
        {
            //if (_isLogRequestResponse) _log.Verbose("Send request: {request}", request);
            try
            {
                var encoded = Encoding.UTF8.GetBytes(request);
                var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
                await _wsClient.SendAsync(buffer, WebSocketMessageType.Text, true, Cts.Token);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                _exceptionEvent.OnNext(e);
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
                        Log.Information("result.MessageType: {MessageType}", result.MessageType);
                        Log.Information("Websocket close, stop listing.");
                        break;
                    }
                    
                    
                    offset += result.Count;
                    free -= result.Count;

                    if (result.EndOfMessage)
                    {
                        var str = Encoding.UTF8.GetString(buffer, 0, offset);
                        _responseBroker.OnNext(str);
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
                Log.Error("Exception: {e}", e.Message);
                Log.Debug("State: {state}", _wsClient.State);
                _exceptionEvent.OnNext(e);
            }
        }
    }


    
}
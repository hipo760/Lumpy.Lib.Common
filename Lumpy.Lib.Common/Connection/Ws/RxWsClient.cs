using System;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;

namespace Lumpy.Lib.Common.Connection.Ws
{
    public class RxWsClient
    {
        private readonly ILogger _log;
        private ClientWebSocket WsClient { get; set; }
        private CancellationTokenSource _cts;

        private readonly Subject<WebSocketState> _websocketStateObservable;
        private readonly Subject<Exception> _exceptionSubject;
        private readonly Subject<string> _requestSubject;
        private readonly Subject<string> _responseSubject;

        private IDisposable _requestSub;


        public IObservable<WebSocketState> WebsocketStateObservable => _websocketStateObservable;
        public IObservable<Exception> ExceptionObservable => _exceptionSubject;
        public IObserver<string> RequestObserver => _requestSubject;
        public IObservable<string> ResponseObservable => _responseSubject;
        public WebSocketState WebSocketState => WsClient.State;
        public string RemoteUrl { get; set; }
        public int BufferSize { get; }

        public RxWsClient(string remote, int bufferSize = 1024, ILogger log = null)
        {
            _log = log ?? Logger.None;
            RemoteUrl = remote;
            BufferSize = bufferSize;

            _requestSubject = new Subject<string>();
            _responseSubject = new Subject<string>();
            _websocketStateObservable = new Subject<WebSocketState>();
            _exceptionSubject = new Subject<Exception>();
        }

        public void Connect() => ConnectAsync().Wait();

        public async Task ConnectAsync()
        {

            WsClient = new ClientWebSocket();
            var serverUri = new Uri(RemoteUrl);
            _cts = new CancellationTokenSource();
            await WsClient
                .ConnectAsync(serverUri, _cts.Token)
                .ContinueWith(async t =>
                {
#if NET5_0_OR_GREATER
                    if (t.IsCompletedSuccessfully)
                    {
                        _log.Information("Connecting...done");
                        _websocketStateObservable.OnNext(WebSocketState);
                        if (WebSocketState == WebSocketState.Open)
                        {
                            await Echo();
                        }
                    }
                    else if (t.IsFaulted || t.IsCanceled || t.IsCompleted)
                    {
                        if (t.Exception != null)
                        {
                            _log.Error("Exception: {e}", t.Exception);
                            _exceptionSubject.OnNext(t.Exception);
                        }
                        _websocketStateObservable.OnNext(WebSocketState);
                    }

#elif NETSTANDARD2_0
                    if (t.IsCompleted)
	                {
                        if (WebSocketState == WebSocketState.Open)
                            {
                                await Echo();
                            }
	                }
                    else if (t.IsFaulted || t.IsCanceled)
                    {
                        if (t.Exception != null)
                        {
                            _log.Error("Exception: {e}", t.Exception);
                            _exceptionSubject.OnNext(t.Exception);
                        }
                    }
                    _websocketStateObservable.OnNext(WebSocketState);    
#else
#error This code block does not match csproj TargetFrameworks list
#endif
                });

            //if (WebSocketState == WebSocketState.Open)
            //{
            //    await Echo();
            //}
        }

        public void Close() => CloseAsync().Wait();

        public async Task CloseAsync()
        {
            try
            {
                if (WebSocketState != WebSocketState.Open) return;
                _requestSub?.Dispose();
                _cts.Cancel();
                await WsClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

            }
            catch (Exception e)
            {
                _log.Error("Exception: {e}", e);
                _exceptionSubject.OnNext(e);
            }
        }

        private async Task Send(string request)
        {
            //if (_isLogRequestResponse) _log.Verbose("Send request: {request}", request);
            try
            {
                var encoded = Encoding.UTF8.GetBytes(request);
                var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
                await WsClient.SendAsync(buffer, WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception e)
            {
                _log.Error("Exception: {e}", e);
                _exceptionSubject.OnNext(e);
            }
        }

        private async Task Echo()
        {
            _requestSub?.Dispose();
            _requestSub = _requestSubject
                .Where(m => WebSocketState == WebSocketState.Open)
                .Select(m => Observable.FromAsync(() => Send(m)))
                .Concat()
                .Subscribe();

            try
            {
                var buffer = new byte[BufferSize];
                var offset = 0;
                var free = buffer.Length;

                while (WebSocketState == WebSocketState.Open || WebSocketState == WebSocketState.CloseSent)
                {
                    var bytesReceived = new ArraySegment<byte>(buffer, offset, free);
                    var result = await WsClient.ReceiveAsync(bytesReceived, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _log.Information("A receive has completed because a close message was received.");
                        break;
                    }

                    offset += result.Count;
                    free -= result.Count;

                    if (result.EndOfMessage)
                    {
                        var str = Encoding.UTF8.GetString(buffer, 0, offset);
                        _responseSubject.OnNext(str);
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
                _websocketStateObservable.OnNext(WebSocketState);
            }
            catch (Exception e)
            {
                _log.Error("Exception: {e}", e);
                _log.Debug("State: {state}", WebSocketState);
                _websocketStateObservable.OnNext(WebSocketState);
                _exceptionSubject.OnNext(e);
            }
        }
    }
}
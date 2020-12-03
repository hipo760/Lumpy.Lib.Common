using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Lumpy.Lib.Common.Broker;
using Lumpy.Lib.Common.Connection.Ws;
using Serilog;

namespace Lumpy.Lib.Common.Connection.Exchange
{
    public abstract class ExchangeWebsocketAction:IExchangeConnectionAction
    {
        protected readonly ILogger Log;
        protected readonly RxWsClient RxWsClient;
        protected Subject<long> HeartBeatEvent;
        protected ExchangeWebsocketAction(ILogger log,string exchangeHost)
        {
            Log = log;
            RxWsClient = new RxWsClient(log, exchangeHost);
            HeartBeatEvent = new Subject<long>();
        }
        public Task Connect() => RxWsClient.Connect();
        public Task Disconnect() => RxWsClient.Disconnect();
        public virtual bool CheckConnection() => RxWsClient.WebSocketState != WebSocketState.Open;
        public void Request(string request) => RxWsClient.RequestBroker.OnNext(request);
        public IObservable<string> ResponseBroker => RxWsClient.ResponseBroker;
        public IObservable<long> HeartBeatEventBroker => HeartBeatEvent;
    }
}
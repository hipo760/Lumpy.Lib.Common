using System;
using System.Threading.Tasks;

namespace Lumpy.Lib.Common.Connection.Exchange
{
    public interface IExchangeConnectionAction
    {
        Task Connect();
        Task Disconnect();
        bool CheckConnection();
        void Request(string request);
        IObservable<string> ResponseBroker { get; }
        IObservable<long> HeartBeatEventBroker { get; }
    }
}
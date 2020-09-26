using System;
using System.Reactive.Subjects;

namespace Lumpy.Lib.Common.Broker
{
    public class ReplyDataEventBroker<T> : IObservable<T>//,IDisposable
    {
        private readonly ReplaySubject<T> _subscriptions;

        public ReplyDataEventBroker()
        {
            _subscriptions = new ReplaySubject<T>();
        }

        public IDisposable Subscribe(IObserver<T> observer) => _subscriptions.Subscribe(observer);
        public virtual void Publish(T dataEvent) => _subscriptions.OnNext(dataEvent);
        public void Close() => _subscriptions.OnCompleted();
    }
}

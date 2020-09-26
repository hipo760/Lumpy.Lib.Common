using System;
using System.Reactive.Subjects;

namespace Lumpy.Lib.Common.Broker
{
    public class DataEventBroker<T> : IObservable<T>
    {
        private readonly Subject<T> _subscriptions;

        public DataEventBroker()
        {
            _subscriptions = new Subject<T>();
        }

        public IDisposable Subscribe(IObserver<T> observer) => _subscriptions.Subscribe(observer);
        public virtual void Publish(T dataEvent) => _subscriptions.OnNext(dataEvent);
        public void Close() => _subscriptions.OnCompleted();
    }
}

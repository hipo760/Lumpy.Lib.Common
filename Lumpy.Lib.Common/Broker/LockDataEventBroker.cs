using System;
using System.Reactive.Subjects;

namespace Lumpy.Lib.Common.Broker
{
    public class LockDataEventBroker<T> : IObservable<T>//,IDisposable
    {
        private readonly Subject<T> _subscriptions;

        private readonly object _balanceLock = new object();

        public LockDataEventBroker()
        {
            _subscriptions = new Subject<T>();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (_balanceLock)
            {
                return _subscriptions.Subscribe(observer);
            }
        }

        public virtual void Publish(T dataEvent)
        {
            lock (_balanceLock)
            {
                _subscriptions.OnNext(dataEvent);
            }
        }

        public void Close()
        {
            lock (_balanceLock)
            {
                _subscriptions.OnCompleted();
            }
        }
    }
}

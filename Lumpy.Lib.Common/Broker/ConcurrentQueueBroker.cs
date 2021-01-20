using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Mc.StrategyPosition.Service.Broker
{
    public class ConcurrentQueueBroker<T>:IDisposable
    {
        private IDisposable _dequeueSub;
        private IDisposable _inputSub;
        private readonly ConcurrentQueue<T> _eventQueue;
        private readonly Subject<T> _dataEvent;
        private readonly TimeSpan _dequeueInterval;

        public IObservable<T> DataEvent => _dataEvent;

        public ConcurrentQueueBroker(TimeSpan dequeueInterval)
        {
            _dequeueInterval = dequeueInterval;
            _dataEvent = new Subject<T>();
            _eventQueue = new ConcurrentQueue<T>();
        }
        public void ListenEvent(IObservable<T> inputSource)
        {
            _dequeueSub = Observable
                .Interval(_dequeueInterval)
                .Subscribe(l => OnDequeueEvent());
            _inputSub = inputSource
                .Subscribe(OnNewEvent);
        }
        private void OnDequeueEvent()
        {
            if (!_eventQueue.TryDequeue(out var pos)) return;
            _dataEvent.OnNext(pos);
        }
        private void OnNewEvent(T newEvent)
        {
            _eventQueue.Enqueue(newEvent);
        }
        public void Dispose()
        {
            _dataEvent.OnCompleted();
            _dequeueSub?.Dispose();
            _inputSub?.Dispose();
            _eventQueue.Clear();
        }
    }
}
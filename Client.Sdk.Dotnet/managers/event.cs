using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.managers
{
    public delegate Task CancelListenFunc();

    // 基础可释放接口
    public interface IDisposable<T>
    {
        bool IsDisposed { get; }
        string ObjectId { get; }
        void OnDispose(Func<Task> action);
        Task DisposeAsync();
    }

    // 可释放基类实现
    public abstract class Disposable : IDisposable<object>
    {
        private bool _isDisposed = false;
        private readonly List<Func<Task>> _disposeActions = new List<Func<Task>>();

        public bool IsDisposed => _isDisposed;
        public string ObjectId => GetType().Name + "@" + GetHashCode();

        protected Disposable()
        {
        }

        public void OnDispose(Func<Task> action)
        {
            if (_isDisposed)
            {
                Debug.WriteLine($"Cannot add dispose action to already disposed object: {ObjectId}");
                return;
            }
            _disposeActions.Add(action);
        }

        public async Task DisposeAsync()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            foreach (var action in _disposeActions)
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during dispose action for {ObjectId}: {ex.Message}");
                }
            }
        }
    }

    // 事件可发射接口
    public interface IEventsEmittable<T>
    {
        EventsEmitter<T> Events { get; }
        EventsListener<T> CreateListener(bool synchronized = false);
    }

    // 混合实现 - C# 中使用基类
    public abstract class EventsEmittableBase<T> : IEventsEmittable<T>
    {
        private readonly EventsEmitter<T> _events = new EventsEmitter<T>();

        public EventsEmitter<T> Events => _events;

        public EventsListener<T> CreateListener(bool synchronized = false)
        {
            return new EventsListener<T>(_events, synchronized);
        }
    }

    // 事件可监听基类
    public abstract class EventsListenable<T> : Disposable
    {
        private readonly List<IDisposable> _listeners = new List<IDisposable>();
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        public bool Synchronized { get; }
        public abstract EventsEmitter<T> Emitter { get; }

        public IReadOnlyList<IDisposable> Listeners => _listeners.AsReadOnly();

        protected EventsListenable(bool synchronized) : base()
        {
            Synchronized = synchronized;
            OnDispose(async () => await CancelAll());
        }

        public async Task CancelAll()
        {
            if (_listeners.Any())
            {
                // 停止监听所有事件
                foreach (var listener in _listeners.ToList())
                {
                    if (listener is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();
                    else
                        listener.Dispose();

                    _listeners.Remove(listener);
                }
            }
        }

        // 监听所有事件，保证在释放时取消
        public CancelListenFunc Listen(Func<T, Task> onEvent)
        {
            Func<T, Task> func = onEvent;

            if (Synchronized)
            {
                // 确保 onEvent 按顺序触发（等待前一个 onEvent 完成）
                func = async (evt) =>
                {
                    await _syncLock.WaitAsync();
                    try
                    {
                        await onEvent(evt);
                    }
                    finally
                    {
                        _syncLock.Release();
                    }
                };
            }

            var subscription = Emitter.Subscribe(func);
            _listeners.Add(subscription);

            // 创建取消函数以在一次调用中取消监听并从列表中删除
            async Task CancelFunc()
            {
                if (subscription is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else
                    subscription.Dispose();

                _listeners.Remove(subscription);
            }

            return CancelFunc;
        }

        // 方便的方法来监听和过滤特定事件类型
        public CancelListenFunc On<E>(Func<E, Task> then, Func<E, bool> filter = null)
        {
            return Listen(async (evt) =>
            {
                // 事件必须是 E
                if (!(evt is E eEvt)) return;

                // 如果使用了过滤器，过滤器必须为 true
                if (filter != null && !filter(eEvt)) return;

                // 转换为 E
                await then(eEvt);
            });
        }

        // 方便的方法来监听和过滤特定事件类型，只一次
        public CancelListenFunc Once<E>(Func<E, Task> then, Func<E, bool> filter = null)
        {
            CancelListenFunc cancelFunc = null;
            cancelFunc = Listen(async (evt) =>
            {
                // 事件必须是 E
                if (!(evt is E eEvt)) return;

                // 如果使用了过滤器，过滤器必须为 true
                if (filter != null && !filter(eEvt)) return;

                // 转换为 E
                await then(eEvt);

                // 一次事件后取消
                if (cancelFunc != null)
                    await cancelFunc();
            });

            return cancelFunc;
        }

        // 等待特定事件类型
        public async Task<E> WaitFor<E>(
            TimeSpan timeout,
            Func<E, bool> filter = null,
            Func<Task<E>> onTimeout = null)
        {
            var taskCompletionSource = new TaskCompletionSource<E>();

            var cancelFunc = On<E>(
                async (evt) =>
                {
                    if (!taskCompletionSource.Task.IsCompleted)
                    {
                        taskCompletionSource.SetResult(evt);
                    }
                },
                filter);

            try
            {
                var timeoutTask = Task.Delay(timeout);
                var resultTask = taskCompletionSource.Task;

                var completedTask = await Task.WhenAny(resultTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    if (onTimeout != null)
                        return await onTimeout();

                    throw new TimeoutException("等待事件超时");
                }

                return await resultTask;
            }
            finally
            {
                // 始终清理监听器
                await cancelFunc();
            }
        }
    }

    // 事件发射器
    public class EventsEmitter<T> : EventsListenable<T>
    {
        private readonly Subject<T> _subject = new Subject<T>();
        private bool _queueMode = false;
        private readonly Queue<T> _queue = new Queue<T>();

        public EventsEmitter(bool listenSynchronized = false) : base(listenSynchronized)
        {
            OnDispose(async () => _subject.Dispose());
        }

        public IDisposable Subscribe(Func<T, Task> onEvent)
        {
            // 创建一个包装 Func<T, Task> 的 Observer 适配器
            var observer = new AsyncFuncObserver<T>(onEvent);
            return _subject.Subscribe(observer);
        }

        public override EventsEmitter<T> Emitter => this;

        public void Emit(T evt)
        {
            // 检查是否已释放
            if (IsDisposed)
            {
                // 记录日志：无法在已释放的发射器上发出事件
                return;
            }

            // 队列模式
            if (_queueMode)
            {
                _queue.Enqueue(evt);
                return;
            }

            // 发出事件
            _subject.OnNext(evt);
        }

        public void UpdateQueueMode(bool newValue, bool shouldEmitQueued = true)
        {
            // 检查是否已释放
            if (IsDisposed)
            {
                // 记录日志：无法更新已释放的发射器上的队列模式
                return;
            }

            if (_queueMode == newValue) return;

            _queueMode = newValue;
            if (!_queueMode && shouldEmitQueued) EmitQueued();
        }

        public void EmitQueued()
        {
            while (_queue.Count > 0)
            {
                var evt = _queue.Dequeue();
                // 发出事件
                _subject.OnNext(evt);
            }
        }

        // Subject<T> 实现
        private class Subject<TEvent> : IObservable<TEvent>, IDisposable
        {
            private readonly List<IObserver<TEvent>> _observers = new List<IObserver<TEvent>>();
            private bool _isDisposed = false;

            public IDisposable Subscribe(IObserver<TEvent> observer)
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(Subject<TEvent>));

                if (!_observers.Contains(observer))
                    _observers.Add(observer);

                return new Subscription(this, observer);
            }

            public void OnNext(TEvent value)
            {
                if (_isDisposed) return;

                foreach (var observer in _observers.ToArray())
                {
                    try
                    {
                        observer.OnNext(value);
                    }
                    catch
                    {
                        // 忽略观察者错误
                    }
                }
            }

            public void Dispose()
            {
                _isDisposed = true;
                _observers.Clear();
            }

            private class Subscription : IDisposable
            {
                private readonly Subject<TEvent> _subject;
                private readonly IObserver<TEvent> _observer;

                public Subscription(Subject<TEvent> subject, IObserver<TEvent> observer)
                {
                    _subject = subject;
                    _observer = observer;
                }

                public void Dispose()
                {
                    if (_subject._observers.Contains(_observer))
                        _subject._observers.Remove(_observer);
                }
            }
        }

        // 异步函数观察者适配器
        private class AsyncFuncObserver<TEvent> : IObserver<TEvent>
        {
            private readonly Func<TEvent, Task> _onNextAsync;

            public AsyncFuncObserver(Func<TEvent, Task> onNextAsync)
            {
                _onNextAsync = onNextAsync;
            }

            public void OnCompleted()
            {
                // 不做任何操作
            }

            public void OnError(Exception error)
            {
                // 不做任何操作
            }

            public void OnNext(TEvent value)
            {
                // 启动任务但不等待它
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _onNextAsync(value);
                    }
                    catch (Exception)
                    {
                        // 处理异常或记录日志
                    }
                });
            }
        }
    }

    // 事件监听器
    public class EventsListener<T> : EventsListenable<T>
    {
        private readonly EventsEmitter<T> _emitter;

        public override EventsEmitter<T> Emitter => _emitter;

        public EventsListener(EventsEmitter<T> emitter, bool synchronized = false)
            : base(synchronized)
        {
            _emitter = emitter;
        }
    }


}

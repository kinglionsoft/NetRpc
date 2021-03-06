﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace NetRpc
{
    public class ClientProxy<TService> : IDisposable
    {
        private bool _disposed;
        protected readonly object LockDispose = new object();
        public event EventHandler Connected;
        public event EventHandler DisConnected;
        public event EventHandler<EventArgsT<Exception>> ExceptionInvoked;
        private readonly object LockObj = new object();
        private readonly IConnectionFactory _factory;
        private readonly int _hearbeatInterval;
        private bool _isConnected;
        public event Func<ClientProxy<TService>, Task> Heartbeat;
        private readonly Timer _tHearbeat;

        public NetRpcContext Context { get; } = new NetRpcContext();

        public TService Proxy { get; }

        public ClientProxy(IConnectionFactory factory, bool isWrapFaultException, int timeoutInterval, int hearbeatInterval = 1000 * 10)
        {
            _factory = factory;
            _hearbeatInterval = hearbeatInterval;
            var call = new Call(factory, isWrapFaultException, timeoutInterval, Context);
            ClientMethodInvoker invoker = new ClientMethodInvoker(call);
            Proxy = SimpleDispatchProxyAsync.Create<TService>(invoker);
            ((SimpleDispatchProxyAsync)(object)Proxy).ExceptionInvoked += ProxyExceptionInvoked;
            _tHearbeat = new Timer(hearbeatInterval);
            _tHearbeat.Elapsed += THearbeatElapsed;
        }

        private void ProxyExceptionInvoked(object sender, EventArgsT<Exception> e)
        {
            OnExceptionInvoked(e);
        }

        private void THearbeatElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DoHeartbeat();
        }

        public bool IsConnected
        {
            get
            {
                lock (LockObj)
                    return _isConnected;
            }
            protected set
            {
                lock (LockObj)
                {
                    if (_isConnected == value)
                        return;
                    _isConnected = value;
                }

                if (value)
                    OnConnected();
                else
                    OnDisConnected();
            }
        }

        public void StartHeartbeat(bool isImmediate = false)
        {
            _tHearbeat.Start();
            if (isImmediate)
#pragma warning disable 4014
                HeartbeatAsync();
#pragma warning restore 4014
        }

        public async Task HeartbeatAsync()
        {
            await OnHeartbeat();
            IsConnected = true;
        }

        private async void DoHeartbeat()
        {
            try
            {
                await OnHeartbeat();
                IsConnected = true;
            }
            catch
            {
            }
        }

        private void OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected void OnDisConnected()
        {
            DisConnected?.Invoke(this, EventArgs.Empty);
        }

        private Task OnHeartbeat()
        {
            var func = Heartbeat;
            if (func != null)
                return func(this);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ClientProxy()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (LockDispose)
            {
                if (_disposed)
                    return;

                if (disposing)
                    DisposeManaged();

                _disposed = true;
            }
        }

        private void DisposeManaged()
        {
            _tHearbeat?.Dispose();
            _factory.Dispose();
        }

        private void OnExceptionInvoked(EventArgsT<Exception> e)
        {
            ExceptionInvoked?.Invoke(this, e);
        }
    }
}
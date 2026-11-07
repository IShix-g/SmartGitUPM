
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace SmartGitUPM.Editor
{
    internal sealed class EditorAsync : IDisposable
    {
        public bool IsStarted => _completionSource != default;

        bool _isDisposed;
        Func<bool> _isOperationComplete;
        TaskCompletionSource<bool> _completionSource;

        public async Task StartAsync(Func<bool> isOperationComplete, CancellationToken cancellationToken = default)
        {
            if (IsStarted)
            {
                throw new InvalidOperationException("Task is already started.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (isOperationComplete())
            {
                return;
            }

            _isOperationComplete = isOperationComplete;
            var completionSource = new TaskCompletionSource<bool>();
            _completionSource = completionSource;
            // Cancellation can arrive on a worker thread. Only complete the Task there;
            // detach from EditorApplication on the captured Editor context in finally.
            using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled());

            EditorApplication.update += OnUpdate;

            try
            {
                await completionSource.Task;
            }
            finally
            {
                Cleanup();
            }
        }

        public void Reset()
        {
            if (IsStarted)
            {
                Cancel();
            }
        }

        public void Cancel(Exception exception = default)
        {
            if (!IsStarted)
            {
                return;
            }

            if (exception == default)
            {
                _completionSource.TrySetCanceled();
            }
            else
            {
                _completionSource.TrySetException(exception);
            }
        }

        void OnUpdate()
        {
            try
            {
                if (_isOperationComplete())
                {
                    _completionSource.TrySetResult(true);
                }
            }
            catch (Exception exception)
            {
                _completionSource.TrySetException(exception);
            }
        }

        void Cleanup()
        {
            _completionSource = default;
            _isOperationComplete = default;
            EditorApplication.update -= OnUpdate;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            if (IsStarted)
            {
                Cancel();
            }
        }
    }
}

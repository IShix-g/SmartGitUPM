
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
        CancellationTokenSource _tokenSource;

        public async Task StartAsync(Func<bool> isOperationComplete, CancellationToken cancellationToken = default)
        {
            if (IsStarted)
            {
                throw new InvalidOperationException("Task is already started.");
            }

            if (isOperationComplete())
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            _isOperationComplete = isOperationComplete;
            _completionSource = new TaskCompletionSource<bool>();
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _tokenSource.Token.Register(() =>
                Cancel(new OperationCanceledException("Operation was cancelled by token")));

            EditorApplication.update += OnUpdate;

            try
            {
                await _completionSource.Task;
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
                Cleanup();
            }
        }
        
        public void Cancel(Exception exception = default)
        {
            if (!IsStarted)
            {
                return;
            }

            _completionSource.TrySetException(
                exception ?? new OperationCanceledException("Operation was cancelled"));
            Cleanup();
        }

        void OnUpdate()
        {
            if (_isOperationComplete())
            {
                _completionSource.SetResult(true);
            }
        }

        void Cleanup()
        {
            _completionSource = default;
            if (_tokenSource != default)
            {
                _tokenSource?.Dispose();
                _tokenSource = default;
            }
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
                if (_tokenSource != default)
                {
                    _tokenSource?.Dispose();
                    _tokenSource = default;
                }
                Cleanup();
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    internal static class TaskExtensions
    {
        public static void ContinueOnMainThread<T>(
            this Task<T> @this,
            Action<Task<T>> onSuccess,
            Action<Exception> onError = default,
            Action onCancel = default,
            Action onCompleted = default,
            CancellationToken cancellationToken = default)
        {
            var context = SynchronizationContext.Current;
            @this.ContinueWith(t =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested || t.IsCanceled)
                    {
                        if (onCancel != default)
                        {
                            if (SynchronizationContext.Current == context)
                            {
                                onCancel.Invoke();
                            }
                            else
                            {
                                context.Post(_ => onCancel.Invoke(), default);
                            }
                        }

                        return;
                    }

                    if (t.IsFaulted)
                    {
                        if (t.Exception != default && onError != default)
                        {
                            if (SynchronizationContext.Current == context)
                            {
                                onError.Invoke(t.Exception);
                            }
                            else
                            {
                                context.Post(_ => onError.Invoke(t.Exception), default);
                            }
                        }

                        return;
                    }

                    if (t.IsCompleted)
                    {
                        if (SynchronizationContext.Current == context)
                        {
                            onSuccess(t);
                        }
                        else
                        {
                            context.Post(_ => onSuccess(t), default);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (onError != default)
                    {
                        if (SynchronizationContext.Current == context)
                        {
                            onError.Invoke(ex);
                        }
                        else
                        {
                            context.Post(_ => onError.Invoke(ex), default);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Unhandled exception in ContinueOnMainThread: {ex}");
                    }
                }
                finally
                {
                    if (onCompleted != default)
                    {
                        if (SynchronizationContext.Current == context)
                        {
                            onCompleted.Invoke();
                        }
                        else
                        {
                            context.Post(_ => onCompleted.Invoke(), default);
                        }
                    }
                }
            },
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Current);
        }

        public static void ContinueOnMainThread(
            this Task @this,
            Action<Task> onSuccess,
            Action<Exception> onError = default,
            Action onCancel = default,
            Action onCompleted = default,
            CancellationToken cancellationToken = default)
        {
            var context = SynchronizationContext.Current;
            @this.ContinueWith(t =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested || t.IsCanceled)
                        {
                            if (onCancel != default)
                            {
                                if (SynchronizationContext.Current == context)
                                {
                                    onCancel.Invoke();
                                }
                                else
                                {
                                    context.Post(_ => onCancel.Invoke(), default);
                                }
                            }
                            return;
                        }

                        if (t.IsFaulted)
                        {
                            if (t.Exception != default && onError != default)
                            {
                                if (SynchronizationContext.Current == context)
                                {
                                    onError.Invoke(t.Exception);
                                }
                                else
                                {
                                    context.Post(_ => onError.Invoke(t.Exception), default);
                                }
                            }
                            return;
                        }

                        if (t.IsCompleted)
                        {
                            if (SynchronizationContext.Current == context)
                            {
                                onSuccess(t);
                            }
                            else
                            {
                                context.Post(_ => onSuccess(t), default);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (onError != default)
                        {
                            if (SynchronizationContext.Current == context)
                            {
                                onError.Invoke(ex);
                            }
                            else
                            {
                                context.Post(_ => onError.Invoke(ex), default);
                            }
                        }
                        else
                        {
                            Debug.LogError($"Unhandled exception in ContinueOnMainThread: {ex}");
                        }
                    }
                    finally
                    {
                        if (onCompleted != default)
                        {
                            if (SynchronizationContext.Current == context)
                            {
                                onCompleted.Invoke();
                            }
                            else
                            {
                                context.Post(_ => onCompleted.Invoke(), default);
                            }
                        }
                    }
                },
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Current);
        }

        public static void Handled(this Task task, Action<Task> onCompleted = default, Action<Exception> onError = default)
            => HandleTaskAsync(task, onCompleted, onError, SynchronizationContext.Current);

        static async void HandleTaskAsync(Task task, Action<Task> onCompleted, Action<Exception> onError, SynchronizationContext context)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException ex)
            {
                Debug.LogWarning("Task was canceled. Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                if (onError != default)
                {
                    if (SynchronizationContext.Current == context)
                    {
                        onError(ex);
                    }
                    else
                    {
                        context?.Post(_ => onError(ex), default);
                    }
                }
                else
                {
                    if (SynchronizationContext.Current == context)
                    {
                        Debug.LogError(ex);
                    }
                    else
                    {
                        context?.Post(_ => Debug.LogError(ex), default);
                    }
                }
            }
            finally
            {
                if (onCompleted != default)
                {
                    if (SynchronizationContext.Current == context)
                    {
                        onCompleted(task);
                    }
                    else
                    {
                        context?.Post(_ => onCompleted(task), default);
                    }
                }
            }
        }

        public static void Handled<T>(this Task<T> task, Action<Task<T>> onCompleted = default, Action<Exception> onError = default)
            => HandleTaskAsync<T>(task, onCompleted, onError, SynchronizationContext.Current);

        static async void HandleTaskAsync<T>(Task<T> task, Action<Task<T>> onCompleted, Action<Exception> onError, SynchronizationContext context)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException ex)
            {
                Debug.LogWarning("Task was canceled. Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                if (onError != default)
                {
                    if (SynchronizationContext.Current == context)
                    {
                        onError(ex);
                    }
                    else
                    {
                        context?.Post(_ => onError(ex), default);
                    }
                }
                else
                {
                    if (SynchronizationContext.Current == context)
                    {
                        Debug.LogError(ex);
                    }
                    else
                    {
                        context?.Post(_ => Debug.LogError(ex), default);
                    }
                }
            }
            finally
            {
                if (onCompleted != default)
                {
                    if (SynchronizationContext.Current == context)
                    {
                        onCompleted(task);
                    }
                    else
                    {
                        context?.Post(_ => onCompleted(task), default);
                    }
                }
            }
        }

        public static void SafeCancelAndDispose(this CancellationTokenSource @this)
        {
            if (@this == default)
            {
                return;
            }

            try
            {
                if (!@this.IsCancellationRequested)
                {
                    @this.Cancel();
                }
                @this.Dispose();
            }
            catch
            {
                // Ignore
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading;

namespace Hangfire.Storage.SQLite
{

    // Daniel Lindblom WAS HERE:
    // Added this utility as a light alternative to use Polly since I assume you do not want to drag
    // that dependency in to this library.

    public static class Retrying
    {
        public const int Once = 1;
        public const int Twice = 2;
    }

    public static class Retry
    {
        public static TimeSpan DefaultDelay { get; set; } = TimeSpan.FromMilliseconds(250);

        public static void Execute(Action<int> action, CancellationToken token, params TimeSpan[] delays)
        {
            if (delays is null || delays.Length == 0)
                delays = new TimeSpan[] { DefaultDelay };

            var retries = delays.Length;
            var tries = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    action(tries + 1);
                    return;
                }
                catch
                {
                    var delay = delays[tries % delays.Length];
                    if (++tries > retries) throw;
                    token.WaitHandle.WaitOne(delay);
                }
            }
            throw new OperationCanceledException();
        }
        public static void Execute(Action<int> action, params TimeSpan[] delays)
        {
            Execute(action, CancellationToken.None, delays);
        }

        public static void Execute(Action<int> action, int retries = Retrying.Once, TimeSpan? delay = null)
        {
            delay = delay ?? DefaultDelay;
            var delays = Enumerable.Range(1, retries).Select(_ => delay.Value).ToArray();
            Execute(action, delays);
        }

        public static void Execute(Action<int> action, CancellationToken token, int retries = Retrying.Once, TimeSpan? delay = null)
        {
            delay = delay ?? DefaultDelay;
            var delays = Enumerable.Range(1, retries).Select(_ => delay.Value).ToArray();
            Execute(action, token, delays);
        }

        public static TResult Execute<TResult>(Func<int, TResult> func, CancellationToken token, params TimeSpan[] delays)
        {
            if (delays is null || delays.Length == 0)
                delays = new TimeSpan[] { DefaultDelay };

            var retries = delays.Length;
            var tries = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    return func(tries + 1);
                }
                catch
                {
                    var delay = delays[tries % delays.Length];
                    if (++tries > retries) throw;
                    token.WaitHandle.WaitOne(delay);
                }
            }
            throw new OperationCanceledException();
        }

        public static TResult Execute<TResult>(Func<int, TResult> func, params TimeSpan[] delays)
        {
            return Execute(func, CancellationToken.None, delays);
        }

        public static TResult Execute<TResult>(Func<int, TResult> func, int retries = Retrying.Once, TimeSpan? delay = null)
        {
            delay = delay ?? DefaultDelay;
            var delays = Enumerable.Range(1, retries).Select(_ => delay.Value).ToArray();
            return Execute(func, delays);
        }

        public static TResult Execute<TResult>(Func<int, TResult> func, CancellationToken token, int retries = Retrying.Once, TimeSpan? delay = null)
        {
            delay = delay ?? DefaultDelay;
            var delays = Enumerable.Range(1, retries).Select(_ => delay.Value).ToArray();
            return Execute(func, token, delays);
        }

        public static void Once(Action<int> action, CancellationToken? token = null, TimeSpan? delay = null)
        {
            token = token ?? CancellationToken.None;
            delay = delay ?? DefaultDelay;
            Execute(action, token.Value, Retrying.Once, delay.Value);
        }
        public static void Twice(Action<int> action, CancellationToken? token = null, TimeSpan? delay = null)
        {
            token = token ?? CancellationToken.None;
            delay = delay ?? DefaultDelay;
            Execute(action, token.Value, Retrying.Twice, delay.Value);
        }
        public static TResult Once<TResult>(Func<int, TResult> func, CancellationToken? token = null, TimeSpan? delay = null)
        {
            token = token ?? CancellationToken.None;
            delay = delay ?? DefaultDelay;
            return Execute(func, token.Value, Retrying.Once, delay.Value);
        }
        public static TResult Twice<TResult>(Func<int, TResult> func, CancellationToken? token = null, TimeSpan? delay = null)
        {
            token = token ?? CancellationToken.None;
            delay = delay ?? DefaultDelay;
            return Execute(func, token.Value, Retrying.Twice, delay.Value);
        }
    }
}
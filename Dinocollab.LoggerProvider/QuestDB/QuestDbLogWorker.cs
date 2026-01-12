using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestDB;
using QuestDB.Senders;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Dinocollab.LoggerProvider.QuestDB
{
    public sealed class QuestDbLogWorker<T> : BackgroundService
    {
        private readonly Channel<T> _channel;
        private readonly QuestDBLoggerOption _options;
        private readonly ILogger? _logger;

        private ISender? _sender;
        private readonly object _senderLock = new();

        private readonly string _tableName;
        private volatile bool _tableReady = false;
        public QuestDbLogWorker(
            ILogger<QuestDbLogWorker<T>> logger,
            IOptions<QuestDBLoggerOption> optionsAccessor)
        {
            _options = optionsAccessor.Value;
            _logger = logger;

            _tableName = _options.TableLogName ?? typeof(T).GetTableName();

            _channel = Channel.CreateBounded<T>(
                new BoundedChannelOptions(_options.CapacityQueue)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });
        }

        /* -----------------------------
         *  Public API
         * -----------------------------*/

        public bool TryEnqueue(T log)
            => _channel.Writer.TryWrite(log);

     
        // Create table + TTL via HTTP API
        public async Task InitialAsync(CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromSeconds(1);
            var maxDelay = TimeSpan.FromMinutes(1);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _options.ApiUrl.CreateTableAsync<T>(_options.TableLogName);
                    await _options.ApiUrl.AlterTTLAsync<T>(_options.TTLDAYS, _options.TableLogName);

                    _tableReady = true;

                    _logger?.LogInformation(
                        "QuestDB table {Table} is ready",
                        _tableName);

                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(
                        ex,
                        "QuestDB table not ready, retrying in {Delay}",
                        delay);

                    await Task.Delay(delay, cancellationToken);

                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(delay.TotalMilliseconds * 2, maxDelay.TotalMilliseconds)
                    );
                }
            }
        }
           /* -----------------------------
          *  BackgroundService lifecycle
          * -----------------------------*/
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var delay = TimeSpan.FromSeconds(1);
            var maxDelay = TimeSpan.FromMinutes(1);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunAsync(stoppingToken);

                    delay = TimeSpan.FromSeconds(1);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "QuestDbLogWorker crashed, retrying in {Delay}",
                        delay);

                    await Task.Delay(delay, stoppingToken);

                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(delay.TotalMilliseconds * 2, maxDelay.TotalMilliseconds)
                    );
                }
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Run(() => InitialAsync(cancellationToken), cancellationToken);
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await TryFlushAsync(cancellationToken);
            }
            catch { /* swallow */ }

            DisposeSender();
            await base.StopAsync(cancellationToken);
        }

        /* -----------------------------
         *  Core loop
         * -----------------------------*/

        private async Task RunAsync(CancellationToken ct)
        {
            while (!_tableReady)
            {
                await Task.Delay(500, ct);
            }
            var counter = 0;
            var lastFlush = DateTime.UtcNow;

            await foreach (var log in _channel.Reader.ReadAllAsync(ct))
            {
                var sender = EnsureSender();

                await sender
                    .Table(_tableName)
                    .ToConvert(log)
                    .AtAsync(DateTime.UtcNow);

                counter++;

                if (counter >= _options.BatchSize ||
                    (DateTime.UtcNow - lastFlush).TotalMilliseconds >= _options.FlushIntervalMs)
                {
                    await TryFlushAsync(ct);
                    counter = 0;
                    lastFlush = DateTime.UtcNow;
                }
            }

            if (counter > 0)
                await TryFlushAsync(ct);
        }

        /* -----------------------------
         *  Sender management
         * -----------------------------*/

        private ISender EnsureSender()
        {
            if (_sender != null)
                return _sender;

            lock (_senderLock)
            {
                if (_sender == null)
                {
                    _logger?.LogInformation("Creating QuestDB sender");
                    _sender = Sender.New(_options.ConnectionString);
                }
                return _sender;
            }
        }

        private void DisposeSender()
        {
            lock (_senderLock)
            {
                try
                {
                    _sender?.Dispose();
                }
                catch { /* ignore */ }
                finally
                {
                    _sender = null;
                }
            }
        }

        /* -----------------------------
         *  Flush with real retry
         * -----------------------------*/

        private async Task TryFlushAsync(CancellationToken ct)
        {
            const int MaxRetry = 5;
            var attempt = 0;

            while (true)
            {
                try
                {
                    var sender = EnsureSender();
                    await sender.SendAsync(ct);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    attempt++;

                    _logger?.LogWarning(
                        ex,
                        "QuestDB send failed (attempt {Attempt}), recreating sender",
                        attempt);

                    DisposeSender();

                    if (attempt >= MaxRetry)
                    {
                        throw new InvalidOperationException(
                            $"QuestDB flush failed after {attempt} attempts", ex);
                    }

                    await Task.Delay(500 * attempt, ct);
                }
            }
        }
    }
}

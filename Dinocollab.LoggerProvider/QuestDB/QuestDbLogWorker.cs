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
        private readonly ISender _sender;

        private readonly string _tableName = "logs";
        private readonly ILogger? _logger;
        private readonly QuestDBLoggerOption _options;
        public QuestDbLogWorker(
            ILogger<QuestDbLogWorker<T>> logger,
            IOptions<QuestDBLoggerOption> optionsAccessor
            )
        {
            _options = optionsAccessor.Value;
            _channel = Channel.CreateBounded<T>(
                new BoundedChannelOptions(_options.CapacityQueue)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });

            _sender = Sender.New(_options.ConnectionString);
            _tableName = _options.TableLogName ?? typeof(T).GetTableName();
            _logger = logger;
        }

        public bool TryEnqueue(T log)
            => _channel.Writer.TryWrite(log);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "An error occurred in QuestDbLogWorker.");
                    try { await TryFlushAsync(stoppingToken); } catch { /* ignore */ }
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        public async Task RunAsync(CancellationToken stoppingToken = default)
        {
            var counter = 0;
            var lastFlush = DateTime.UtcNow;

            await foreach (var log in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await _sender
                   .Table(_tableName)
                   .ToConvert(log)
                   .AtAsync(DateTime.UtcNow);

                counter++;

                if (counter >= _options.BatchSize ||
                    (DateTime.UtcNow - lastFlush).TotalMilliseconds >= _options.FlushIntervalMs)
                {
                    await TryFlushAsync(stoppingToken); // Change to call FlushAsync() instead of _sender.Send()
                    counter = 0;
                    lastFlush = DateTime.UtcNow;
                }
            }
            if (counter > 0)
                await TryFlushAsync(stoppingToken);
        }
        public async Task TryFlushAsync(CancellationToken ct = default)
        {
            const int MaxRetry = 5;
            const int InitialDelayMs = 100;

            var attempt = 0;
            var delay = InitialDelayMs;

            while (true)
            {
                try
                {
                    await _sender.SendAsync(ct);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    attempt++;

                    if (attempt >= MaxRetry)
                    {
                        throw new InvalidOperationException(
                            $"QuestDB flush failed after {attempt} attempts", ex);
                    }

                    await Task.Delay(Math.Min(delay, 5000), ct);
                    delay *= 2; 
                }
            }
        }

        public Task FlushAsync()
        {
            return _sender.SendAsync();
        }
        public void Flush()
        {
            _sender.Send();
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await _options.ApiUrl.CreateTableAsync<T>(_options.TableLogName);
            await _options.ApiUrl.AlterTTLAsync<T>(_options.TTLDAYS, _options.TableLogName);
            await base.StartAsync(cancellationToken);
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await TryFlushAsync();
            _sender.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}

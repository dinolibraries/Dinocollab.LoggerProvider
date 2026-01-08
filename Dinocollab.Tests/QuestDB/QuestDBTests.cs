using Dinocollab.LoggerProvider.Models;
using Dinocollab.LoggerProvider.QuestDB;
using Microsoft.Extensions.DependencyInjection;
using QuestDB;
using Serilog;
namespace Dinocollab.Tests.QuestDB
{
    public class QuestDBTests
    {
        /// <summary>
        /// CREATE TABLE logs (
        /// timestamp TIMESTAMP,
        /// level SYMBOL,
        /// message STRING
        /// ) timestamp(timestamp) PARTITION BY DAY;
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TestPushTest()
        {

            // 1. Khởi tạo sender kết nối HTTP tới QuestDB
            // port 9000 mặc định ILP HTTP
            var sender = Sender.New("tcp::addr=localhost:9009;");
            var now = DateTime.UtcNow;
            // 2. Đẩy 1 log đơn
            await sender.Table("logs")
                  .Symbol("level", "INFO")
                  .Column("message", "Unit test log")
                  .AtAsync(now);

            // 3. Gửi dữ liệu
            await sender.SendAsync();

            // Nếu không exception → insert thành công
            Assert.True(true);
        }
        [Fact]
        public async Task TestPushLogToQuestDB()
        {
      
            var services = new ServiceCollection();
            services.AddSingleton(typeof(QuestDbLogWorker<>));
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<QuestDbLogWorker<HttpContextMessageLog>>();
            using var cts = new CancellationTokenSource();
            await logger.StartAsync(cts.Token);
            logger.TryEnqueue(new HttpContextMessageLog
            {
                Level = "Information",
                Method = "GET",
                Host = "localhost",
                Path = "/api/test",
                StatusCode = 200,
                Assembly = "Dinocollab.Tests",
                UserAgent = "UnitTestAgent/1.0",
                TraceId = Guid.NewGuid().ToString(),
                IsAuthenticated = true,
                UserId = "user-123",
                UserName = "testuser",
                Query = "param1=value1&param2=value2",
                Referer = "http://example.com",
                RemoteIp = "192.168.1.1",
                RequestId = Guid.NewGuid().ToString(),
                RequestBody = "{\"key\":\"value\"}",
                ResponseBody = "{\"status\":\"success\"}",
                XForwardedFor = "192.168.1.1",
                Controller = "TestController",
                Action = "TestAction"
            });
            await Task.Delay(2000); // Wait for the log to be processed
            await logger.StopAsync(cts.Token);
        }
    }
}

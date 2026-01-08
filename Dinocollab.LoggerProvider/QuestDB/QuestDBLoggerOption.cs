using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dinocollab.LoggerProvider.QuestDB
{
    public class QuestDBLoggerOption
    {
        public required string ConnectionString { get; set; }
        public int BatchSize { get; set; } = 500;
        public int FlushIntervalMs { get; set; } = 1000;
        public int CapacityQueue { get; set; } = 10_000;
        public bool IsRequestBody { get; set; } = true;
        public bool IsResponseBody { get; set; } = false;
        public int TTLDAYS { get; set; } = 1;
        public required string ApiUrl { get; set; }
        public string? TableLogName { get; set; }
    }
}

using MqttService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentDetectionWorker.Models.Common
{
    public class GlobalConfig
    {
        public MqttConfig MqttConfig { get; set; }
        public RedisConfig RedisConfig { get; set; }
        public RedisChannels RedisChannels { get; set; }
        public Constants Constants { get; set; }
    }
    public class RedisConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int Database { get; set; }
        public string Password { get; set; }
        public string ConnectionName { get; set; }
        public int RetryCount { get; set; }
        public int RetryTimeout { get; set; }
        public int ConnectionTimeout { get; set; }
        public int KeepAlive { get; set; }
        public int ScanPageSize { get; set; }
        public Boolean AbortOnFail { get; set; }

    }

    public class RedisChannels
    {
        public string ActionChannel { get; set; }
        public string TopicsCountChannel { get; set; }
    }

    public class Constants
    {
        public string VectorJobExpression { get; set; }
        public string VectorJobUnit { get; set; } = "seconds";
        public string DevicesSegments { get; set; }
        public string DevicesCoordinates { get; set; }
        public string IntersectionIds { get; set; }
        public int RunJobEvery { get; set; }
        public int CollisionThreshold { get; set; }
        public float TimeSteps { get; set; }
        public int BatchSize { get; set; }
        public int MaxCollisionTime { get; set; }
    }

}

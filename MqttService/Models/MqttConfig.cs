using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttService.Models
{
    public class MqttConfig
    {
        public string Host { get; set; }
        public string PublicHostTest { get; set; }
        public string P2PChannel { get; set; }
        public int Port { get; set; }
        public bool RetryOnTimeout { get; set; }
        public bool UsePublicHost { get; set; }
        public bool SecureTls { get; set; }
        public int RetryAfter { get; set; }
        public int RetryCount { get; set; }
    }

}

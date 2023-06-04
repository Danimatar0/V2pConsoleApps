using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttService.Service
{
    public interface IMQTTService
    {
        public Task PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1);
    }
}

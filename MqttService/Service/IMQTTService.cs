using MQTTnet.Extensions.ManagedClient;
using MqttService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttService.Service
{
    public interface IMQTTService
    {
        public Task StartAsync(string brokerAddress, string clientId, string username, string password);
        public Task StopAsync();
        public Task UnsubscribeAsync(string topic);
        public Task PublishAsync(string topic, string payload, int qos = 0, bool retain = false);
    }
}

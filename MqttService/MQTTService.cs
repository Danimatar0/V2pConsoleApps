using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using NLog;

namespace MqttService
{
    public class MQTTService
    {
        private readonly IMqttNetLogger _mqttLogger;
        private readonly ILogger<MQTTService> _logger;
        public MQTTService(IMqttNetLogger mlogger, ILogger<MQTTService> logger)
        {
            _mqttLogger = mlogger;
            _logger = logger;
        }
        public async Task Connect_Client_Using_MQTTv5()
        {
            var mqttFactory = new MqttFactory();

            try
            {
                using (var mqttClient = mqttFactory.CreateMqttClient())
                {
                    var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").WithProtocolVersion(MqttProtocolVersion.V500).Build();

                    // In MQTTv5 the response contains much more information.
                    var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);


                    if (response.ResultCode == MqttClientConnectResultCode.Success)
                    {
                        Console.WriteLine("The MQTT client is connected.");
                    }
                    else
                    {
                        Console.WriteLine("Couldn't connect to MQTT client..");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to connect to MQTT: " + ex.Message);
            }
        }

        public async Task Disconnect_Clean()
        {
            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                // Calling _DisconnectAsync_ will send a DISCONNECT packet before closing the connection.
                // Using a reason code requires MQTT version 5.0.0!
                await mqttClient.DisconnectAsync(MqttClientDisconnectOptionsReason.ImplementationSpecificError);
            }
        }

        public async Task Ping_Server()
        {
            /*
             * This sample sends a PINGREQ packet to the server and waits for a reply.
             *
             * This is only supported in MQTTv5.0.0+.
             */

            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                // This will throw an exception if the server does not reply.
                await mqttClient.PingAsync(CancellationToken.None);

                Console.WriteLine("The MQTT server replied to the ping request.");
            }
        }

        public static void Reconnect_Using_Timer()
        {
            /*
             * This sample shows how to reconnect when the connection was dropped.
             * This approach uses a custom Task/Thread which will monitor the connection status.
             * This is the recommended way but requires more custom code!
             */

            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();

                _ = Task.Run(
                    async () =>
                    {
                        // User proper cancellation and no while(true).
                        while (true)
                        {
                            try
                            {
                                // This code will also do the very first connect! So no call to _ConnectAsync_ is required in the first place.
                                if (!await mqttClient.TryPingAsync())
                                {
                                    await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                                    // Subscribe to topics when session is clean etc.
                                    Console.WriteLine("The MQTT client is connected.");
                                }
                            }
                            catch
                            {
                                // Handle the exception properly (logging etc.).
                            }
                            finally
                            {
                                // Check the connection state every 5 seconds and perform a reconnect if required.
                                await Task.Delay(TimeSpan.FromSeconds(5));
                            }
                        }
                    });
            }
        }
    }
}
//using System.Net.NetworkInformation;
//using System.Runtime.Intrinsics.X86;
//using System;
//using System.Security.Authentication;
//using Microsoft.Extensions.Logging;
//using MQTTnet;
//using MQTTnet.Client;
//using MQTTnet.Diagnostics;
//using MQTTnet.Extensions.ManagedClient;
//using MQTTnet.Formatter;
//using MqttService.Models;
//using NLog;
//using static NLog.LayoutRenderers.Wrappers.ReplaceLayoutRendererWrapper;
//using NLog.Fluent;
//using MQTTnet.Server;
//using Microsoft.Extensions.Options;
//using System.Threading.Channels;
//using System.Net;
//using MQTTnet.Adapter;

//namespace MqttService.Service
//{
//    public class MQTTService : IMQTTService
//    {
//        private readonly ILogger<MQTTService> _logger;
//        private readonly MqttConfig _mqttConfig;
//        public MQTTService(ILogger<MQTTService> logger, IOptions<MqttConfig> options)
//        {
//            _logger = logger;
//            _mqttConfig = options.Value;
//        }

//        public void OnConnected(MqttClientConnectedEventArgs obj)
//        {
//            _logger.LogInformation("Successfully connected.");
//        }

//        public void OnConnectingFailed(ManagedProcessFailedEventArgs obj)
//        {
//            _logger.LogWarning("Couldn't connect to broker.");
//        }

//        public void OnDisconnected(MqttClientDisconnectedEventArgs obj)
//        {
//            _logger.LogInformation("Successfully disconnected.");
//        }

//        public async Task PingServer()
//        {
//            /*
//             * This sample sends a PINGREQ packet to the server and waits for a reply.
//             *
//             * This is only supported in MQTTv5.0.0+.
//             */

//            var mqttFactory = new MqttFactory();

//            using (var mqttClient = mqttFactory.CreateMqttClient())
//            {
//                var mqttClientOptions = new MqttClientOptionsBuilder()
//                                        .WithTcpServer(_mqttConfig.UsePublicHost ? _mqttConfig.PublicHostTest : _mqttConfig.Host)
//                                        .Build();

//                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

//                // This will throw an exception if the server does not reply.
//                await mqttClient.PingAsync(CancellationToken.None);

//                Console.WriteLine("The MQTT server replied to the ping request.");
//            }
//        }

//        public async Task ConnectAsync(MqttConfig config)
//        {
//            string clientId = "AccidentDetector";
//            string mqttURI = config.UsePublicHost ? config.PublicHostTest : config.Host;
//            int mqttPort = config.Port;

//            // Setup and start a managed MQTT client.
//            var options = new ManagedMqttClientOptionsBuilder()
//                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
//                .WithClientOptions(new MqttClientOptionsBuilder()
//                    .WithClientId(clientId)
//                    .WithTcpServer(mqttURI)
//                    .Build())
//                .Build();

//            var mqttClient = new MqttFactory().CreateManagedMqttClient();
//            try
//            {

//                Console.WriteLine("Creating new MQTT Client..");
//                _logger.LogInformation("Creating new MQTT Client..");


//                Console.WriteLine("Starting MQTT Client..");
//                _logger.LogInformation("Starting MQTT Client..");

//                await mqttClient.StartAsync(options);

//                if (mqttClient.IsStarted && mqttClient.IsConnected)
//                {
//                    Console.WriteLine("Successfully connected to MQTT Client..");
//                    _logger.LogInformation("Successfully connected to MQTT Client..");
//                }
//                else
//                {
//                    throw new Exception("Unable to start MQTT client or connect to the broker");
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"Unable to connect to MQTT broker: {ex.Message}");
//            }
//            finally
//            {
//                _logger.LogInformation("Disposing MQTT Client..");
//                await mqttClient.StopAsync(true);
//                mqttClient.Dispose();
//            }
//        }
//        public async Task PublishAsync(IManagedMqttClient mqttClient, string topic, string payload, bool retainFlag = true, int qos = 1)
//        {

//            //Set up message builder
//            var msgBuilder = new MqttApplicationMessageBuilder()
//                            .WithTopic(topic)
//                            .WithPayload(payload)
//                            //.WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
//                            .WithRetainFlag(retainFlag)
//                            .Build();
//            try
//            {
//                //Publish message
//                mqttClient.EnqueueAsync(msgBuilder);

//                Console.WriteLine($"Publishing message {payload} to topic {topic}..");
//                _logger.LogInformation($"Publishing message {payload} to topic {topic}..");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Unable to publish payload: " + ex.Message);
//                _logger.LogError("Unable to publish payload: " + ex.Message);
//            }
//        }
//    }
//}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MqttService.Service;

namespace MqttService.Service
{
    public class MQTTService : IMQTTService
    {
        private readonly IManagedMqttClient _client;
        private readonly ILogger<MQTTService> _logger;
        public MQTTService(ILogger<MQTTService> logger)
        {
            _logger = logger;

            // Create a new managed MQTT client
            _client = new MqttFactory().CreateManagedMqttClient();

            // Set the event handlers for connected, disconnected and message received events
            _client.ConnectedAsync += (args) =>
            {
                OnConnected(args);
                return Task.CompletedTask;
            };

            _client.DisconnectedAsync += (args) =>
            {
                OnDisconnected(args);
                return Task.CompletedTask;
            };

            _client.ApplicationMessageReceivedAsync += (args) =>
            {
                OnMessageReceived(args);
                return Task.CompletedTask;
            };
        }

        public async Task StartAsync(string brokerAddress, string clientId, string username, string password)
        {
            try
            {

                // Create a new client options object with the broker address, client id, username and password
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    //.WithCredentials(username, password)
                    .WithTcpServer(brokerAddress)
                    .Build();

                if (_client.IsStarted)
                    return;

                // Start the managed client with the options and an auto reconnect delay of 5 seconds
                await _client.StartAsync(new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(options)
                    .Build());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Console.WriteLine(ex.Message);
            }
        }

        public async Task StopAsync()
        {
            // Stop the managed client
            await _client.StopAsync();
        }

        public async Task UnsubscribeAsync(string topic)
        {
            // Unsubscribe from a topic
            await _client.UnsubscribeAsync(topic);
        }

        public async Task PublishAsync(string topic, string payload, int qos = 0, bool retain = false)
        {
            try
            {
                // Publish a message to a topic with a given quality of service level (0, 1 or 2) and a retain flag
                await _client.EnqueueAsync(new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .WithRetainFlag(retain)
                    .Build());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Console.WriteLine(ex.Message);
            }
        }

        private void OnConnected(MqttClientConnectedEventArgs e)
        {
            // Handle the connected event here
            Console.WriteLine("Connected to broker.");
        }

        private void OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            // Handle the disconnected event here
            Console.WriteLine("Disconnected from broker.");
        }

        private void OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            // Handle the message received event here
            Console.WriteLine($"Received message from {e.ClientId} on topic {e.ApplicationMessage.Topic} with payload {e.ApplicationMessage.ConvertPayloadToString()}");
        }
    }
}

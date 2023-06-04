using System.Net.NetworkInformation;
using System.Runtime.Intrinsics.X86;
using System;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MqttService.Models;
using NLog;
using static NLog.LayoutRenderers.Wrappers.ReplaceLayoutRendererWrapper;
using NLog.Fluent;
using MQTTnet.Server;
using Microsoft.Extensions.Options;

namespace MqttService.Service
{
    public class MQTTService : IMQTTService
    {
        private readonly IMqttNetLogger _mqttLogger;
        private readonly ILogger<MQTTService> _logger;
        private readonly MqttConfig _mqttConfig;
        public MQTTService(IMqttNetLogger mlogger, ILogger<MQTTService> logger, IOptions<MqttConfig> options)
        {
            _mqttLogger = mlogger;
            _logger = logger;
            _mqttConfig = options.Value;
        }

        public void OnConnected(MqttClientConnectedEventArgs obj)
        {
            _logger.LogInformation("Successfully connected.");
        }

        public void OnConnectingFailed(ManagedProcessFailedEventArgs obj)
        {
            _logger.LogWarning("Couldn't connect to broker.");
        }

        public void OnDisconnected(MqttClientDisconnectedEventArgs obj)
        {
            _logger.LogInformation("Successfully disconnected.");
        }

        public async Task PingServer()
        {
            /*
             * This sample sends a PINGREQ packet to the server and waits for a reply.
             *
             * This is only supported in MQTTv5.0.0+.
             */

            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder()
                                        .WithTcpServer(_mqttConfig.UsePublicHost ? _mqttConfig.PublicHostTest : _mqttConfig.Host)
                                        .Build();

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                // This will throw an exception if the server does not reply.
                await mqttClient.PingAsync(CancellationToken.None);

                Console.WriteLine("The MQTT server replied to the ping request.");
            }
        }

        public async Task PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1)
        {

            string clientId = "AccidentDetector";
            string mqttURI = _mqttConfig.UsePublicHost ? _mqttConfig.PublicHostTest : _mqttConfig.Host;
            int mqttPort = _mqttConfig.Port;

            // Creates a new client
            var messageBuilder = new MqttClientOptionsBuilder()
              .WithClientId(clientId)
              .WithTcpServer(mqttURI, mqttPort);

            // Create client options objects
            ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
                                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(_mqttConfig.RetryAfter))
                                    .WithClientOptions(messageBuilder.Build())
                                    .Build();

            // Creates the client object
            IManagedMqttClient _mqttClient = new MqttFactory().CreateManagedMqttClient();
            // Set up handlers


            //Set up message builder
            var msgBuilder = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(payload)
                            .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                            .WithRetainFlag(retainFlag)
                            .Build();
            try
            {
                _logger.LogInformation("Starting MQTT Client..");

                //Start mqtt client
                _mqttClient.StartAsync(options).GetAwaiter().GetResult();


                _logger.LogInformation("Publishing message..");
                //Publish message
                await _mqttClient.InternalClient.PublishAsync(msgBuilder);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to publish message to MQTT broker: {ex.Message}");
            }
            finally
            {
                _logger.LogInformation("Disposing MQTT Client..");
                _mqttClient.StopAsync(true);
                _mqttClient.Dispose();
            }
        }
    }
}
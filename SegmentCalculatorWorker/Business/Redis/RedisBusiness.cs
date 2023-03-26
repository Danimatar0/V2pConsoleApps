using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SegmentCalculatorWorker.Models.Common;
using StackExchange.Redis;
using System;
using static ServiceStack.Diagnostics;
using ILogger = Serilog.ILogger;

namespace SegmentCalculatorWorker.Business.Redis
{
    public class RedisBusiness : IRedisBusiness
    {
        private ConnectionMultiplexer _redis;
        private readonly ILogger<RedisBusiness> _logger;
        private readonly GlobalConfig _globalConfig;
        public RedisBusiness(ILogger<RedisBusiness> logger, IOptions<GlobalConfig> options)
        {
            _globalConfig = options.Value;
            _logger = logger;
        }

        public IDatabase GetRedisDatabase()
        {
            if (_redis == null || !_redis.IsConnected)
            {
                Connect();
            }
            return _redis.GetDatabase();
        }
        public void Connect()
        {

            while (true)
            {
                try
                {
                    if (_redis == null)
                    {
                        _logger.LogInformation($"Initiating connection to Redis server");

                        //Configuration options for redis connection establishment
                        ConfigurationOptions configurationOptions = new ConfigurationOptions
                        {
                            AbortOnConnectFail = _globalConfig.RedisConfig.AbortOnFail,
                            ConnectRetry = _globalConfig.RedisConfig.RetryCount,
                            ClientName = _globalConfig.RedisConfig.ConnectionName,
                            ConnectTimeout = _globalConfig.RedisConfig.ConnectionTimeout,
                            DefaultDatabase = _globalConfig.RedisConfig.Database,
                            KeepAlive = _globalConfig.RedisConfig.KeepAlive,
                            Password = _globalConfig.RedisConfig.Password,
                            EndPoints = new EndPointCollection()
                            {
                                _globalConfig.RedisConfig.Host
                            }
                        };

                        //Connecting to redis
                        _redis = ConnectionMultiplexer.Connect(configurationOptions);
                        _logger.LogInformation($"Connected to Redis server: {_globalConfig.RedisConfig.Host}:{_globalConfig.RedisConfig.Port}");
                        break;
                    }
                }
                catch (RedisConnectionException e)
                {
                    _logger.LogError("Could not connect to Redis server: " + e.Message + " after " + _globalConfig.RedisConfig.RetryCount + " retries");
                }
                catch (RedisTimeoutException e)
                {
                    _logger.LogError("Timed out while trying to connect to Redis server: " + e.Message);
                }
            }
        }

        public void Disconnect()
        {
            try
            {
                _redis.Close();
                _redis.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError("Could not disconnect from Redis server: " + e.Message);
            }
        }

        public void StringSet(string key, string value)
        {
            try
            {
                if (_redis == null || !_redis.IsConnected)
                {
                    Connect();
                }
                IDatabase db = _redis.GetDatabase();
                bool isSet = db.StringSet(key, value);

                if (!isSet)
                {
                    _logger.LogError($"Unable to set value {value} for key {key}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
            }
        }

        public string StringGet(string key)
        {
            try
            {
                if (_redis == null || !_redis.IsConnected)
                {
                    Connect();
                }
                IDatabase db = _redis.GetDatabase();
                return db.StringGet(key);
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
                return null;
            }
        }

        public void StringDelete(string key)
        {
            throw new NotImplementedException();
        }


        public void HashDelete(string key, string field)
        {
            throw new NotImplementedException();
        }

        public void HashGet(string key, string field)
        {
            throw new NotImplementedException();
        }

        public void HashGetAll(string key)
        {
            throw new NotImplementedException();
        }

        public void HashSetAdd(string key, string field, string value)
        {
            throw new NotImplementedException();
        }

        public void JsonDelete(string key)
        {
            throw new NotImplementedException();
        }

        public void JsonGet(string key)
        {
            throw new NotImplementedException();
        }

        public void JsonSet(string key, string value, TimeSpan? expiry)
        {
            throw new NotImplementedException();
        }

        public void Publish(string channel, string message)
        {
            throw new NotImplementedException();
        }

        public void PublishToChannels(List<string> channels, string message)
        {
            throw new NotImplementedException();
        }
        public void SortedSetAdd(string key, string value, double score)
        {
            throw new NotImplementedException();
        }

        public void SortedSetRemove(string key, string value)
        {
            throw new NotImplementedException();
        }

        public void SortedSetRemoveRangeByScore(string key, double score)
        {
            throw new NotImplementedException();
        }

        public ISubscriber Subscribe()
        {
            try
            {
                if (_redis == null || !_redis.IsConnected)
                {
                    Connect();
                }
                ISubscriber sub = _redis.GetSubscriber();
                return sub;
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
                return null;
            }

        }

        public void SubscribeToChannels(List<string> channels)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(string channel)
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeFromChannels(List<string> channels)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<HashEntry> HashScan(string key, string pattern, int cursor)
        {
            try
            {
                if (_redis == null || !_redis.IsConnected)
                {
                    Connect();
                }
                IDatabase db = _redis.GetDatabase();
                return db.HashScan(key, pattern, cursor: cursor, pageSize: _globalConfig.RedisConfig.ScanPageSize);
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
                return new List<HashEntry>();
            }
        }

        public RedisValue ListGet(string key, long index)
        {
            try
            {
                if (_redis == null || !_redis.IsConnected)
                {
                    Connect();
                }
                IDatabase db = _redis.GetDatabase();
                return db.ListGetByIndex(key, index);
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
                return RedisValue.Null;
            }
        }

        public void GenerateDummyHashData(string key, int n)
        {
            try
            {
                if (_redis == null || !_redis.IsConnected)
                {
                    Connect();
                }
                IDatabase db = _redis.GetDatabase();
                var rand = new Random();

                for (int i = 0; i < n; i++)
                {
                    var fieldName = $"jw30X:{Guid.NewGuid().ToString().Replace("-", "")}";
                    //var x = rand.Next(28, 32);
                    //var y = rand.Next(29, 33);
                    //var z = rand.Next(31, 33);
                    var x = rand.Next(50, 55);
                    var y = rand.Next(50, 55);
                    var z = rand.Next(0, 5);
                    var value = $"{x},{y},{z}";
                    db.HashSetAsync(key, fieldName, value);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
            }
        }

        public void GenerateDummyDeviceCoordinateStringPair(string topicPrefix,int n)
        {
            try
            {
                if (_redis == null || !_redis.IsConnected)
                {
                    Connect();
                }
                IDatabase db = _redis.GetDatabase();
                var rand = new Random();

                for (int i = 0; i < n; i++)
                {
                    // Generate random key and value
                    string key = $"{topicPrefix}:" + Helpers.Helpers.RandomString(rand, 15); // replace 10 with desired length of random string

                    for(int j=0; j<2; j++)
                    {
                        var x = rand.Next(50, 55);
                        var y = rand.Next(50, 55);
                        var z = rand.Next(0, 5); // adjust range of z as desired
                        string value = $"{x},{y},{z}";
                        string fieldname = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        //Console.WriteLine("Current time: " + fieldname);
                        // Insert key-value pair into Redis database
                        db.HashSetAsync(key, fieldname, value);
                        Thread.Sleep(2000);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
            }
        }
    }
}

using SegmentCalculatorWorker.Models.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ILogger = Serilog.ILogger;

namespace SegmentCalculatorWorker.Business.Redis
{
    public class RedisBusiness : IRedisBusiness
    {
        private readonly ILogger<RedisBusiness> _logger;
        private readonly IOptions<GlobalConfig> _globalConfig;
        //private readonly IRedisDatabase _db;
        private readonly GlobalConfig _config;
        private static ConfigurationOptions _configurationOptions;
        private ConnectionMultiplexer _multiplexer;

        public RedisBusiness(ILogger<RedisBusiness> logger, IOptions<GlobalConfig> options)
        {
            _globalConfig = options;
            _config = _globalConfig.Value;
            _logger = logger;
            _configurationOptions = GetConfigurationOptions(_config);
        }

        private static ConfigurationOptions GetConfigurationOptions(GlobalConfig config)
        {
            return new ConfigurationOptions
            {
                AbortOnConnectFail = config.RedisConfig.AbortOnFail,
                ConnectRetry = config.RedisConfig.RetryCount,
                ClientName = config.RedisConfig.ConnectionName,
                ConnectTimeout = config.RedisConfig.ConnectionTimeout,
                DefaultDatabase = config.RedisConfig.Database,
                KeepAlive = config.RedisConfig.KeepAlive,
                Password = config.RedisConfig.Password,
                EndPoints = new EndPointCollection()
                            {
                                config.RedisConfig.Host
                            }
            };
        }
        public void StringSet(string key, string value)
        {
            try
            {
                IDatabase db = GetRedisDatabase();

                bool isSet = db.StringSet(key, value);

                if (!isSet)
                {
                    _logger.LogError($"Unable to set value {value} for key {key}");
                }
                Disconnect(db);
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
                IDatabase db = GetRedisDatabase();
                string str = db.StringGet(key);

                Disconnect(db);
                return str;
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
            try
            {
                IDatabase db = GetRedisDatabase();

                db.HashSet(key, field, value);
                Disconnect(db);
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
            }
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
                IDatabase db = GetRedisDatabase();
                IEnumerable<HashEntry> entries = db.HashScan(key, pattern, cursor: cursor, pageSize: _config.RedisConfig.ScanPageSize);

                //Disconnect(db);

                return entries;
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
                IDatabase db = GetRedisDatabase();
                RedisValue value = db.ListGetByIndex(key, index);

                //Disconnect(db);
                return value;
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
                return RedisValue.Null;
            }
        }

        public void ListDel(string key)
        {
            try
            {
                IDatabase db = GetRedisDatabase();
                db.KeyDelete(key);
            }
            catch (Exception e)
            {
                _logger.LogError("Could not delete list key from Redis server: " + e.Message);
            }
        }
        public void GenerateDummyHashData(string key, int n)
        {
            try
            {
                IDatabase db = GetRedisDatabase();
                var rand = new Random();

                for (int i = 0; i < n; i++)
                {
                    var fieldName = $"jw30X:{Guid.NewGuid().ToString().Replace("-", "")}";
                    var speed = rand.Next(10, 120);
                    //var fieldName = $"HXbW5:{Guid.NewGuid().ToString().Replace("-", "")}";
                    //var x = rand.Next(28, 32);
                    //var y = rand.Next(29, 33);
                    //var z = rand.Next(31, 33);
                    var x = rand.Next(50, 55);
                    var y = rand.Next(50, 55);
                    var z = rand.Next(0, 5);
                    var value = $"{x},{y},{z}:{speed}";
                    db.HashSetAsync(key, fieldName, value);
                }
                Disconnect(db);

            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
            }
        }

        public IEnumerable<object> GetList(string key)
        {

            try
            {
                List<object> enumerable = new List<object>();
                IDatabase db = GetRedisDatabase();

                if (db.IsConnected(key))
                {
                    RedisValue[] redisValues = db.ListRange(key);

                    foreach (RedisValue redisValue in redisValues)
                    {
                        enumerable.Add((object)redisValue);
                    }

                }
                Disconnect(db);
                return enumerable;
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
                return new List<object>();
            }
        }
        public void Disconnect(IDatabase db)
        {
            db.Multiplexer.Close();
            db.Multiplexer.Dispose();
        }

        public IDatabase GetRedisDatabase()
        {
            return GetRedisConnection().GetDatabase();
            //_logger.LogInformation($"Connection Established : {multiplexer.IsConnected}");
            //return multiplexer.GetDatabase();
        }

        private ConnectionMultiplexer GetRedisConnection()
        {
            if (_multiplexer == null)
            {
                _multiplexer = ConnectionMultiplexer.Connect(_configurationOptions);
            }
            return _multiplexer;
        }
        public IEnumerable<RedisKey> ScanKeys(string pattern)
        {
            IServer server = default;
            List<RedisKey> enumerable = new List<RedisKey>();
            try
            {
                ConnectionMultiplexer connection = GetRedisConnection();

                server = connection.GetServer($"{_config.RedisConfig.Host}:{_config.RedisConfig.Port}");
                IDatabase db = connection.GetDatabase();

                // show all keys in database 0 that include the given pattern in their name
                enumerable.AddRange(server.Keys(pattern: pattern).ToList());

                return enumerable;
            }
            catch (Exception e)
            {
                _logger.LogError("Could not get value from Redis server: " + e.Message);
                return new List<RedisKey>();
            }
            //finally
            //{
            //    server.FlushDatabase();
            //}
        }
    }
}


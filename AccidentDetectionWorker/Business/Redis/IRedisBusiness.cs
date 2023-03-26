using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentDetectionWorker.Business.Redis
{
    public interface IRedisBusiness
    {
        public IDatabase GetRedisDatabase();
        public void Connect();
        public void Disconnect();
        public void StringSet(string key, string value);
        public string StringGet(string key);
        public void StringDelete(string key);
        public ISubscriber Subscribe();
        public void Unsubscribe(string channel);
        public void Publish(string channel, string message);
        public void SubscribeToChannels(List<string> channels);
        public void UnsubscribeFromChannels(List<string> channels);
        public void PublishToChannels(List<string> channels, string message);
        public void JsonGet(string key);
        public void JsonDelete(string key);
        public void JsonSet(string key, string value, TimeSpan? expiry);
        public void SortedSetAdd(string key, string value, double score);
        public void SortedSetRemove(string key, string value);
        public void SortedSetRemoveRangeByScore(string key, double score);
        public void HashSetAdd(string key, string field, string value);
        public void HashGet(string key, string field);
        public void HashDelete(string key, string field);
        public void HashGetAll(string key);
        public IEnumerable<HashEntry> HashScan(string key, string pattern, int cursor);
        public RedisValue ListGet(string key, long index);
        public void GenerateDummyHashData(string key, int n);
    }
}

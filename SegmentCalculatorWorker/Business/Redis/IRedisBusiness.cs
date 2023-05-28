using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SegmentCalculatorWorker.Business.Redis
{
    public interface IRedisBusiness
    {
        public IDatabase GetRedisDatabase();
        //public ConnectionMultiplexer Connect();
        public void Disconnect(IDatabase db);
        public void StringSet(string key, string value);
        public string StringGet(string key);
        public void StringDelete(string key);
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
        public void ListDel(string key);
        public void GenerateDummyHashData(string key, int n);

        public IEnumerable<object> GetList(string key);
        public IEnumerable<RedisKey> ScanKeys(string pattern);

    }
}

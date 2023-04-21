using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace StorageProxy
{
    public class DualCache : IMemoryCache
    {
        private MemoryCache _memoryCache;
        private readonly int _dbIndex;
        private ConnectionMultiplexer _multiplexer;
        private JsonSerializerOptions _serializerSettings;

        public DualCache(ConnectionMultiplexer multiplexer, MemoryCache memoryCache, int dbIndex)
        {
            _multiplexer = multiplexer;
            _memoryCache = memoryCache;
            _dbIndex = dbIndex;
            this._serializerSettings = new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
        }


        public void Dispose()
        {
            _memoryCache.Dispose();
            this._multiplexer = null;
        }

        public ICacheEntry CreateEntry(object key)
        {
            return this._memoryCache.CreateEntry(key);
        }


        public void Remove(object key)
        {
            _multiplexer.GetDatabase(_dbIndex).KeyDelete(key.ToString());
        }

        public bool TryGetValue(object key, out object value)
        {
            _memoryCache.TryGetValue(key, out value);
            if (value == null)
            {
                try
                {
                    var redisValue =  _multiplexer.GetDatabase(_dbIndex).StringGet(key.ToString());
                    if (redisValue.HasValue)
                    {
                        value = JsonSerializer.Deserialize(redisValue, value?.GetType(), _serializerSettings);
                        _memoryCache.Set(key, value, TimeSpan.FromMinutes(1));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($" {DateTime.UtcNow:g} - Redis Timeout for ${key.ToString()}");
                    return false;
                }
            }

            return value != null;
        }

        public void RedisStore(string key, object item, TimeSpan absoluteExpirationRelativeToNow)
        {
            try
            {
                _multiplexer.GetDatabase(_dbIndex)
                    .StringSet(key, JsonSerializer.Serialize(item, _serializerSettings));
            }
            catch
            {
                // ignored
            }
        }
    }

    public static class CacheExtensions
    {
        public static TItem Store<TItem>(this IMemoryCache cache, string key, TItem value,
            TimeSpan absoluteExpirationRelativeToNow)
        {
            var entry = cache.CreateEntry(key);
            entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            entry.Value = value;
            entry.Dispose();

            if (cache is DualCache dual)
            {
                dual.RedisStore(key, value, absoluteExpirationRelativeToNow);
            }

            return value;
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace StorageProxy
{
    public class DualCache : IMemoryCache
    {

        private MemoryCache _memoryCache;
        private IDatabase _redisDatabase;

        public DualCache(IDatabase redisDatabase, MemoryCache memoryCache)
        {
            _redisDatabase = redisDatabase;
            _memoryCache = memoryCache;
        }


        public void Dispose()
        {
            _memoryCache.Dispose();
            this._redisDatabase = null;
        }

        public ICacheEntry CreateEntry(object key)
        {
            return this._memoryCache.CreateEntry(key);
         
        }

      

        public void Remove(object key)
        {
            _redisDatabase.KeyDelete(key.ToString());
        }

        public bool TryGetValue(object key, out object value)
        {
            _memoryCache.TryGetValue(key, out  value);
            if (value == null)
            {
                var redisValue = _redisDatabase.StringGet(key.ToString());
                if (redisValue.HasValue)
                {
                    value = JsonConvert.DeserializeObject(redisValue, value?.GetType());
                }
            }

            return value != null;
        }
    }
    
    public class Ca


}
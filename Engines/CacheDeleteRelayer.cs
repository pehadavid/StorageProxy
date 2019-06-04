using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace StorageProxy.Engines
{
    public class CacheDeleteRelayer : Relayer
    {
    

        public override Task WriteRelayAsync(string[] uriSegments, HttpContext context)
        {
            if (uriSegments.Length < 1)
            {
                return context.Response.WriteNotFoundAsync();
            }

            if (context.Request.Query.TryGetValue("key", out var key))
            {
                var cacheKey = RelayDomain + key;
                var cacheData = memoryCache.Get<CachedProxyContent>(cacheKey);
                if (cacheData != null)
                {
                    memoryCache.Remove(cacheKey);
                    var offsetNow = DateTimeOffset.UtcNow;
                    var cacheInfo = new
                    {
                        cacheData.ContentType,
                        cacheData.DateGenerated,
                        cacheData.Expires,
                        DeletedAt = offsetNow,
                        ShortedTime = (offsetNow - cacheData.DateGenerated)
                    };
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync(JsonConvert.SerializeObject(cacheInfo));
                }


            }
            return context.Response.WriteNotFoundAsync();
        }

        public CacheDeleteRelayer(IMemoryCache memcache, string rDomain, List<string> allowrdAllowedRemoteHosts) : base(memcache, rDomain, allowrdAllowedRemoteHosts)
        {
        }
    }
}

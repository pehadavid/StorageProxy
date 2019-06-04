using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace StorageProxy.Engines
{
    public class CacheInfoRelayer : Relayer
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
                    var expires = cacheData.Expires - (DateTime.UtcNow - cacheData.DateGenerated);
                    var cacheInfo = new
                    {
                        cacheData.ContentType,
                        cacheData.DateGenerated,
                        TTL = cacheData.Expires,
                        Expires = expires
                    };
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync(JsonConvert.SerializeObject(cacheInfo));
                }


            }
            return context.Response.WriteNotFoundAsync();
        }

        public CacheInfoRelayer(IMemoryCache memcache, string rDomain, List<string> allowrdAllowedRemoteHosts) : base(memcache, rDomain, allowrdAllowedRemoteHosts)
        {
        }
    }

     
}

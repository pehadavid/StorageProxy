using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace StorageProxy.Engines
{
    public class ProxyRelayer : Relayer
    {
   
        public override Task WriteRelayAsync(string[] uriSegments, HttpContext context)
        {
    
            var uri = RelayDomain + string.Join("/", uriSegments);
            return GetFromCacheOrRemote(uri, context);
        }

        public ProxyRelayer(IMemoryCache memcache, string rDomain, List<string> allowrdAllowedRemoteHosts) : base(memcache, rDomain, allowrdAllowedRemoteHosts)
        {
        }
    }
}
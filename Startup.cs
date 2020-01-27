using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StorageProxy.Engines;

namespace StorageProxy
{
    public class Startup
    {

        IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions() { });
        private DualCache _dualCache;
        protected string relayDomain;
        protected List<string> allowedHosts;
        protected List<ImageSize> imageSizes;


        protected RedisConfigurationSettings _redisConfigurationSettings;
        #region relayers

        protected DecoderRelayer decoderRelayer;
        protected CompressRelayer compressRelayer;
        protected CacheInfoRelayer cacheInfoRelayer;
        protected CacheDeleteRelayer cacheDeleteRelayer;
        protected ProxyRelayer proxyRelayer;

        #endregion
        
        public Startup(IConfiguration configuration)
        {
            this._redisConfigurationSettings = 
                configuration.GetSection("Redis").Get<RedisConfigurationSettings>() ?? new RedisConfigurationSettings() {Enabled = false};
            this.relayDomain = configuration["RelayDomain"];
            this.imageSizes = configuration.GetSection("AllowedSizes").Get<ImageSize[]>().ToList();
            
            var hosts = configuration.GetSection("ListenOn").GetChildren().ToArray().Select(c => c.Value).ToArray();
            this.allowedHosts = hosts.ToList();
            var cacheManager = _redisConfigurationSettings.Enabled ? _dualCache : memoryCache;
            this.decoderRelayer = new DecoderRelayer(cacheManager, relayDomain, allowedHosts);
            this.compressRelayer = new CompressRelayer(cacheManager, relayDomain, allowedHosts, imageSizes);
            this.cacheInfoRelayer = new CacheInfoRelayer(cacheManager, relayDomain, allowedHosts);
            this.cacheDeleteRelayer = new CacheDeleteRelayer(cacheManager, relayDomain, allowedHosts);
            this.proxyRelayer = new ProxyRelayer(cacheManager, relayDomain, allowedHosts);
        }
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            
            app.Run(async (context) =>
            {
                await RelayPath(context.Request.Path, context);
            });
        }

        private Task RelayPath(PathString requestPath, HttpContext context)
        {
            if (requestPath == null || !requestPath.HasValue || requestPath.Value == "/")
            {
                return Relayer.WriteProxyPage(context);
            }
            else
            {
                return WriteRelay(context);
            }
        }

        private Task WriteRelay(HttpContext context)
        {
            string[] segments = context.Request.Path.GetSegments();
            //if (segments.Length < 2)
            //    return Relayer.WriteProxyPage(context);
#if DEBUG
            //InjectUri(segments);
#endif
            Relayer relayer = null;
            switch (segments[0])
            {
                case "dlx":
                    relayer = decoderRelayer;
                    break;
                case "compress":
                    relayer = compressRelayer;
                    break;
                case "cache-info":
                    relayer = cacheInfoRelayer;
                    break;
                case "cache-delete":
                    relayer = cacheDeleteRelayer;
                    break;
                default:
                    relayer = proxyRelayer;
                    break;
               
            }


            return relayer.WriteRelayAsync(segments, context);

        }



















    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace StorageProxy.Engines
{
    public class DecoderRelayer : Relayer
    {

        public override Task WriteRelayAsync(string[] uriSegments, HttpContext context)
        {
            try
            {
                var bytes = uriSegments.Skip(1).FirstOrDefault().StringToByteArray();
                var remotePath = Encoding.ASCII.GetString(bytes);
                if (!IsRemoteValid(ref remotePath))
                {
                    return WriteProxyPage(context);
                }
                else
                {
                    return GetFromCacheOrRemote(remotePath, context);
                }
            }
            catch (Exception)
            {

                return WriteProxyPage(context);
            }
        }

        private bool IsRemoteValid(ref string remotePath)
        {
            try
            {
                TryCorrectRemote(ref remotePath);
                var url = new Uri(remotePath);
                return _allowedRemoteHosts.Contains(url.Host);
            }
            catch (Exception)
            {

                return false;
            }
        }

        private void TryCorrectRemote(ref string remotePath)
        {
            foreach (string listenOn in _allowedRemoteHosts)
            {
                remotePath = remotePath.Replace(listenOn, RelayDomain);
            }
            
            if (!remotePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (remotePath.StartsWith("//"))
                {
                    remotePath = "http:" + remotePath;
                }

                if (remotePath.StartsWith("/")) // assuming that we want to reach blob domain
                    remotePath = RelayDomain + remotePath;

            }

        }


        public DecoderRelayer(IMemoryCache memcache, string rDomain, List<string> allowrdAllowedRemoteHosts) : base(memcache, rDomain, allowrdAllowedRemoteHosts)
        {

        }
    }
}

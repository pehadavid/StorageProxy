using System;

namespace StorageProxy
{
    public class CachedProxyContent
    {
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
        public string Etag { get; set; }
        public DateTimeOffset DateGenerated { get; set; }
        public TimeSpan Expires { get; set; }
    }
}
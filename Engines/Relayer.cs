using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace StorageProxy.Engines
{
    public abstract class Relayer
    {
        protected string RelayDomain;
        protected IMemoryCache memoryCache;
        public TimeSpan DefaultExpire => TimeSpan.FromMinutes(30);

        protected Relayer(IMemoryCache memcache, string rDomain, List<string> allowrdAllowedRemoteHosts)
        {
            this.RelayDomain = rDomain;
            this._allowedRemoteHosts = allowrdAllowedRemoteHosts;
            this.memoryCache = memcache;
        }

        protected readonly List<string> _allowedRemoteHosts;
        public abstract Task WriteRelayAsync(string[] uriSegments, HttpContext context);

        public static Task WriteProxyPage(HttpContext context)
        {
            AddHeaderCache(context.Response.Headers, "0");
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = 200;

            string bodyTemplate =
                $"<html><head> <style></style></head><body><p>{Cow}</p><p>{File.GetCreationTime(Assembly.GetExecutingAssembly().Location)}</p></body> </html>";
            return context.Response.WriteAsync(bodyTemplate);
        }

        protected static void AddHeaderCache(IHeaderDictionary headers, string etag)
        {
            string expires = DateTime.UtcNow.AddDays(8).ToString("R");
            headers.Add("Cache-Control", "public,max-age=676800");
            headers.Add("Expires", expires);
            headers.Add("Etag", etag);
            headers.Add("Access-Control-Allow-Origin", "*");
        }

        protected virtual async Task GetFromCacheOrRemote(string remotePath, HttpContext context)
        {
            CachedProxyContent cached = await GetOrStoreData(remotePath);
            if (cached == null)
            {
                await context.Response.WriteEmptyAsync(404);
            }
            else
            {
                if (cached.Etag.Equals(context.Request.Headers["If-none-match"].FirstOrDefault()))
                {
                    await context.Response.WriteEmptyAsync(304);
                }
                else
                {
                    context.Response.Headers.Add("Content-Type", cached.ContentType);
                    AddHeaderCache(context.Response.Headers, cached.Etag);
                    await context.Response.Body.WriteAsync(cached.Content, 0, cached.Content.Length);
                }
            }
        }

        protected virtual async Task<CachedProxyContent> GetOrStoreData(string remotePath)
        {
            CachedProxyContent cached = memoryCache.Get<CachedProxyContent>(remotePath);

            if (cached == null)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    var response = await client.GetAsync(remotePath);
                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    byte[] innerBuffer = await response.Content.ReadAsByteArrayAsync();
                    cached = new CachedProxyContent()
                    {
                        Content = ReEncodeImage(innerBuffer, response.Content.Headers.ContentType.MediaType),
                        ContentType = response.Content.Headers.ContentType.MediaType,
                        Etag = innerBuffer.GetHash(),
                        DateGenerated = DateTimeOffset.UtcNow,
                    };
                    WriteCache(remotePath, cached, response.Headers.CacheControl);
                }
                catch (Exception e)
                {
                    throw new AggregateException($"Cannot fetch data from {remotePath}", e);
                }
            }

            return cached;
        }

        private byte[] ReEncodeImage(byte[] innerBuffer, string contentTypeMediaType)
        {
            if (contentTypeMediaType.Contains("jpeg") || contentTypeMediaType.Contains("jpg"))
            {
                using (MagickImage image = new MagickImage(innerBuffer))
                {
                    image.Quality = 85;
                    return image.ToByteArray(MagickFormat.Pjpeg);
                }
            }
            else if (contentTypeMediaType.Contains("png"))
            {
                using (MagickImage image = new MagickImage(innerBuffer))
                {
                    image.Strip();
                    return image.ToByteArray(MagickFormat.Png);
                }
            }

            return innerBuffer;
        }
        //private Task WriteCache(string path, string contentType, byte[] buffer, CacheControlHeaderValue headersCacheControl)
        //{
        //    var pxy = new CachedProxyContent()
        //    {
        //        Content = buffer,
        //        ContentType = contentType,
        //        Etag = buffer.GetHash()

        //    };
        //    return WriteCache(path, pxy, headersCacheControl);
        //}

        protected void WriteCache(string path, CachedProxyContent proxyContent,
            CacheControlHeaderValue headersCacheControl)
        {
            TimeSpan expire = headersCacheControl?.MaxAge ?? DefaultExpire;
            proxyContent.Expires = expire;
            memoryCache.Set(path, proxyContent, expire);
        }

        public static string Cow => @"We need some milk. Moo ! Moo !";
    }
}
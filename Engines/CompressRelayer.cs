using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace StorageProxy.Engines
{
    public class CompressRelayer : Relayer
    {
        public override Task WriteRelayAsync(string[] uriSegments, HttpContext context)
        {
            var segments = uriSegments.Skip(1).ToArray();
            //size is the first segement

            return GetFromCacheOrRemote(segments, context);
        }

        protected async Task GetFromCacheOrRemote(string[] uriSegments, HttpContext context)
        {
            var segments = uriSegments.Skip(1).ToArray();
            var uri = RelayDomain + string.Join("/", segments);
            CachedProxyContent cached = await GetOrStoreData(uri);
            if (cached == null)
            {
                await context.Response.WriteNotFoundAsync();
            }
            else
            {
                context.Request.Headers.Add("X-MEMORY-X", "fetched");
                await ResizeStoreWrite(cached, context, uriSegments[0], uri);
            }
        }

        private async Task ResizeStoreWrite(CachedProxyContent cached, HttpContext context, string strSize,
            string ressourceUri)
        {
            try
            {
                string xmemValue = "first-pass";
                var size = ConvertStrSize(strSize);
                string resizedCacheKey = $"{ressourceUri}/{strSize}";
                var resizedCached = this.memoryCache.Get<CachedProxyContent>(resizedCacheKey);

                if (resizedCached == null)
                {
                    var resized = JustResize(cached, size);
                    resizedCached = new CachedProxyContent()
                    {
                        Content = resized,
                        ContentType = cached.ContentType,
                        Etag = resized.GetHash()
                    };
                    WriteCache(resizedCacheKey, resizedCached, null);
                }
                else
                {
                    xmemValue = "sized";
                }

                var ifNoneMatch = context.Request.Headers["If-none-match"].FirstOrDefault();
                context.Response.Headers.Add("Content-Type", resizedCached?.ContentType);
                context.Response.Headers.Add("Content-Length", (resizedCached?.Content.Length).GetValueOrDefault(0).ToString());
                if (resizedCached.Etag.Equals(ifNoneMatch))
                {
                    await context.Response.WriteEmptyAsync(304);
                }
                else
                {
                    AddHeaderCache(context.Response.Headers, resizedCached.Etag);
                    context.Response.Headers.Add("X-MEMORY-X", xmemValue);
            
                    await context.Response.Body.WriteAsync(resizedCached.Content, 0, resizedCached.Content.Length);
                }
            }
            catch (InvalidSegmentSizeException)
            {
                await context.Response.WriteEmptyAsync(406);
            }
        }

        // private static JpegEncoder NiceEncoder
        // {
        //     get
        //     {
        //         JpegEncoder encoder = new JpegEncoder
        //         {
        //             IgnoreMetadata = true,
        //             Quality = 85,
        //             Subsample = JpegSubsample.Ratio420
        //         };
        //         return encoder;
        //     }
        // }

        private byte[] JustResize(CachedProxyContent cached, (int, int) size)
        {
            using (MemoryStream ms = new MemoryStream(cached.Content))
            using (MagickImage magickImage = new MagickImage(ms.ToArray()))
            {
                magickImage.Resize(size.Item1, size.Item2);
                magickImage.Quality = 85;
                magickImage.Density = new Density(192, DensityUnit.PixelsPerInch);
                return magickImage.ToByteArray(MagickFormat.Pjpeg);
        
            }
        }

        private (int, int) ConvertStrSize(string s)
        {
            try
            {
                const string exp = @"([0-9]+)?\-([0-9]+)?";
                var mCollection = Regex.Match(s, exp);

                int width = int.Parse(mCollection.Groups[1].Value);
                int height = int.Parse(mCollection.Groups[2].Value);
                AssertSize(width, height, s);
                return (width, height);
            }
            catch (Exception)
            {
                throw new InvalidSegmentSizeException($"Cannot do the job for segment {s}. Bad Segment.");
            }
        }

        private void AssertSize(int width, int height, string sizeKey)
        {
            if (width < 1 || height < 1)
            {
                throw new InvalidSegmentSizeException($"Cannot do the job for w: {width} h: {height}. Bad values.");
            }

            var size = SizeList[sizeKey];

            if (size == null || size.Height == 0 || size.Width == 0)
            {
                throw new InvalidSegmentSizeException(
                    $"Cannot do the job for w: {width} h: {height}. No compatible size");
            }
        }


        private Dictionary<string, ImageSize> SizeList { get; set; }
        public CompressRelayer(IMemoryCache memcache, string rDomain, List<string> allowrdAllowedRemoteHosts, List<ImageSize>  _imageSizes) : base(
            memcache, rDomain, allowrdAllowedRemoteHosts)
        {
            this.SizeList = new Dictionary<string, ImageSize>();
            foreach (ImageSize imageSize in _imageSizes)
            {
                SizeList.TryAdd($"{imageSize.Width}-{imageSize.Height}", imageSize);
            }
        }
    }

    public class InvalidSegmentSizeException : Exception
    {
        public InvalidSegmentSizeException(string s) : base(s)
        {
        }
    }
}
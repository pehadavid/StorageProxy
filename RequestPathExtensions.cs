using System.Linq;
using Microsoft.AspNetCore.Http;

namespace StorageProxy
{
    internal static class RequestPathExtensions
    {
        public static string[] GetSegments(this PathString sourcePath)
        {
            return sourcePath.Value.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }
    }
}
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace StorageProxy.Engines
{
    public static class ResponseWriterHelper
    {
        public static Task WriteNotFoundAsync(this HttpResponse response)
        {
            return response.WriteEmptyAsync(404);
        }

        public static Task WriteEmptyAsync(this HttpResponse response, int statusCode)
        {
            response.StatusCode = statusCode;
           return Task.CompletedTask;
            
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreCodedChatbot.Logging
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context, ILogger<ErrorHandlingMiddleware> logger)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                logger.LogCritical(e, $"Completely unhandled error encountered");
                throw;
            }
        }
    }
}
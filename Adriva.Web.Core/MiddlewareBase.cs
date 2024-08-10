using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Adriva.Web.Core
{
    public abstract class MiddlewareBase
    {
        protected RequestDelegate Next { get; private set; }

        protected ILogger Logger { get; private set; }

        protected MiddlewareBase(RequestDelegate next, ILogger logger)
        {
            this.Next = next;
            this.Logger = logger;
        }
    }
}

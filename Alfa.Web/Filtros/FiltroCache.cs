using Microsoft.AspNetCore.Mvc.Filters;

namespace Alfa.Web.Filtros
{
    public class FiltroCache : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var response = context.HttpContext.Response;
            response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            response.Headers.Pragma = "no-cache";
            response.Headers.Expires = "0";

            base.OnResultExecuting(context);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Idempotency.API.Attributes
{
    public class IdempotentFullParametersAttribute : ActionFilterAttribute
    {
        public int ExpireHours { get; set; }

        private static readonly MemoryCache Cache = new(new MemoryCacheOptions());

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var values = context.ActionArguments;
            var requestBodyJson = new Dictionary<string, object>();

            foreach (var value in values)
            {
                if (value.Key != null)
                {
                    var bodyJson = GetJsonElementFromRequest(value.Value);
                    if (bodyJson.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in bodyJson.EnumerateObject())
                        {
                            requestBodyJson[property.Name] = property.Value;
                        }
                    }
                    else
                    {
                        requestBodyJson[value.Key] = value.Value;
                    }
                }
            }

            var requestHash = HashHelper.ComputeHash(requestBodyJson.ToString());
            var cacheKey = $"Idempotency-{requestHash}";

            if (Cache.TryGetValue(cacheKey, out ObjectResult cachedResult))
            {
                context.Result = cachedResult;
                return;
            }

            var executedContext = await next();
            if (executedContext.Result is ObjectResult objectResult)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(ExpireHours)
                };

                Cache.Set(cacheKey, objectResult, cacheEntryOptions);
            }
        }

        private static JsonElement GetJsonElementFromRequest(object request)
        {
            string json = request switch
            {
                JsonElement element => element.ToString(),
                string jsonString => jsonString,
                _ => JsonSerializer.Serialize(request)
            };
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
    }

}

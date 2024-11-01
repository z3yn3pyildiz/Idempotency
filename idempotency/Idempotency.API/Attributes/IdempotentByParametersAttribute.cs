using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text;

namespace Idempotency.API.Attributes
{
    public class IdempotentByParametersAttribute : ActionFilterAttribute
    {
        public int ExpireHours { get; set; }
        public string[] SelectedParameters { get; set; }

        private static readonly MemoryCache Cache = new(new MemoryCacheOptions());

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var values = context.ActionArguments;
            var requestBodyJson = new Dictionary<string, object>();

            foreach (var value in values)
            {
                if (value.Key!=null)
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

            var selectedBodyValues = GetSelectedParameters(requestBodyJson);
            var requestHash = HashHelper.ComputeHash(selectedBodyValues);
            var cacheKey = $"Idempotency-{requestHash}";

            if (TryGetCachedResult(cacheKey, out var cachedResult))
            {
                context.Result = cachedResult;
                return;
            }

            var executedContext = await next();

            if (executedContext.Result is ObjectResult objectResult)
            {
                CacheResponse(cacheKey, objectResult);
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

        private string GetSelectedParameters(Dictionary<string, object> bodyJson)
        {
            var selectedValues = new StringBuilder();
            foreach (var param in SelectedParameters)
            {
                if (bodyJson.TryGetValue(param, out var value))
                {
                    selectedValues.Append(value);
                }
            }
            return selectedValues.ToString();
        }

        private bool TryGetCachedResult(string cacheKey, out ObjectResult cachedResult)
        {
            return Cache.TryGetValue(cacheKey, out cachedResult);
        }

        private void CacheResponse(string cacheKey, ObjectResult objectResult)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(ExpireHours)
            };

            Cache.Set(cacheKey, objectResult, cacheEntryOptions);
        }
    }
}
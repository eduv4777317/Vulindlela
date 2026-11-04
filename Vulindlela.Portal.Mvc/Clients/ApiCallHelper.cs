using System.Net.Http.Json;
using Vulindlela.Portal.Mvc.Models;

namespace Vulindlela.Portal.Mvc.Clients
{
    public static class ApiCallHelper
    {
        public static async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send, ILogger logger)
        {
            try
            {
                using var response = await send();
                if (response.IsSuccessStatusCode)
                {
                    var value = response.Content.Headers.ContentLength == 0
                        ? default
                        : await response.Content.ReadFromJsonAsync<T>();
                    return new ApiResult<T>(true, value, (int)response.StatusCode);
                }

                var problem = await TryReadProblemAsync(response);
                return new ApiResult<T>(true, default, (int)response.StatusCode, problem?.Title);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                logger.LogWarning(ex, "Service call failed; showing the page without this data.");
                return ApiResult<T>.Unavailable();
            }
        }

        private static async Task<ProblemDto?> TryReadProblemAsync(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<ProblemDto>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

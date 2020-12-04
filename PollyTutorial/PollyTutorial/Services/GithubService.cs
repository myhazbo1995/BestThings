using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using PollyTutorial.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PollyTutorial.Services
{
    public class GithubService : IGithubService
    {
        private const int MaxRetries = 3;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AsyncRetryPolicy _retryPolicy;
        private static readonly Random random = new Random();

        public GithubService(IHttpClientFactory httpClientFactory)
        {
            this._httpClientFactory = httpClientFactory;
            _retryPolicy = Policy.Handle<HttpRequestException>(exceptionPredicate =>
                {
                    return exceptionPredicate.Message != "This is a fake request";
                })
                .WaitAndRetryAsync(MaxRetries, times => TimeSpan.FromMilliseconds(100 * times));
        }

        public async Task<GithubUser> GetUserByUserNameAsync(string userName)
        {
            var client = _httpClientFactory.CreateClient("Github");
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                if (random.Next(1, 3) == 1)
                    throw new HttpRequestException("This is a fake request");

                var result = await client.GetAsync($"/users/{userName}");

                if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                var resultString = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GithubUser>(resultString);
            });
        }
    }

    public interface IGithubService
    {
        Task<GithubUser> GetUserByUserNameAsync(string userName);
    }
}

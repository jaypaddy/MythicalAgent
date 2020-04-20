using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System.Net.Http;

namespace JayPaddy.Agent
{
    public static class JobBuilder
    {
        [FunctionName("JobBuilder")]
        public static void Run([TimerTrigger("*/30 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"JobBuilder:C# Timer trigger function executed at: {DateTime.Now}");
            log.LogInformation($"JobBuilder:TENANTID:{System.Environment.GetEnvironmentVariable("TENANTID")}");
            log.LogInformation($"JobBuilder:OAUTHCLIENTID:{System.Environment.GetEnvironmentVariable("OAUTHCLIENTID")}");
            log.LogInformation($"JobBuilder:OAUTHSECRET:{System.Environment.GetEnvironmentVariable("OAUTHSECRET")}");
            log.LogInformation($"JobBuilder:SCOPE:{System.Environment.GetEnvironmentVariable("SCOPE")}");

            string bearerToken = GetBearerToken().GetAwaiter().GetResult();
            log.LogInformation($"JobBuilder:Bearer Token:{bearerToken}");

            string url = System.Environment.GetEnvironmentVariable("JOBEXECUTOR2");
            log.LogInformation($"JJobBuilder:OBEXECUTOR2:{url}");

            var res = TriggerJob(bearerToken,url);
            log.LogInformation($"JobBuilder:JobExecutor Output:{res}");
        }

        public static async Task<string> GetBearerToken()
        {
            string tenantID = System.Environment.GetEnvironmentVariable("TENANTID");
            string AadInstance = $"https://login.microsoftonline.com/{tenantID}/"; 
            string  Scope = System.Environment.GetEnvironmentVariable("SCOPE");
            string[] scopes = new string[] { Scope };
            string clientID = System.Environment.GetEnvironmentVariable("OAUTHCLIENTID");
            string clientSecret = System.Environment.GetEnvironmentVariable("OAUTHSECRET");

            IConfidentialClientApplication app;
            app = ConfidentialClientApplicationBuilder.Create(clientID)
                                                    .WithClientSecret(clientSecret)
                                                    .WithAuthority(new Uri(AadInstance))
                                                    .Build();
            AuthenticationResult result = await app.AcquireTokenForClient(scopes)
                  .ExecuteAsync();

            return result.AccessToken;
        }
    
        public static async Task<string> TriggerJob(string bearerToken, string url)
        {
            System.Net.Http.HttpResponseMessage response;
            HttpClient hClient = new HttpClient();
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url );
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            response = await hClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
    }
}

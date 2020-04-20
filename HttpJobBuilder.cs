using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;



using Microsoft.Azure.WebJobs.Host;
using Microsoft.Identity.Client;
using System.Net.Http;

namespace Jaypaddy.Agent
{
    public static class HttpJobBuilder
    {
        [FunctionName("HttpJobBuilder")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("HttpJobBuilder:C# HTTP trigger function processed a request.");
            
            log.LogInformation($"HttpJobBuilder:TENANTID:{System.Environment.GetEnvironmentVariable("TENANTID")}");
            log.LogInformation($"HttpJobBuilder:OAUTHCLIENTID:{System.Environment.GetEnvironmentVariable("OAUTHCLIENTID")}");
            log.LogInformation($"HttpJobBuilder:OAUTHSECRET:{System.Environment.GetEnvironmentVariable("OAUTHSECRET")}");
            log.LogInformation($"HttpJobBuilder:SCOPE:{System.Environment.GetEnvironmentVariable("SCOPE")}");

            string bearerToken = GetBearerToken().GetAwaiter().GetResult();
            log.LogInformation($"HttpJobBuilder:Bearer Token:{bearerToken}");

            string url = System.Environment.GetEnvironmentVariable("JOBEXECUTOR2");
            log.LogInformation($"HttpJobBuilder:JOBEXECUTOR2:{url}");

            string res = TriggerJob(bearerToken,url).GetAwaiter().GetResult();
            log.LogInformation($"HttpJobBuilder:JobExecutor Output:{res}");

            return new OkObjectResult(res);
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
            string content = await response.Content.ReadAsStringAsync();
            return content;
        }    
    }
}

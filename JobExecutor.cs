using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;


namespace JayPaddy.Agent
{
    public static class JobExecutor
    {
        [FunctionName("JobExecutor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("JobExecutor:C# HTTP trigger function processed a request.");
            string token = req.Headers["Authorization"];
            //Remove the word Bearer if it exists
            token = token.Replace("Bearer ", "");
            log.LogInformation($"JobExecutor:Bearer:{token}");
            bool bAuth = ValidateBearer(token, log).GetAwaiter().GetResult();
            string responseMessage = bAuth
                ? "Valid Token"
                : "Invalid Token";
            log.LogInformation($"JobExecutor:{responseMessage}");
            if (bAuth)
            {
                return new OkObjectResult(responseMessage);
            }
            else{
                return new  UnauthorizedObjectResult(responseMessage);
            }
        }

        private static async Task<bool> ValidateBearer(string bearerToken, ILogger log)
        {
            string tenantID = System.Environment.GetEnvironmentVariable("TENANTID");
            string audience = System.Environment.GetEnvironmentVariable("AUDIENCE2");            
            string stsDiscoveryEndPoint = $"https://login.microsoftonline.com/{tenantID}/v2.0/.well-known/openid-configuration";
            ConfigurationManager<OpenIdConnectConfiguration> configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndPoint, new OpenIdConnectConfigurationRetriever());
            OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().Result;

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidAudience = audience,
                ValidIssuer = $"https://sts.windows.net/{tenantID}/",
                ValidateAudience = true,
                ValidateIssuer = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true
            };
            System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();
            SecurityToken jwt;
            try {
                var result = tokendHandler.ValidateToken(bearerToken, validationParameters, out jwt);
                return true;
            }
            catch (Exception ex)
            {
                
                log.LogInformation($"JobExecutor:ValidateToken exception:{ex.Message}");
                return false;
            }
 

        }
    }
}

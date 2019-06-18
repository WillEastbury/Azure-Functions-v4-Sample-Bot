using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SampleFunctionBot
{
    /// <summary>
    /// Handle Http Interactions with the bot via the http adapter and messages endpoint
    /// </summary>
    /// 

    public class BotHost
    {

        private readonly IBot bot;
        private readonly IBotFrameworkHttpAdapter bfhttp;
        private readonly HttpClient htc = new HttpClient();

        public BotHost(IBot _bot, IBotFrameworkHttpAdapter _bfhttp)
        {
            bot = _bot;
            bfhttp = _bfhttp;
        }

        /// <summary>
        /// Bot Messaging endpoint for Bot Framework to communicate with
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>

        [FunctionName("messages")]
        public async Task RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "messages")]
            HttpRequest req,
            ILogger log
           )
        {
            await bfhttp.ProcessAsync(req, req.HttpContext.Response, bot);
        }

        /// <summary>
        /// Endpoint for the V4 WebChat Control to get the Messaging token from
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>

        [FunctionName("directlinetoken")]
        public async Task<IActionResult> RunAsyncToken([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "directlinetoken")] HttpRequest req, ILogger log)
        {
            var pp = new PostPayload()
            {
                user = new User()
                {
                    id = "dl_" + Guid.NewGuid().ToString(),
                    name = "You"
                }
            };

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Post,
                Environment.GetEnvironmentVariable("DirectLineConversationEndpoint"));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                Environment.GetEnvironmentVariable("DirectLineSecret"));

            //
            request.Content = new StringContent(JsonConvert.SerializeObject(pp), Encoding.UTF8, "application/json");

            var response = await htc.SendAsync(request);
            var cnt = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                string token = JsonConvert.DeserializeObject<DirectLineToken>(cnt).token;

                var config = new ChatConfig() { Token = token, UserId = pp.user.id };

                return (IActionResult)new OkObjectResult(config);
            }
            else
            {

                throw new Exception("");
            }

        }


        /// <summary>
        /// Issue a token for the Cognitive endpoints
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>

        [FunctionName("cognitivetoken")]
        public async Task<IActionResult> RunAsyncCogToken([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "cognitivetoken")] HttpRequest req, ILogger log)
        {

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Post,
                Environment.GetEnvironmentVariable("CognitiveTokenEndpoint"));

            request.Headers.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("CognitiveSecret"));

            var response = await htc.SendAsync(request);

            return new OkObjectResult(new authToken() { token = await response.Content.ReadAsStringAsync()});

        }

        public class PostPayload
        {
            public User user { get; set; }
            //public List<string> trustedOrigins { get; set; }
        }

        public class authToken {

            public string token;
        }

        public class User
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class DirectLineToken
        {
            public string conversationId { get; set; }
            public string token { get; set; }
            public int expires_in { get; set; }
        }

        public class ChatConfig
        {
            public string Token { get; set; }
            public string UserId { get; set; }
            
        }
    }

}

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SampleFunctionBot
{
    public static class ServeHttpWebChatClient
    {
        [FunctionName("ServeHttpWebChatClient")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "chatclient")] HttpRequest req)
        {

            return (ActionResult) new FileStreamResult(File.OpenRead(Environment.GetEnvironmentVariable("IndexFile")), "text/html");

        }
    }
}

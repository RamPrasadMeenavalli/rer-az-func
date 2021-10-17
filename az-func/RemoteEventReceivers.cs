using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Xml.Linq;
using az_func.Common.Helpers;
using az_func.Common.EventReceivers;
using PnP.Framework;
using Microsoft.SharePoint.Client;

namespace az_func
{
    public static class ProcessItemEvents
    {
        [FunctionName("ProcessItemEvents")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var xdoc = XDocument.Parse(requestBody);
            var eventRoot = xdoc.Root.Descendants().First().Descendants().First();
            if (eventRoot.Name.LocalName != "ProcessEvent" && eventRoot.Name.LocalName != "ProcessOneWayEvent")
            {
                throw new Exception($"Unable to resolve event type");
            }

            var payload = eventRoot.FirstNode.ToString();
            var properties = SerializerHelper.Deserialize<SPRemoteEventProperties>(payload);

            var listId = properties.ItemEventProperties.ListId;
            var itemId = properties.ItemEventProperties.ListItemId;
            var webUrl = properties.ItemEventProperties.WebUrl;
            var clientId = Environment.GetEnvironmentVariable("ClientId");
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");

            var context = new AuthenticationManager()
            .GetACSAppOnlyContext(webUrl,clientId, clientSecret);

            if (eventRoot.Name.LocalName == "ProcessEvent")
            {
                try{
                // Process Synchronous Events
                // -ing events, i.e ItemAdding
                var result = new ProcessEventResponse {
                    ProcessEventResult = new SPRemoteEventResult {
                        Status = SPRemoteEventServiceStatus.Continue
                    }
                };                

                switch (properties.EventType)
                {
                    case SPRemoteEventType.ItemAdding:
                        {
                            // Example : If file name contains the word "reject",
                            // do not upload the file.
                            if(properties.ItemEventProperties.AfterUrl.Contains("reject")) {
                                result = new ProcessEventResponse
                                {
                                    ProcessEventResult = new SPRemoteEventResult{
                                        Status = SPRemoteEventServiceStatus.CancelWithError,
                                        ErrorMessage = "reject is not allowed in the File Name"
                                    }
                                };
                            }
                            break;
                        }

                    default: { break; }
                }

                var responseTemplate = @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
                                            <s:Body>{0}</s:Body>
                                        </s:Envelope>";
                var content = SerializerHelper.Serialize(result);

                return new ContentResult{
                    Content= String.Format(responseTemplate, content),
                    ContentType= "text/xml"
                };
            }catch(Exception ex){
                Console.WriteLine(ex.Message, ex.StackTrace);
            }
            }

            if (eventRoot.Name.LocalName == "ProcessOneWayEvent")
            {
                // Asynchronous events
                // -ed events, i.e. ItemAdded
                switch (properties.EventType)
                {
                    case SPRemoteEventType.ItemAdded:
                        {
                            // Example : Connect to SharePoint and update the item
                            // Or prform actions on an external system
                            var item = context.Web.Lists
                                .GetById(properties.ItemEventProperties.ListId)
                                .GetItemById(properties.ItemEventProperties.ListItemId);
                            item["Title"] = "Updated from Az Func RER!";
                            item.Update();
                            context.ExecuteQuery();
                            break;
                        }
                    default: { break; }
                }
                return new OkObjectResult("Done");
            }

            throw new Exception($"Unable to resolve event type");
        }
    }
}

using AppInsightExceptionsMailer.Mail.Interfaces;
using AppInsightExceptionsMailer.Mail.Pocos;
using AppInsightsReport.Mail.Pocos;
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AppInsightExceptionsMailer
{
    public class AppInsightsReportFunction
    {
        private readonly ApplicationInsightsDataClient _applicationInsightsDataClient;
        private readonly IConfiguration _configuration;
        private readonly IMailService _mailService;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<AppInsightsReportFunction> _logger;
        private readonly ApplicationInsightsSection _applicationInsightsSection;
        public AppInsightsReportFunction(ApplicationInsightsDataClient applicationInsightsDataClient, IConfiguration configuration, IHttpClientFactory clientFactory, IMailService mailService, ILogger<AppInsightsReportFunction> logger, IOptions<ApplicationInsightsSection> options)
        {
            _applicationInsightsDataClient = applicationInsightsDataClient;
            _configuration = configuration;
            _mailService = mailService;
            _clientFactory = clientFactory;
            _logger = logger;
            _applicationInsightsSection = options.Value;
        }

        [Function("AppInsightsReportFunction")]
        public async Task RunAsync([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation($"AppInsightSummaryFunction executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");

            await RunAppInsightQuery();
        }

        private async Task RunAppInsightQuery()
        {
            var data = new { query = _applicationInsightsSection.Query };

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("x-api-key", _applicationInsightsSection.ApiKey);

            var req = string.Format("{0}/{1}/query", _applicationInsightsSection.BaseUrl, _applicationInsightsSection.ApplicationId);
            var response = await client.PostAsync(req, data.AsJson());

            response.EnsureSuccessStatusCode();


            if (response.Content is object && response.Content.Headers.ContentType.MediaType == "application/json")
            {
                var contentStream = await response.Content.ReadAsStreamAsync();

                using var streamReader = new StreamReader(contentStream);
                using var jsonReader = new JsonTextReader(streamReader);

                JsonSerializer serializer = new JsonSerializer();
                var logs = serializer.Deserialize<Root>(jsonReader);
                await EmailLogs(logs);
            }
            else
            {
                _logger.LogError($"Response not in proper format", response);
            }

        }

        private async Task EmailLogs(Root logs)
        {
            var encodedQuery = await EncodedQuery(_applicationInsightsSection.Query);
            var linkToQuery = string.Format(_applicationInsightsSection.QueryUrl, _applicationInsightsSection.TenantId, _applicationInsightsSection.SubscriptionId
                , _applicationInsightsSection.ResourceGroup, _applicationInsightsSection.AppInsightsInstanceName, encodedQuery);

            foreach (var log in logs.tables[0].rows)
            {
                var mailRequest = new MailRequest()
                {
                    Subject = "An Exception occurred in cloud role instance - " + log[1],
                    Body = ToHtmlTable(log[0].ToString(), log[2].ToString(), log[3].ToString(), log[1].ToString()),
                    ToEmail = _applicationInsightsSection.CloudInstanceOwners.Values.ContainsKey(log[1].ToString()) ?
                                _applicationInsightsSection.CloudInstanceOwners.Values[log[1].ToString()] :
                                _applicationInsightsSection.CloudInstanceOwners.Values["default"]
                };
                await _mailService.SendEmailAsync(mailRequest);
            }

        }

        private async Task<string> EncodedQuery(string query)
        {
            var bytes = Encoding.UTF8.GetBytes(query);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream compressionStream = new System.IO.Compression.GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
                {
                    await compressionStream.WriteAsync(bytes, 0, bytes.Length);
                }
                memoryStream.Seek(0, SeekOrigin.Begin);
                byte[] data = memoryStream.ToArray();
                string encodedQuery = Convert.ToBase64String(data);
                return HttpUtility.UrlEncode(encodedQuery);
            }
        }

        private string ToHtmlTable(string name, string description, string count, string cloudRoleInstance)
        {
            string textBody = " <table border=" + 1 + " cellpadding=" + 0 + " cellspacing=" + 0 + " width = " + 400 + "><tr bgcolor='light grey'><td><b>Problem Id</b></td> <td> <b> Outer Message</b> </td><td> <b> Occurance (past 24 Hrs)</b> </td><td> <b> Cloud Role Instance </b> </td></tr>";
            textBody += "<tr><td>" + name + "</td><td> " + description + "</td><td>" + count + " time(s)</td><td>" + cloudRoleInstance + "</td></tr>";
            textBody += "</table>";
            textBody += "<br/> For more info - ";

            return textBody;
        }

    }

    public static class Extensions
    {
        public static StringContent AsJson(this object o)
            => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
    }

}

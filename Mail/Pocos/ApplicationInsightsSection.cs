using System.Collections.Generic;

namespace AppInsightsReport.Mail.Pocos
{
    public class ApplicationInsightsSection
    {
        public string InstrumentationKey { get; set; }
        public string RoleName { get; set; }
        public string BaseUrl { get; set; }
        public string ApplicationId { get; set; }
        public string ApiKey { get; set; }
        public string Query { get; set; }
        public string QueryUrl { get; set; }
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        public string AppInsightsInstanceName { get; set; }

        public CloudInstanceOwners CloudInstanceOwners { get; set; }
    }

    public class CloudInstanceOwners
    {
        public Dictionary<string, string> Values { get; set; }
    }
}

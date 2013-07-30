using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant.Azure
{
    /// <summary>
    /// Internal class that represents the data layout in Azure Storage
    /// </summary>
    internal class OAuth2AzureCredentialData : TableEntity
    {
        public OAuth2AzureCredentialData() { }
        public OAuth2AzureCredentialData(string consumerKey, string consumerSecret)
        {
            this.PartitionKey = consumerKey;
            this.RowKey = consumerSecret;
        }

        public string username { get; set; }

        public string propertiesJson
        {
            get
            {
                if (properties != null)
                {
                    return JsonConvert.SerializeObject(properties);
                }
                return null;
            }
            set
            {
                properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
            }
        }


        public Dictionary<string, string> properties { get; set; }
    }
}

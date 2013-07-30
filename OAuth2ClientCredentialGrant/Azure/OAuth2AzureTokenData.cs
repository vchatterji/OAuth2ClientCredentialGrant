using Microsoft.WindowsAzure.Storage.Table;
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
    internal class OAuth2AzureTokenData : TableEntity
    {
        public OAuth2AzureTokenData() { }

        /// <summary>
        /// Constructor that accepts the access token
        /// </summary>
        /// <param name="accessToken">The access token</param>
        public OAuth2AzureTokenData(string accessToken, DateTime validTill)
        {
            this.PartitionKey = accessToken;
            this.RowKey = accessToken;
            this.ValidTill = validTill;
        }

        /// <summary>
        /// The consumer key
        /// </summary>
        public string ConsumerKey
        {
            get;
            set;
        }

        /// <summary>
        /// The consumer secret
        /// </summary>
        public string ConsumerSecret
        {
            get;
            set;
        }

        
        /// <summary>
        /// Token validity
        /// </summary>
        public DateTime ValidTill { get; set; }
    }
}

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant.Azure
{
    /// <summary>
    /// Since we cannot retieve a token by consumer key and consumer secret efficiently 
    /// (the partition key and row key for the OAuthTokendata entity are 
    /// authtoken, authtoken respectively), we create this entity
    /// which uses a different PartitionKey. This entity is stored along
    /// with OAuthTokenData on every write of OAuthTokenData. It is also
    /// deleted whenever the corresponding OAuthTokenData entity is deleted
    /// </summary>
    internal class OAuth2TokenReference : TableEntity
    {
        /// <summary>
        /// Public constructor
        /// </summary>
        public OAuth2TokenReference()
        {
        }

        /// <summary>
        /// The access token. The rest of the properties are persisten in PartitionKey and RowKey respectively
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Constructor taking all parameters
        /// </summary>
        /// <param name="consumerKey">The consumer key (this is stored as the PartitionKey)</param>
        /// <param name="consumerSecret">The consumer secret (this is stored as the RowKey)</param>
        /// <param name="accessToken">The access token</param>
        internal OAuth2TokenReference(string consumerKey, string consumerSecret, string accessToken)
        {
            this.PartitionKey = consumerKey;
            this.RowKey = consumerSecret;
            this.AccessToken = accessToken;
        }
    }
}

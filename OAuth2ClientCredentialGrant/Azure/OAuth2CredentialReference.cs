using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant.Azure
{
    /// <summary>
    /// Since we cannot retieve a credential by username efficiently 
    /// (the partition key and row key for the OAuthCredentialData entity are 
    /// consumer key, consumer secret respectively), we create this entity
    /// which uses a different PartitionKey. This entity is stored along
    /// with OAuthTokenData on every write of OAuthCredentialData. It is also
    /// deleted whenever the corresponding OAuthCredentialData entity is deleted
    /// </summary>
    internal class OAuth2CredentialReference : TableEntity
    {
        /// <summary>
        /// Public constructor
        /// </summary>
        public OAuth2CredentialReference()
        {
        }

        /// <summary>
        /// Constructor with all the parameters
        /// </summary>
        /// <param name="username">The username (this is stored as the PartitionKey</param>
        /// <param name="consumer_Key">The consumer key (this is stored as the RowKey)</param>
        /// <param name="consumer_Secret">The consumer secret</param>
        public OAuth2CredentialReference(string username, string consumer_Key, string consumer_Secret)
        {
            this.PartitionKey = username;
            this.RowKey = consumer_Key;
            this.consumer_Secret = consumer_Secret;
        }

        /// <summary>
        /// The consumer secret. The rest of the properties are persisten in PartitionKey and RowKey respectively
        /// </summary>
        public string consumer_Secret { get; set; }
    }
}

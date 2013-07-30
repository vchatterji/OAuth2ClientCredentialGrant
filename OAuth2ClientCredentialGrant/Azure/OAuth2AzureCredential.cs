using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant.Azure
{
    /// <summary>
    /// Concrete derived class for OAuth2Credential
    /// </summary>
    internal class OAuth2AzureCredential : OAuth2Credential
    {
        private OAuth2AzureCredentialData credential;

        /// <summary>
        /// Constructor taking the underlying TableEntity object that is read/written in Azure Storage
        /// </summary>
        /// <param name="credential"></param>
        internal OAuth2AzureCredential(OAuth2AzureCredentialData credential)
        {
            this.credential = credential;
        }

        /// <summary>
        /// The consumer key for this credential
        /// </summary>
        public override string ConsumerKey
        {
            get { return credential.PartitionKey; }
            set { credential.PartitionKey = value; }
        }


        /// <summary>
        /// The consumer secret for this credential
        /// </summary>
        public override string ConsumerSecret
        {
            get { return credential.RowKey; }
            set { credential.RowKey = value; }
        }

        /// <summary>
        /// The username for this credential
        /// </summary>
        public override string Username
        {
            get { return credential.username; }
            set { credential.username = value; }
        }

        /// <summary>
        /// The name, value properties for this credential
        /// </summary>
        public override Dictionary<string, string> Properties
        {
            get { return credential.properties; }
            set { credential.properties = value; }
        }

        /// <summary>
        /// The underlying TableEntity that is read/written from Azure Storage
        /// </summary>
        public override object CredentialData
        {
            get { return credential; }
        }
    }
}

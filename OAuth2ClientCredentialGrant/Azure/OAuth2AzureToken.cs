using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant.Azure
{
    /// <summary>
    /// Concrete implementation of an OAuth2Token
    /// </summary>
    public class OAuth2AzureToken : OAuth2Token
    {
        private OAuth2AzureTokenData token;

        /// <summary>
        /// Constructor taking the underlying TableEntity object that is read/written in Azure Storage
        /// </summary>
        /// <param name="token">The underlying TableEntity object</param>
        /// <param name="username">The username</param>
        internal OAuth2AzureToken(OAuth2AzureTokenData token, string username)
        {
            this.token = token;
            this.Username = username;
        }

        /// <summary>
        /// The access token
        /// </summary>
        public override string AccessToken
        {
            get
            {
                return token.PartitionKey;
            }
            set
            {
                token.PartitionKey = value;
                token.RowKey = value;
            }
        }

        /// <summary>
        /// Token validity (UTC)
        /// </summary>
        public override DateTime ValidTill
        {
            get
            {
                return token.ValidTill;
            }
            set
            {
                token.ValidTill = value;
            }
        }

        /// <summary>
        /// The underlying TableEntity that is read/written from Azure Storage
        /// </summary>
        public override object TokenData
        {
            get { return token; }
        }

        /// <summary>
        /// The consumer key
        /// </summary>
        public override string ConsumerKey
        {
            get
            {
                return token.ConsumerKey;
            }
            set
            {
                token.ConsumerKey = value;
            }
        }

        /// <summary>
        /// The consumer secret
        /// </summary>
        public override string ConsumerSecret
        {
            get
            {
                return token.ConsumerSecret;
            }
            set
            {
                token.ConsumerSecret = value;
            }
        }

        public override string Username
        {
            get;
            set;
        }
    }
}

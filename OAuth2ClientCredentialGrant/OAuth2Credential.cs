using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant
{
    /// <summary>
    /// Abstract class representing an OAuth2 credential
    /// </summary>
    public abstract class OAuth2Credential
    {
        /// <summary>
        /// The consumer key for this credential
        /// </summary>
        public abstract string ConsumerKey
        {
            get;
            set;
        }

        /// <summary>
        /// The consumer secret for this credential
        /// </summary>
        public abstract string ConsumerSecret
        {
            get;
            set;
        }

        /// <summary>
        /// The username for this credential
        /// </summary>
        public abstract string Username
        {
            get;
            set;
        }

        /// <summary>
        /// The name, value properties for this credential
        /// </summary>
        public abstract Dictionary<string, string> Properties
        {
            get;
            set;
        }

        /// <summary>
        /// The underlying storage specific object if any
        /// </summary>
        public abstract object CredentialData
        {
            get;
        }
    }
}

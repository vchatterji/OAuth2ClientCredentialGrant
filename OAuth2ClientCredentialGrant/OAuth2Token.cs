using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant
{
    /// <summary>
    /// Abstract class representing a OAuth2 Token
    /// </summary>
    public abstract class OAuth2Token
    {
        /// <summary>
        /// The token
        /// </summary>
        public abstract string AccessToken
        {
            get;
            set;
        }

        /// <summary>
        /// The consumer key for which this token was issued
        /// </summary>
        public abstract string ConsumerKey { get; set; }
        
        /// <summary>
        /// The consumer secret for which this token was issued
        /// </summary>
        public abstract string ConsumerSecret { get; set; }
        
        /// <summary>
        /// Validity of this token
        /// </summary>
        public abstract DateTime ValidTill { get; set; }

        /// <summary>
        /// The underlying storage specific object if any
        /// </summary>
        public abstract object TokenData
        {
            get;
        }

        /// <summary>
        /// The username associated with this token
        /// </summary>
        public abstract string Username
        {
            get;
            set;
        }
    }
}

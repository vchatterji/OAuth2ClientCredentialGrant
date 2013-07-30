using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant
{
    /// <summary>
    /// The interface for a credential manager
    /// </summary>
    public abstract class OAuth2CredentialManager
    {
        /// <summary>
        /// Validate the credentials
        /// </summary>
        /// <param name="consumer_Key">The consumer key</param>
        /// <param name="consumer_Secret">The consumer secret</param>
        /// <returns>OAuth2Credential that contains username and a dictionary of name value properties</returns>
        public abstract OAuth2Credential GetCredential(string consumer_Key, string consumer_Secret);

        /// <summary>
        /// Sets the property collection for the credential specified by consumer key and consumer secret.
        /// Returns false if the operation fails or credentals don't already exist.
        /// </summary>
        /// <param name="consumer_key">The consumer key of the credential</param>
        /// <param name="consumer_Secret">The consumer secret for the credential</param>
        /// <param name="properties">The properties collection to store against the credential</param>
        public abstract bool SetProperties(string consumer_key, string consumer_Secret, Dictionary<string, string> properties);

        /// <summary>
        /// Creates a credential for the supplied username and with the specified properties
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="properties">The properties</param>
        /// <returns>The credential that was created</returns>
        public abstract OAuth2Credential CreateCredential(string username, Dictionary<string, string> properties);

        /// <summary>
        /// Get  a list of current OAuth2 credentials for the supplied username
        /// </summary>
        /// <param name="username">The username</param>
        /// <returns>The credentials that exist for the username</returns>
        public abstract List<OAuth2Credential> GetCredentials(string username);

        /// <summary>
        /// Deletes the specified credential for the specified username
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="consumer_Key">The consumer key to delete</param>
        public abstract bool DeleteCredential(string consumer_Key, string consumer_Secret);
    }
}

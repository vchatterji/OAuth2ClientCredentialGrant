using OAuth2ClientCredentialsGrant.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant
{
    /// <summary>
    /// This class holds singleton instances of ITokenManager and ICredential manager.
    /// The instances are created based on the OAuth2DataStoreType key specified in your web.config
    /// Currently only an Azure storage is implemented and the default value for the key is 1
    /// </summary>
    public class OAuth2DataFactory
    {
        /// <summary>
        /// Static constructor retrieves factory type from config.
        /// </summary>
        static OAuth2DataFactory()
        {
            factoryType = (FactoryType) OAuth2Config.OAuth2DataStoreType;
        }

        /// <summary>
        /// Type of ITokenManager and ICredentialManager
        /// </summary>
        public enum FactoryType
        {
            None = 0,
            AzureStorage
        }

        private static OAuth2TokenManager tokenManager = null;
        private static OAuth2CredentialManager credentialManager = null;
        private static FactoryType factoryType = FactoryType.None;


        /// <summary>
        /// Returns an ITokenManager instance based on the type set in the config
        /// </summary>
        internal static OAuth2TokenManager TokenManager
        {
            get
            {
                if (tokenManager == null)
                { 
                    switch (factoryType)
                    {
                        case FactoryType.None:
                            return null;
                        case FactoryType.AzureStorage:
                            tokenManager = new OAuth2AzureStorageTokenManager();
                            break;
                    }
                }
                return tokenManager;
            }
        }

        /// <summary>
        /// Returns an ICredentialManager instance based on the type set in the config
        /// </summary>
        public static OAuth2CredentialManager CredentialManager
        {
            get
            {
                if (credentialManager == null)
                {
                    switch (factoryType)
                    {
                        case FactoryType.None:
                            return null;
                        case FactoryType.AzureStorage:
                            credentialManager = new OAuth2AzureStorageCredentialManager();
                            break;
                    }
                }
                return credentialManager;
            }
        }
    }
}

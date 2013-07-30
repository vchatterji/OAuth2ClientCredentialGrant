using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant
{
    /// <summary>
    /// Retrieves configuration values using the ConfigurationManager
    /// These values would be set in the web.config
    /// </summary>
    public class OAuth2Config
    {
        /// <summary>
        /// The OAuth2Token is valid for the specified number of minutes
        /// </summary>
        public static int OAuth2TokenValidityMinutes
        {
            get
            {
                int defaultVal = 30;
                try
                {
                    return int.Parse(ConfigurationManager.AppSettings["OAuth2TokenValidityMinutes"]);
                }
                catch (Exception) { }
                return defaultVal;
            }
        }

        /// <summary>
        /// The OAuth2Token is locally cached for the specified number of minutes
        /// After the specified minutes, it will again be retrieved from underlying storage
        /// and validated
        /// </summary>
        public static int OAuth2TokenLocalCacheValidityMinutes
        {
            get
            {
                int defaultVal = 5;
                try
                {
                    return int.Parse(ConfigurationManager.AppSettings["OAuth2TokenLocalCacheValidityMinutes"]);
                }
                catch (Exception) { }
                return defaultVal;
            }
        }

        /// <summary>
        /// The underlying data store type to use to store credential and token data
        /// </summary>
        public static int OAuth2DataStoreType
        {
            get
            {
                int defaultVal = 1;
                try
                {
                    return int.Parse(ConfigurationManager.AppSettings["OAuth2DataStoreType"]);
                }
                catch (Exception) { }
                return defaultVal;
            }
        }

        /// <summary>
        /// The name of the connection string in your web.config which should be used to connect to the underlying data store
        /// </summary>
        public static string OAuth2ConnectionStringName
        {
            get
            {
                return ConfigurationManager.AppSettings["OAuth2ConnectionStringName"];
            }
        }

        /// <summary>
        /// If this property is set to true, all incoming requests passing through the message handler must be over SSL.
        /// By default this is True. You can set this to False in your web.config appSettings to enable debugging on your
        /// local machine. This parameter should always be set to True in production.
        /// </summary>
        public static bool OAuth2RequireSsl
        {
            get
            {
                bool isSslRequired = true;
                if (!bool.TryParse(ConfigurationManager.AppSettings["OAuth2RequireSsl"], out isSslRequired))
                    isSslRequired = true;
                return isSslRequired;
            }
        }
    }
}

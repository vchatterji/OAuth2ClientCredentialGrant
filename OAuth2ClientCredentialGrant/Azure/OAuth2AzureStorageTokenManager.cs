using log4net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant.Azure
{
    /// <summary>
    /// Implements a token manager with Azure Storage as its underlying store
    /// </summary>
    public class OAuth2AzureStorageTokenManager : OAuth2TokenManager
    {
        public string connectionString = null;
        const string tokenTableName = "OAuth2Token";

        CloudStorageAccount storageAccount;
        CloudTableClient tableClient;
        CloudTable table;

        private static ILog logObj = null;

        /// <summary>
        /// Log4Net logger to log error events
        /// </summary>
        public static ILog Logger
        {
            get
            {
                if (logObj == null)
                    logObj = log4net.LogManager.GetLogger("AzureStorageTokenManager");
                return logObj;
            }
            set { logObj = value; }
        }

        /// <summary>
        /// Constructor. Creates table in Azure Storage if it doesn't exist already
        /// </summary>
        public OAuth2AzureStorageTokenManager()
        {
            if (!string.IsNullOrEmpty(OAuth2Config.OAuth2ConnectionStringName))
            {
                connectionString = System.Configuration.ConfigurationManager.ConnectionStrings[OAuth2Config.OAuth2ConnectionStringName].ConnectionString;
            }

            //Initialize tables they don't exist
            if (connectionString != null)
            {
                storageAccount = CloudStorageAccount.Parse(connectionString);
                tableClient = storageAccount.CreateCloudTableClient();
                table = tableClient.GetTableReference(tokenTableName);
                table.CreateIfNotExists();
            }
        }

        /// <summary>
        /// Gets the specified token. If the token is expired, this method will delete it from the storage
        /// </summary>
        /// <param name="accessToken">The access token to get</param>
        /// <returns>Returns the token if it is found. Otherwise returns null</returns>
        protected override OAuth2Token GetTokenFromStore(string accessToken)
        {
            try
            {
                TableOperation op = TableOperation.Retrieve<OAuth2AzureTokenData>(accessToken, accessToken);
                TableResult result = table.Execute(op);

                OAuth2AzureTokenData token = result.Result as OAuth2AzureTokenData;

                if (token == null)
                    return null;

                if (token.ValidTill >= DateTime.UtcNow)
                {
                    OAuth2Credential check = OAuth2DataFactory.CredentialManager.GetCredential(token.ConsumerKey, token.ConsumerSecret);
                    if (check == null)
                    {
                        InvalidateToken(accessToken);
                        return null;
                    }
                    //Increase the validity of the token
                    token.ValidTill = DateTime.UtcNow.AddMinutes(OAuth2Config.OAuth2TokenValidityMinutes);
                    TableOperation saveOp = TableOperation.InsertOrReplace(token);

                    table.Execute(saveOp);
                    return new OAuth2AzureToken(token, check.Username);

                }
                else
                {
                    InvalidateToken(accessToken);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ValidateToken Failed", ex);
                return null;
            }
        }

        /// <summary>
        /// Writes the specified token to Azure Storage
        /// </summary>
        /// <param name="token">The token to save</param>
        /// <returns></returns>
        protected override bool SaveTokenToStore(OAuth2Token token)
        {
            try
            {
                TableOperation op = TableOperation.InsertOrReplace((OAuth2AzureTokenData)token.TokenData);
                table.Execute(op);

                OAuth2TokenReference reference = new OAuth2TokenReference(token.ConsumerKey, token.ConsumerSecret, token.AccessToken);
                TableOperation op2 = TableOperation.InsertOrReplace(reference);
                table.Execute(op2);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("SaveToken Failed", ex);
                return false;
            }
        }

        /// <summary>
        /// Invalidates the specified token and removes it from the store
        /// </summary>
        /// <param name="accessToken">The access token to invalidate</param>
        /// <returns>The token deleted. The Username property will be null in this object</returns>
        protected override OAuth2Token InvalidateTokenInStore(string accessToken)
        {
            return InvalidateToken(accessToken, (OAuth2TokenReference)null);
        }

        /// <summary>
        /// Private method that invalidates the specified token and uses the OAuth2TokenReference 
        /// if specified (instead of loading it from the store)
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="reference"></param>
        /// <returns>The token deleted. The Username property will be null in this object</returns>
        private OAuth2AzureToken InvalidateToken(string accessToken, OAuth2TokenReference reference)
        {
            try
            {
                TableOperation op = TableOperation.Retrieve<OAuth2AzureTokenData>(accessToken, accessToken);
                TableResult result = table.Execute(op);

                OAuth2AzureTokenData token = result.Result as OAuth2AzureTokenData;

                if (token == null)
                    return null;

                string consumerKey = token.ConsumerKey;
                string consumerSecret = token.ConsumerSecret;

                TableOperation delOp = TableOperation.Delete(token);
                table.Execute(delOp);


                if (reference == null)
                {
                    op = TableOperation.Retrieve<OAuth2TokenReference>(consumerKey, consumerSecret);
                    table.Execute(op);
                    reference = result.Result as OAuth2TokenReference;
                }

                string username = null;
                if (reference != null)
                {
                    username = reference.PartitionKey;
                    delOp = TableOperation.Delete(reference);
                    table.Execute(delOp);
                }

                return new OAuth2AzureToken(token, null);
            }
            catch (Exception ex)
            {
                Logger.Error("InvalidateToken Failed", ex);
                return null;
            }
        }

        /// <summary>
        /// Creates or retrieves an OAuth2Token object based on the specified parameters.
        /// The object is not stored in Azure until a call is made to SaveToken. 
        /// If this method returns isNew = true, then caller must make sure SaveToken is called
        /// to persist the token in Azure Storage
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <param name="isNew">Out parameter which indicates if the token was retrieved from store (isNew = false).
        /// If the token was not retrieved from the store, the caller must persist the token to the store by calling
        /// SaveToken()
        /// </param>
        /// <returns></returns>
        protected override OAuth2Token CreateTokenInStore(string username, string consumerKey, string consumerSecret, out bool isNew)
        {
            try
            {
                isNew = false;
                TableOperation op = TableOperation.Retrieve<OAuth2TokenReference>(consumerKey, consumerSecret);
                TableResult result = table.Execute(op);

                OAuth2TokenReference token = result.Result as OAuth2TokenReference;

                if (token != null)
                {
                    if (GetToken(token.AccessToken) != null)
                    {
                        OAuth2AzureTokenData data = new OAuth2AzureTokenData(token.AccessToken, DateTime.UtcNow.AddMinutes(OAuth2Config.OAuth2TokenValidityMinutes));
                        data.ConsumerKey = consumerKey;
                        data.ConsumerSecret = consumerSecret;
                        return new OAuth2AzureToken(data, username);
                    }
                }

                isNew = true;
                string rtoken = Guid.NewGuid().ToString();
                string etoken = Convert.ToBase64String(Encoding.UTF8.GetBytes(rtoken));
                OAuth2AzureTokenData data2 = new OAuth2AzureTokenData(etoken, DateTime.UtcNow.AddMinutes(OAuth2Config.OAuth2TokenValidityMinutes));
                data2.ConsumerKey = consumerKey;
                data2.ConsumerSecret = consumerSecret;
                return new OAuth2AzureToken(data2, username);
            }
            catch (Exception ex)
            {
                Logger.Error("CreateToken Failed", ex);
                isNew = false;
                return null;
            }
        }


        /// <summary>
        /// Invalidates the token for the specified consumer key, consumer secret pair and removes it from the store
        /// </summary>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <returns></returns>
        protected override OAuth2Token InvalidateTokenInStore(string consumerKey, string consumerSecret)
        {
            TableOperation op = TableOperation.Retrieve<OAuth2TokenReference>(consumerKey, consumerSecret);
            TableResult result = table.Execute(op);


            OAuth2TokenReference reference = result.Result as OAuth2TokenReference;
            if (reference == null)
                return null;

            string accessToken = reference.AccessToken;
            return InvalidateToken(accessToken, reference);

        }
    }
}

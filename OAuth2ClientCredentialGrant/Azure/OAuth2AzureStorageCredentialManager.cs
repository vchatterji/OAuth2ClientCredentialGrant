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
    /// Implements a credential manager with Azure Storage as its underlying store
    /// </summary>
    public class OAuth2AzureStorageCredentialManager : OAuth2CredentialManager
    {
        public string connectionString = null;
        const string credentialTableName = "OAuth2Credential";

        CloudStorageAccount storageAccount;
        CloudTableClient tableClient;
        CloudTable table;

        private static ILog logObj = null;

        /// <summary>
        /// Log4Net logger to log error events
        /// </summary>
        private static ILog Logger
        {
            get
            {
                if (logObj == null)
                    logObj = log4net.LogManager.GetLogger("AzureStorageCredentialManager");
                return logObj;
            }
            set { logObj = value; }
        }

        /// <summary>
        /// Constructor. Creates table in Azure Storage if it doesn't exist already
        /// </summary>
        public OAuth2AzureStorageCredentialManager()
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
                table = tableClient.GetTableReference(credentialTableName);
                table.CreateIfNotExists();
            }
        }

        /// <summary>
        /// Gets the OAuth2Crdential object based on consumer key and consumer secret.
        /// Every consumer key, consumer secret is attached to a particular username
        /// </summary>
        /// <param name="consumer_Key">The consumer key</param>
        /// <param name="consumer_Secret">The consumer secret</param>
        /// <returns></returns>
        public override OAuth2Credential GetCredential(string consumer_Key, string consumer_Secret)
        {
            TableOperation op = TableOperation.Retrieve<OAuth2AzureCredentialData>(consumer_Key, consumer_Secret);
            TableResult result = table.Execute(op);

            OAuth2AzureCredentialData credential = result.Result as OAuth2AzureCredentialData;
            if (credential == null)
                return null;

            return new OAuth2AzureCredential(credential);
        }

        /// <summary>
        /// Creates a new consumer key and consumer secret pair for the specified username.
        /// You may also store extra name value pairs as credential properties which can be accessed when the credential is retrieved.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public override OAuth2Credential CreateCredential(string username, Dictionary<string, string> properties)
        {
            string gKey = Guid.NewGuid().ToString();
            string consumer_Key = Convert.ToBase64String(Encoding.UTF8.GetBytes(gKey));
            string gSecret = Guid.NewGuid().ToString();
            string consumer_Secret = Convert.ToBase64String(Encoding.UTF8.GetBytes(gSecret));
            OAuth2AzureCredentialData credential = new OAuth2AzureCredentialData(consumer_Key, consumer_Secret);
            credential.username = username;
            credential.properties = properties;

            OAuth2CredentialReference reference = new OAuth2CredentialReference(username, consumer_Key, consumer_Secret);
            try
            {
                TableOperation op = TableOperation.InsertOrReplace(credential);
                table.Execute(op);

                TableOperation op2 = TableOperation.InsertOrReplace(reference);
                table.Execute(op2);
                return new OAuth2AzureCredential(credential);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all consumer key, consumer secret pairs for the specified username. More than one pair may be created for a particular username
        /// </summary>
        /// <param name="username">The username</param>
        /// <returns></returns>
        public override List<OAuth2Credential> GetCredentials(string username)
        {
            try
            {
                List<OAuth2Credential> credentials = new List<OAuth2Credential>();

                TableQuery<OAuth2CredentialReference> query = new TableQuery<OAuth2CredentialReference>()
                 .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, username));

                foreach (OAuth2CredentialReference reference in table.ExecuteQuery(query))
                {
                    credentials.Add(GetCredential(reference.RowKey, reference.consumer_Secret));
                }

                return credentials;
            }
            catch (Exception)
            {
                return new List<OAuth2Credential>();
            }
        }

        /// <summary>
        /// Deletes the specified consumer key, consumer secret pair from the store. It also removes any tokens that may have been issued
        /// using the specified credenial
        /// </summary>
        /// <param name="consumer_Key">The consumer key</param>
        /// <param name="consumer_Secret">The consumer secret</param>
        /// <returns></returns>
        public override bool DeleteCredential(string consumer_Key, string consumer_Secret)
        {
            try
            {
                TableOperation op = TableOperation.Retrieve<OAuth2AzureCredentialData>(consumer_Key, consumer_Secret);

                TableResult result = table.Execute(op);

                OAuth2AzureCredentialData credential = result.Result as OAuth2AzureCredentialData;

                if (credential != null)
                {
                    TableOperation op2 = TableOperation.Retrieve<OAuth2CredentialReference>(credential.username, credential.PartitionKey);
                    TableResult result2 = table.Execute(op2);

                    OAuth2CredentialReference credentialRef = result2.Result as OAuth2CredentialReference;
                    if (credentialRef != null)
                    {
                        TableOperation delOp = TableOperation.Delete(credentialRef);
                        table.Execute(delOp);
                    }

                    TableOperation delOp2 = TableOperation.Delete(credential);
                    table.Execute(delOp2);

                    return OAuth2DataFactory.TokenManager.InvalidateToken(consumer_Key, consumer_Secret) != null;
                }
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the property collection of name value pairs for the credential specified by the specified consumer key
        /// and secret.
        /// </summary>
        /// <param name="consumer_Key">The consumer key</param>
        /// <param name="consumer_Secret">The consumer secret</param>
        /// <param name="properties">The properties to set</param>
        /// <returns></returns>
        public override bool SetProperties(string consumer_Key, string consumer_Secret, Dictionary<string, string> properties)
        {
            TableOperation op = TableOperation.Retrieve<OAuth2AzureCredentialData>(consumer_Key, consumer_Secret);
            TableResult result = table.Execute(op);

            OAuth2AzureCredentialData currentCredential = result.Result as OAuth2AzureCredentialData;
            if (currentCredential == null)
                return false;

            currentCredential.properties = properties;

            TableOperation op2 = TableOperation.InsertOrReplace(currentCredential);
            TableResult result2 = table.Execute(op2);

            OAuth2AzureCredentialData credential = result2.Result as OAuth2AzureCredentialData;
            if (credential == null)
                return false;

            return true;
        }
    }
}

OAuth2ClientCredentialGrant
===========================

A library that allows you to secure REST based Web Api projects that use WebMatrix.WebData.SimpleMembershipProvider as their MembershipProvider

This project works under the following conditions:
1. You are using .NET Framework v4.5 for all you projects
2. You are using WebMatrix.WebData.SimpleMembershipProvider as your MembershipProvider in your WebApi project.
   To enable this, your web.config must have the following entries:
	<system.web>
		<compilation debug="true" targetFramework="4.5"/>
		<roleManager enabled="true" defaultProvider="SimpleRoleProvider">
			<providers>
				<clear />
				<add name="SimpleRoleProvider" type="WebMatrix.WebData.SimpleRoleProvider, WebMatrix.WebData" />
			</providers>
		</roleManager>
		<membership defaultProvider="SimpleMembershipProvider">
			<providers>
				<clear />
				<add name="SimpleMembershipProvider" type="WebMatrix.WebData.SimpleMembershipProvider, WebMatrix.WebData" />
			</providers>
		</membership>
	<sytem.web>
3. You have a connection strings as shown below:
	<connectionStrings>
		<add name="DefaultConnection" connectionString="Your SQL connection string" providerName="System.Data.SqlClient" />
		<add name="StorageConnectionString" connectionString="DefaultEndpointsProtocol=https;AccountName=Your azure storage account name;AccountKey=Your Azure storage account key" />
	</connectionStrings>
 4. You have the following appsettings in web.config:
	<appSettings>
        <!-- The below value gives the name of the connection string configured earlier -->
        <add key="OAuth2ConnectionStringName" value="StorageConnectionString" />
        <!-- If set to true, the below key will ensure that calls to your API come over SSL. Set to true on production systems -->
        <add key="OAuth2RequireSsl" value="False"/>
   </appSettings>
5. Make sure that the following DOES NOT EXIST in your web.config:
			<dependentAssembly>
				<assemblyIdentity name="System.Net.Http" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
				<bindingRedirect oldVersion="0.0.0.0-2.0.0.0" newVersion="2.0.0.0" />
			</dependentAssembly>
6. Follow the FAQ below for more details:

FAQ
===

PUT and DELETE verbs don't work! How can I enable them?
If you are using IIS8.0, you need to disable WebDAV. You can do this by changing the web.config to remove the handler and module:
<system.webServer>
	<modules runAllManagedModulesForAllRequests="true">
		<remove name="WebDAVModule" />
	</modules>
    <handlers>
		<remove name="WebDAV" />
	</handlers>
</system.webServer>

How do I configure Authentication?

You only need to add the handler for your Web API route. For example, if your Web API route is in: 
\App_Start\WebApiConfig.cs

Then the file should look like:
public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
			//Since our handler uses WebSecurity, we initialize it here, otherwise it may cause errors in it is accessed before it is initialized
		    try
            {
                WebSecurity.InitializeDatabaseConnection("DefaultConnection", "UserProfile", "UserId", "UserName", autoCreateTables: true);
            }
            catch(Exception)
            {
            }
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional },
                constraints: null,
                handler: HttpClientFactory.CreatePipeline(
                          new HttpControllerDispatcher(config),
                          new DelegatingHandler[] { new OAuth2ClientCredentialsHandler() })
            );

            //Use the below lines to return JSON from your API instead of the default XML
            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);
        }
    }

--------------------

Where will this component store credentials and how do I set that up?

This component uses Azure Storage Tables to store its data. To configure it to use Azure Storage, you need to add a connection string for Azure
in your web.config file. You can then add a configuration setting pointing to the name of the configuration string. This is shown below: 

<configuration>
    <connectionStrings>
        <add name="StorageConnectionString" connectionString="DefaultEndpointsProtocol=https;AccountName=your_account;AccountKey=your_key" />
    </connectionStrings>
    <appSettings>
        <!-- The below value gives the name of the connection string configured earlier -->
        <add key="OAuth2ConnectionStringName" value="StorageConnectionString" />
        <!-- If set to true, the below key will ensure that calls to your API come over SSL -->
        <add key="OAuth2RequireSsl" value="False"/>
   </appSettings>
</configuration>

-------------------

How do I generate consumer keys and secrets for my API?

You can use the following method to generate a consumer key and secret pair for your API. The username is the username on your site you are
generating API credentials for. You can also specify a Dictionary of name value pairs that will be stored along with the credentials.

OAuth2DataFactory.CredentialManager.CreateCredential(string username, Dictionary<string, string> properties);

-------------------

How do I get/retrieve/delete credentials?

You can use the following methods to manipulate credentials:

//Retrieve a credential based on consumer secret and consumer key. The credential object will contain the properties used while creating the credential.
OAuth2DataFactory.CredentialManager.GetCredential(string consumer_Key, string consumer_Secret);

//Retrieve all the credentials for a particular user (a user can have multiple consumer key/consumer secret pairs which can be deleted if any of them
//is compromised)
OAuth2DataFactory.CredentialManager.GetCredentials(string username);

//Delete a compromised credential
OAuth2DataFactory.CredentialManager.DeleteCredential(string consumer_Key, string consumer_Secret);

------------------

How do I set the properties name value pairs for a credential?

You can use the following method to set the property name value pairs for a credential:
OAuth2DataFactory.CredentialManager.SetProperties(string consumer_Key, string consumer_Secret, Dictionary<string, string> properties);

------------------

How do I see the username of the user who is accessing my protected API?

You can use:
Thread.CurrentPrincipal.Identity.Name in your WebAPI methods to find the username for the user accessing your API.

------------------

How do I acess the properties stored against the credential in my protected Web API method?

You can retrieve the credential in your Web API method using:

string consumerKey = (string) Request.Properties["ConsumerKey"];
string consumerSecret = (string) Request.Properties["ConsumerSecret"];
OAuth2Credential credential = OAuth2ClientCredentialsGrant.OAuth2DataFactory.CredentialManager.GetCredential(consumerKey, consumerSecret);

The credential object thus retrieved will have the properties. If a property changes, you can change it in the object and persist it back to the store using:
OAuth2DataFactory.CredentialManager.SetProperties(consumerKey , consumerSecret , credential.Properties);

------------------

How do I make API calls to my secured API?

You can use the OAuth2Client  to make calls to your secured API.


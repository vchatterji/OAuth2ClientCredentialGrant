using OAuth2ClientCredentialsGrant;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Change this to your own URL pointing to where the WebApiDemo project lives
            string sampleUrl = "http://localhost:8287/api";

            //The following username is who you are creating the credential for
            string username = "varun.chatterji@gmail.com";
            Dictionary<string, string> properties = new Dictionary<string,string>();
            properties.Add("name", "Varun Chatterji");

            Console.WriteLine(string.Format("Creating credentials for {0}", username));
            OAuth2Credential credential = OAuth2DataFactory.CredentialManager.CreateCredential(username, properties);

            Console.WriteLine("Credentials are:");
            Console.WriteLine(string.Format("Consumer Key: {0}", credential.ConsumerKey));
            Console.WriteLine(string.Format("Consumer Secret {0}", credential.ConsumerSecret));

            Console.WriteLine("Calling protected API");
            OAuth2CredentialsGrantClient client = new OAuth2CredentialsGrantClient(credential.ConsumerKey, credential.ConsumerSecret, sampleUrl, false);
            OAuth2ClientCredentialsGrantResponse response = client.Get("sample");
            using (response.response)
            {
                StreamReader sr = new StreamReader(response.response.GetResponseStream());
                string responseText = sr.ReadToEnd();
                Console.WriteLine("Response was:");
                Console.WriteLine(responseText);
            }

            Console.WriteLine("Deleting credential...");
            OAuth2DataFactory.CredentialManager.DeleteCredential(credential.ConsumerKey, credential.ConsumerSecret);

            Console.WriteLine("Press any key to continue..");
            Console.ReadKey();
        }
    }
}

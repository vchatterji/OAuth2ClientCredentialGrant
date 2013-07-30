using OAuth2ClientCredentialsGrant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;

namespace WebApiDemo.Api
{
    public class GetApiResponse
    {
        public string username;
        public Dictionary<string, string> properties;
    }

    public class SampleController : ApiController
    {
        // GET api/<controller>
        public GetApiResponse Get()
        {
            GetApiResponse response = new GetApiResponse();
            response.username = Thread.CurrentPrincipal.Identity.Name;

            string consumerKey = (string)Request.Properties["ConsumerKey"];
            string consumerSecret = (string)Request.Properties["ConsumerSecret"];
            OAuth2Credential credential = OAuth2ClientCredentialsGrant.OAuth2DataFactory.CredentialManager.GetCredential(consumerKey, consumerSecret);

            response.properties = credential.Properties;
            return response;
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}
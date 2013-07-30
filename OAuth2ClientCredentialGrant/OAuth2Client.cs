using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace OAuth2ClientCredentialsGrant
{
    /// <summary>
    /// Contains the response to the request. If the request caused an error
    /// it will be in 
    /// </summary>
    public class OAuth2ClientCredentialsGrantResponse : OAuth2ClientCredentialHandlerResponse
    {
        public HttpWebResponse response { get; set; }
    }

    /// <summary>
    /// You can use this class to access your protected Web API methods
    /// </summary>
    public class OAuth2CredentialsGrantClient
    {
        public enum OAuth2ClientError
        {
            None = 0,
            TokenDoesNotExist = 20000,
            InvalidateTokenFailed = 20001,
            ErrorGettingResponseFromServer = 20002
        }

        #region Internal classes
        /// <summary>
        /// Intrnal class used to deserialize token response
        /// </summary>
        internal class OAuth2TokenResponse
        {
            public string token_type { get; set; }
            public string access_token { get; set; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for the client
        /// </summary>
        /// <param name="consumerKey">The consumer key for the protected API</param>
        /// <param name="consumerSecret">The consumer secret for the protected API</param>
        /// <param name="apiUrl">The base url for the protected API. Example: www.yoursite.com/api</param>
        public OAuth2CredentialsGrantClient(string consumerKey, string consumerSecret, string apiUrl, bool useSsl)
        {
            this.ConsumerKey = HttpUtility.UrlEncode(consumerKey);
            this.ConsumerSecret = HttpUtility.UrlEncode(consumerSecret);

            apiUrl = apiUrl.ToLower();
            if (apiUrl.StartsWith("http://"))
                apiUrl = apiUrl.Substring(7);
            if (apiUrl.StartsWith("https://"))
                apiUrl = apiUrl.Substring(8);

            this.ApiUrl = apiUrl;
            this.UseSsl = useSsl;
        }
        #endregion

        #region Public properties
        /// <summary>
        /// The consumer key for a secured API
        /// </summary>
        public string ConsumerKey { get; set; }

        /// <summary> 
        /// The consumer secret for a secured API
        /// </summary>
        public string ConsumerSecret { get; set; }

        /// <summary>
        /// Indicates whether https should be used to access the API url
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// The base url for the API calls
        /// API calls will be of the form http://apiurl/resourcename/id/params
        /// </summary>
        public string ApiUrl { get; set; }
        #endregion

        #region Private properties
        private string accessToken = null;
        private string lastErrorAsString = null;

        /// <summary>
        /// Make a request containing consumer key and consumer secret to the specified url
        /// </summary>
        /// <param name="url">The url for the request</param>
        /// <param name="isTokenRequest">If set to true, adds the access token in the body of the request</param>
        /// <returns></returns>
        private HttpWebRequest MakeAuthorizedRequest(string url, bool isTokenRequest)
        {
            string bearerCredentials = ConsumerKey + ":" + ConsumerSecret;

            string bearerCredentialsEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(bearerCredentials));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Headers.Add("Authorization", "Basic " + bearerCredentialsEncoded);
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

            if (!isTokenRequest)
            {
                StreamWriter sw = new StreamWriter(request.GetRequestStream());
                sw.Write("grant_type=client_credentials");
                sw.Close();
            }
            else
            {
                StreamWriter sw = new StreamWriter(request.GetRequestStream());
                sw.Write("access_token=" + accessToken);
                sw.Close();
            }
            return request;
        }

        /// <summary>
        /// The authorization token obtained from a /token request
        /// </summary>
        private string AccessToken
        {
            get
            {
                if (string.IsNullOrEmpty(accessToken))
                {
                    HttpWebResponse lastResponse = null;
                    string url = MakeUrl("token", null);
                    HttpWebRequest request = MakeAuthorizedRequest(url, false);
                    
                    try
                    {
                        lastResponse = (HttpWebResponse)request.GetResponse();
                        if (lastResponse.StatusCode == HttpStatusCode.OK)
                        {
                            StreamReader sr = new StreamReader(lastResponse.GetResponseStream());
                            string responseText = sr.ReadToEnd();
                            sr.Close();
                            if (!string.IsNullOrEmpty(responseText))
                            {
                                OAuth2TokenResponse jresponse = (OAuth2TokenResponse)JsonConvert.DeserializeObject(responseText, typeof(OAuth2TokenResponse));
                                accessToken = jresponse.access_token;
                            }
                        }
                    }
                    catch (WebException ex)
                    {
                        StreamReader sr = new StreamReader(ex.Response.GetResponseStream());
                        lastErrorAsString = sr.ReadToEnd();
                    }
                }
                return accessToken;
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Makes a url in the form http://apiurl/resource/data1/data2/data3 ....
        /// </summary>
        /// <param name="resource">The resource to be accessed</param>
        /// <param name="urlData">The url parameters</param>
        /// <returns>The url</returns>
        private string MakeUrl(string resource, string[] urlData)
        {
            string url = "";
            if (UseSsl)
                url += "https://";
            else
                url = "http://";

            url += ApiUrl;

            if (!url.EndsWith("/"))
                url += "/";

            url += HttpUtility.UrlPathEncode(resource);

            if (urlData != null)
            {
                if (!url.EndsWith("/"))
                    url += "/";
                foreach (string urlDatum in urlData)
                {
                    url += HttpUtility.UrlPathEncode(urlDatum) + "/";
                }
            }
            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);

            return url;
        }

        /// <summary>
        /// Create a request object for accessing the protected API.
        /// The url is of the form http://apiurl/resource/urldata1/urldata2 .. 
        /// The body contains url encoded data 
        /// </summary>
        /// <param name="resource">The name of the resource to accesss</param>
        /// <param name="urlData">The data in the url part</param>
        /// <param name="bodyData">The data in the body</param>
        /// <param name="httpVerb">The verb to use</param>
        /// <returns></returns>
        private HttpWebRequest MakeRequest(string resource, string[] urlData, Dictionary<string, string> bodyData, object bodyObject, string httpVerb)
        {
            string atoken = AccessToken;
            if (string.IsNullOrEmpty(atoken))
            {
                return null;
            }

            string url = MakeUrl(resource, urlData);
            HttpWebRequest request = null;

            if (httpVerb.ToLower() == "get" && bodyData != null && bodyData.Count > 0)
            {
                if (url.EndsWith("/"))
                    url = url.Substring(0, url.Length - 1);
                url += "?";
                foreach (string key in bodyData.Keys)
                {
                    url += HttpUtility.UrlEncode(key) + "=" + HttpUtility.UrlEncode(bodyData[key]) + "&";
                }
                if (url.EndsWith("&"))
                    url = url.Substring(0, url.Length - 1);

                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = httpVerb;
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = httpVerb;

                if (bodyData != null && bodyData.Count > 0)
                {
                    request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

                    int i = 0;
                    StreamWriter sw = new StreamWriter(request.GetRequestStream());
                    foreach (string key in bodyData.Keys)
                    {
                        i++;
                        sw.Write(HttpUtility.UrlEncode(key) + "=" + HttpUtility.UrlEncode(bodyData[key]));
                        if (i < bodyData.Count)
                            sw.Write("&");
                    }
                    sw.Close();
                }
                else if(bodyObject != null)
                {
                    request.ContentType = "application/json";

                    int i = 0;
                    StreamWriter sw = new StreamWriter(request.GetRequestStream());
                    string json = JsonConvert.SerializeObject(bodyObject);
                    sw.Write(json);
                    sw.Close();
                }
            }
            request.Headers.Add("Authorization", "Bearer " + atoken);
            return request;
        }

        /// <summary>
        /// Internal method to perform a web request.
        /// Handles the case when API returns 403 forbidden due to expired token
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>The response</returns>
        private OAuth2ClientCredentialsGrantResponse PerformRequest(string resource, string[] urlData, Dictionary<string, string> bodyData, object bodyObject, string verb)
        {
            for (int i = 0; i < 2; i++)
            {
                HttpWebRequest request = MakeRequest(resource, urlData, bodyData, bodyObject, verb);
                if (request == null && !string.IsNullOrEmpty(lastErrorAsString))
                {
                    OAuth2ClientCredentialsGrantResponse resp = (OAuth2ClientCredentialsGrantResponse) JsonConvert.DeserializeObject(lastErrorAsString, typeof(OAuth2ClientCredentialsGrantResponse));
                    resp.response = null;
                    lastErrorAsString = null;
                    return resp;
                }

                HttpWebResponse response = null;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                    OAuth2ClientCredentialsGrantResponse toRet = new OAuth2ClientCredentialsGrantResponse();
                    toRet.errorCode = 0;
                    toRet.errorType = 0;
                    toRet.errorMessage = null;
                    toRet.response = response;
                    return toRet;
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        StreamReader sr = new StreamReader(ex.Response.GetResponseStream());
                        lastErrorAsString = sr.ReadToEnd();
                        OAuth2ClientCredentialsGrantResponse resp = (OAuth2ClientCredentialsGrantResponse)JsonConvert.DeserializeObject(lastErrorAsString, typeof(OAuth2ClientCredentialsGrantResponse));
                        if (resp.errorCode == (int)OAuth2ClientCredentialsGrantError.InvalidAccessToken)
                        {
                            //We need to renew the token
                            accessToken = null;
                            lastErrorAsString = null;
                        }
                        else
                        {
                            resp.response = response;
                            return resp;
                        }
                    }
                    else
                    {
                        OAuth2ClientCredentialsGrantResponse resp = new OAuth2ClientCredentialsGrantResponse();
                        resp.errorCode = (int) OAuth2ClientError.ErrorGettingResponseFromServer;
                        resp.errorType = (int)OAuth2ClientCredentialsGrant.OAuth2ErrorType.ApiClientError;
                        resp.response = null;

                        return resp;
                    }
                }
            }
            return null;
        }
        #endregion

        #region Client REST methods
        /// <summary>
        /// Invalidates the current authentication token if it exists
        /// </summary>
        /// <returns></returns>
        public OAuth2ClientCredentialsGrantResponse InvalidateToken()
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                OAuth2ClientCredentialsGrantResponse response = new OAuth2ClientCredentialsGrantResponse();
                response.errorCode = (int) OAuth2ClientError.TokenDoesNotExist;
                response.errorType = (int) OAuth2ErrorType.ApiClientError;
                response.errorMessage = "No token to delete";
                response.response = null;
                return response;
            }

            //We need to invalidate the token
            HttpWebResponse lastResponse = null;
            string url = MakeUrl("invalidate_token", null);
            HttpWebRequest request = MakeAuthorizedRequest(url, true);

            try
            {
                lastResponse = (HttpWebResponse)request.GetResponse();
                if (lastResponse.StatusCode == HttpStatusCode.OK)
                {
                    accessToken = null;

                    OAuth2ClientCredentialsGrantResponse response = new OAuth2ClientCredentialsGrantResponse();
                    response.errorCode = 0;
                    response.errorType = 0;
                    response.errorMessage = null;
                    response.response = lastResponse;
                    return response;
                }
                else
                {
                    OAuth2ClientCredentialsGrantResponse response = new OAuth2ClientCredentialsGrantResponse();
                    response.errorCode = (int) OAuth2ClientError.InvalidateTokenFailed;
                    response.errorType = (int) OAuth2ErrorType.ApiClientError;
                    response.errorMessage = "Could not invalidate token";
                    response.response = lastResponse;
                    return response;
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    StreamReader sr = new StreamReader(ex.Response.GetResponseStream());
                    string err = sr.ReadToEnd();
                    OAuth2ClientCredentialsGrantResponse response = (OAuth2ClientCredentialsGrantResponse)JsonConvert.DeserializeObject(err, typeof(OAuth2ClientCredentialsGrantResponse));
                    response.response = null;
                    return response;
                }
                else
                {
                    OAuth2ClientCredentialsGrantResponse resp = new OAuth2ClientCredentialsGrantResponse();
                    resp.errorCode = (int)OAuth2ClientError.ErrorGettingResponseFromServer;
                    resp.errorType = (int)OAuth2ClientCredentialsGrant.OAuth2ErrorType.ApiClientError;
                    resp.response = null;

                    return resp;
                }
            }
        }


        /// <summary>
        /// Performs a get request on http://apiurl/resource
        /// </summary>
        /// <param name="resource">The resource to get</param>
        /// <returns>The response object</returns>
        public OAuth2ClientCredentialsGrantResponse Get(string resource)
        {
            return PerformRequest(resource, null, null, null, "GET");
        }

        /// <summary>
        /// Performs a get request on http://apiurl/resource/id1/id2 ...
        /// </summary>
        /// <param name="resource">The resource to get</param>
        /// <param name="ids">The ids to pass in url</param>
        /// <returns></returns>
        public OAuth2ClientCredentialsGrantResponse Get(string resource, Dictionary<string, string> queryStringData, params string[] ids)
        {
            return PerformRequest(resource, ids, queryStringData, null, "GET");
        }


        /// <summary>
        /// Performs a get request on http://apiurl/resource/id1/id2 ...
        /// </summary>
        /// <param name="resource">The resource to get</param>
        /// <param name="ids">The ids to pass in url</param>
        /// <returns></returns>
        public OAuth2ClientCredentialsGrantResponse Get(string resource, params string[] ids)
        {
            return PerformRequest(resource, ids, null, null, "GET");
        }

        /// <summary>
        /// Makes a post request to url specified by http://apiurl/resource
        /// The post contains the bodyParams name value pairs in its body
        /// </summary>
        /// <param name="resource">The resource we are posting</param>
        /// <param name="bodyParams">The name value pairs to post in the body</param>
        /// <returns></returns>
        public OAuth2ClientCredentialsGrantResponse Post(string resource, Dictionary<string, string> bodyParams)
        {
            return PerformRequest(resource, null, bodyParams, null, "POST");
        }

        /// <summary>
        /// Makes a put request to url specified by http://apiurl/resource
        /// The put request contains the the bodyObject serialized to JSON in its body 
        /// </summary>
        /// <param name="resource">The resource we are putting</param>
        /// <param name="bodyParams">The name value pairs to post in the body</param>
        /// <returns></returns>
        public OAuth2ClientCredentialsGrantResponse Put(string resource, object bodyObject)
        {
            return PerformRequest(resource, null, null, bodyObject, "PUT");
        }

        /// <summary>
        /// Makes a post request to url specified by http://apiurl/resource
        /// The post contains the bodyObject serialized as JSON in its body
        /// </summary>
        /// <param name="resource">The resource we are posting</param>
        /// <param name="bodyParams">The name value pairs to post in the body</param>
        /// <returns></returns>
        public OAuth2ClientCredentialsGrantResponse Post(string resource, object bodyObject)
        {
            return PerformRequest(resource, null, null, bodyObject, "POST");
        }

        /// <summary>
        /// Makes a put request to url specified by http://apiurl/resource
        /// The put request contains the bodyParams name value pairs in its body
        /// </summary>
        /// <param name="resource">The resource we are putting</param>
        /// <param name="bodyParams">The name value pairs to post in the body</param>
        /// <returns></returns>
        public OAuth2ClientCredentialsGrantResponse Put(string resource, Dictionary<string, string> bodyParams)
        {
            return PerformRequest(resource, null, bodyParams, null, "PUT");
        }

        /// <summary>
        /// Performs a delete request on http://apiurl/resource/id1/id2 ...
        /// </summary>
        /// <param name="resource">The resource to delete</param>
        /// <param name="ids">The ids to pass in url</param>
        /// <returns></returns>
        public OAuth2ClientCredentialsGrantResponse Delete(string resource, params string[] ids)
        {
            return PerformRequest(resource, ids, null, null, "DELETE");
        }
        #endregion
    }
}

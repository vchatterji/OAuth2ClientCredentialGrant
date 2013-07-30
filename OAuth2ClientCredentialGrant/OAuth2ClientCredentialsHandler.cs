using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using WebMatrix.WebData;

namespace OAuth2ClientCredentialsGrant
{
    /// <summary>
    /// Possible authentication errors
    /// </summary>
    public enum OAuth2ClientCredentialsGrantError
    {
        None = 0, //No error
        ErrorCreatingToken = 10001,
        InvalidAccessToken = 10002,
        RequestNeedsHttps = 10003,
        ErrorSettingSecurityPrincipal = 10004,
        ErrorIssuingToken = 10005,
        InvalidCredentials = 10006,
        RequestMissingAuthorizationHeader = 10007,
        UnAuthenticatedRequest = 10008,
        BodyMissingGrantType = 10009,
        BodyMissingAccessToken = 10010,
        Base64DecodingFailed = 10011,
        MessageHasIncorrectNumberOfParts = 10012,
        UrlDecodingFailed = 10013,
        ApiCallFailed = 10014
    }

    /// <summary>
    /// The error type
    /// </summary>
    public enum OAuth2ErrorType
    {
        None = 0,
        AuthenticationError = 1,
        ApiClientError = 2
    }

    /// <summary>
    /// Base class for an API response
    /// </summary>
    public class OAuth2ClientCredentialHandlerResponse
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string errorMessage { get; set; }

        /// <summary>
        /// Error type. See OAuth2ErrorType
        /// </summary>
        public int errorType { get; set; }

        /// <summary>
        /// Error code.
        /// </summary>
        public int errorCode { get; set; }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //                             Example of adding this to your Web API Route                         //
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /*
     *      //This can usually be found in the App_Start/WebApiConfig 
            public static class WebApiConfig
            {
                public static void Register(HttpConfiguration config)
                {
                    config.Routes.MapHttpRoute(
                        name: "DefaultApi",
                        routeTemplate: "api/{controller}/{id}",
                        defaults: new { id = RouteParameter.Optional },
                        constraints: null,
                        handler: HttpClientFactory.CreatePipeline(
                                  new HttpControllerDispatcher(config),
                                  new DelegatingHandler[] { new OAuth2ClientCredentialsHandler() })
                    );

                    var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
                    config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);
                }
            }
     * 
     */


    /// <summary>
    /// This is the authentication handler that can be added to your API route.
    /// An example configuration is given in the source of this file. This implements
    /// the protocol described at Application Only authentication:
    /// https://dev.twitter.com/docs/auth/application-only-auth
    /// 
    /// To access your API once this handler is added to the route, clients must first call
    /// http://yoursite/api/token with basic authentication and consumer key, consumer secret pair to obtain a token.
    /// Subsequent calls to your API must pass the token obtained in the Authentication header
    /// (please see https://dev.twitter.com/docs/auth/application-only-auth for protocol details.
    /// 
    /// API Crdentials can be created, retrieved and deleted by using OAuthDataFactory.CredentialManager
    /// </summary>
    public class OAuth2ClientCredentialsHandler : DelegatingHandler
    {
        /// <summary>
        /// Response for a token request
        /// </summary>
        private class BearerTokenResponse
        {
            public string token_type { get; set; }
            public string access_token { get; set; }
        }

        /// <summary>
        /// Response for a invalidate token request
        /// </summary>
        private class InvalidateTokenResponse
        {
            public string access_token { get; set; }
        }

        /// <summary>
        /// Responds with HTTP Status code "Bad Request" and error message
        /// </summary>
        /// <param name="request">The request we are responding to</param>
        /// <param name="authenticationErrorCode">The error ocde</param>
        /// <param name="message">The message that will be populated in the response (as JSON)</param>
        /// <returns></returns>
        public HttpResponseMessage RespondError(HttpRequestMessage request, OAuth2ClientCredentialsGrantError authenticationErrorCode, string message)
        {
            return RespondError(request, HttpStatusCode.BadRequest, authenticationErrorCode, message);
        }

        /// <summary>
        /// Responds with the specified HTTP status code and error message
        /// </summary>
        /// <param name="request">The request we are responding to</param>
        /// <param name="status">The HTTP status code to respond with</param>
        /// <param name="authenticationErrorCode">The error code</param>
        /// <param name="message">The message that will be populated in the response (as JSON)</param>
        /// <returns></returns>
        public HttpResponseMessage RespondError(HttpRequestMessage request, HttpStatusCode status, OAuth2ClientCredentialsGrantError authenticationErrorCode, string message)
        {
            var header = new MediaTypeHeaderValue("application/json");
            OAuth2ClientCredentialHandlerResponse msg = new OAuth2ClientCredentialHandlerResponse();
            msg.errorMessage = message;
            msg.errorCode = (int)authenticationErrorCode;
            msg.errorType = (int)OAuth2ErrorType.AuthenticationError;
            return request.CreateResponse<OAuth2ClientCredentialHandlerResponse>(status, msg, header);
        }

        /// <summary>
        /// Responds to an authentication request with the access token
        /// </summary>
        /// <param name="request">The authentication request we are responding to</param>
        /// <param name="accessToken">The access token to be used in subsequent API calls</param>
        /// <returns></returns>
        public HttpResponseMessage RespondAuthentication(HttpRequestMessage request, string accessToken)
        {
            var header = new MediaTypeHeaderValue("application/json");
            BearerTokenResponse msg = new BearerTokenResponse();
            msg.token_type = "bearer";
            msg.access_token = accessToken;
            return request.CreateResponse<BearerTokenResponse>(HttpStatusCode.OK, msg, header);
        }


        /// <summary>
        /// Sets the security principal for the current thread
        /// </summary>
        /// <param name="username">The username</param>
        public void SetSecurityPrincipal(string username)
        {
            string[] roles = new string[0];
            try
            {
                roles = Roles.GetRolesForUser(username);
            }
            catch (Exception)
            {

            }
            finally
            {
                Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(username), roles);
            }
        }

        /// <summary>
        /// Responds to an invalidate token request
        /// </summary>
        /// <param name="request">The request we are responding to</param>
        /// <param name="accessToken">The access token to be invalidated</param>
        /// <returns></returns>
        public HttpResponseMessage RespondInvalidateToken(HttpRequestMessage request, string accessToken)
        {
            var header = new MediaTypeHeaderValue("application/json");
            InvalidateTokenResponse msg = new InvalidateTokenResponse();
            msg.access_token = accessToken;
            return request.CreateResponse<InvalidateTokenResponse>(HttpStatusCode.OK, msg, header);
        }

        /// <summary>
        /// Check if a bearer token is valid. Also sets the security principle if the token is valid.
        /// </summary>
        /// <param name="request">The request containing the token</param>
        /// <param name="token">The actual token</param>
        /// <returns></returns>
        public bool ValidateToken(HttpRequestMessage request, string token)
        {
            if (OAuth2DataFactory.TokenManager == null)
                return false;

            OAuth2Token tokenObj = OAuth2DataFactory.TokenManager.GetToken(token);
            if (tokenObj == null)
                return false;

            request.Properties.Add("ConsumerSecret", tokenObj.ConsumerSecret);
            request.Properties.Add("ConsumerKey", tokenObj.ConsumerKey);

            SetSecurityPrincipal(tokenObj.Username);
            return true;
        }

        /// <summary>
        /// Invalidates the specified bearer token
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="token">The token to be invalidated</param>
        /// <returns></returns>
        public bool InvalidateToken(HttpRequestMessage request, string token)
        {
            if (OAuth2DataFactory.TokenManager == null)
                return false;

            return OAuth2DataFactory.TokenManager.InvalidateToken(token) != null;
        }

        /// <summary>
        /// Gets the supplied credentials
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="consumer_Key">The consumer key</param>
        /// <param name="consumer_Secret">The consumer secret</param>
        /// <returns></returns>
        public OAuth2Credential GetCredentials(HttpRequestMessage request, string consumer_Key, string consumer_Secret)
        {
            if (OAuth2DataFactory.CredentialManager == null)
                return null;

            OAuth2Credential credential = OAuth2DataFactory.CredentialManager.GetCredential(consumer_Key, consumer_Secret);
            if (credential == null)
                return null;
            try
            {
                int userid = WebSecurity.GetUserId(credential.Username);
                if (userid <= 0)
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
            SetSecurityPrincipal(credential.Username);
            return credential;
        }

        /// <summary>
        /// Issue a token based on the consumer key and consumer secret
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="credentials">The credentials</param>

        /// <returns></returns>
        public string IssueToken(HttpRequestMessage request, OAuth2Credential credentials)
        {
            if (OAuth2DataFactory.TokenManager == null)
                return null;

            if (OAuth2DataFactory.CredentialManager == null)
                return null;


            bool isNew = false;
            OAuth2Token token = OAuth2DataFactory.TokenManager.CreateToken(credentials.Username, credentials.ConsumerKey, credentials.ConsumerSecret, out isNew);

            if (token == null)
            {
                RespondError(request, HttpStatusCode.InternalServerError, OAuth2ClientCredentialsGrantError.ErrorCreatingToken, "Error creating token");
            }


            token.ValidTill = DateTime.UtcNow.AddMinutes(OAuth2Config.OAuth2TokenValidityMinutes);

            if (!isNew)
                return token.AccessToken;

            if (OAuth2DataFactory.TokenManager.SaveToken(token))
            {
                return token.AccessToken;
            }
            else
            {
                return null;
            }
        }

        protected async override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //Make sure request is over SSL unless it is configured otherwise
            if (OAuth2Config.OAuth2RequireSsl && request.RequestUri.Scheme.ToLower() != "https")
            {
                return RespondError(request, OAuth2ClientCredentialsGrantError.RequestNeedsHttps, "You need to use https to access this API");
            }

            //This is a request is authenticated with a bearer token. We will validate the
            //token and let the request hit the API if it is valid
            if (request.Headers.Authorization != null &&
                !string.IsNullOrEmpty(request.Headers.Authorization.Scheme) &&
                request.Headers.Authorization.Scheme.ToLower().Trim() == "bearer" &&
                !string.IsNullOrEmpty(request.Headers.Authorization.Parameter))
            {
                //Validate bearer token
                string token = request.Headers.Authorization.Parameter;
                if (ValidateToken(request, token))
                {
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        response = RespondError(request, OAuth2ClientCredentialsGrantError.ApiCallFailed, "Error calling the web API");
                    }
                    return response;
                }
                else
                {
                    return RespondError(request, HttpStatusCode.Forbidden, OAuth2ClientCredentialsGrantError.InvalidAccessToken, "Access token could not be validated. Please obtain a new token");
                }
            }
            else if (
                request.Headers.Authorization != null && request.Content != null &&
                !string.IsNullOrEmpty(request.Headers.Authorization.Scheme) &&
                request.Headers.Authorization.Scheme.ToLower().Trim() == "basic" &&
                !string.IsNullOrEmpty(request.Headers.Authorization.Parameter) &&
                request.RequestUri.Segments != null &&
                request.RequestUri.Segments.Length >= 3 &&
                (request.RequestUri.Segments[2].ToLower().StartsWith("token") || request.RequestUri.Segments[2].ToLower().StartsWith("invalidate_token")))
            {
                string body = request.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(body) && request.RequestUri.Segments[2].ToLower().StartsWith("token"))
                {
                    //Token request without grant_type specified
                    return RespondError(request, OAuth2ClientCredentialsGrantError.BodyMissingGrantType, "Body must contain 'grant_type=client_credentials'");
                }
                else if (string.IsNullOrEmpty(body) && request.RequestUri.Segments[2].ToLower().StartsWith("invalidate_token"))
                {
                    //Invalidate request without token specified
                    return RespondError(request, OAuth2ClientCredentialsGrantError.BodyMissingAccessToken, "Body must contain 'access_token=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA...'");
                }

                NameValueCollection nvc = HttpUtility.ParseQueryString(body, Encoding.UTF8);
                if (request.RequestUri.Segments[2].ToLower().StartsWith("token") &&
                    (string.IsNullOrEmpty(nvc["grant_type"]) || nvc["grant_type"] != "client_credentials"))
                {
                    //Token request without grant_type specified
                    return RespondError(request, OAuth2ClientCredentialsGrantError.BodyMissingGrantType, "Body must contain 'grant_type=client_credentials'");
                }
                else if (request.RequestUri.Segments[2].ToLower().StartsWith("invalidate_token") &&
                    (string.IsNullOrEmpty(nvc["access_token"]) || string.IsNullOrEmpty(nvc["access_token"])))
                {
                    //Invalidate request without token specified
                    return RespondError(request, OAuth2ClientCredentialsGrantError.BodyMissingAccessToken, "Body must contain 'access_token=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA...'");
                }

                //Requesting a new token
                string authData = request.Headers.Authorization.Parameter;
                #region Base64 Decode
                //Base64 decode
                string base64Decoded = null;
                try
                {
                    base64Decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authData));
                }
                catch (Exception)
                {
                }

                if (string.IsNullOrEmpty(base64Decoded))
                {
                    return RespondError(request, OAuth2ClientCredentialsGrantError.Base64DecodingFailed, "Base64 decoding failed");
                }
                #endregion

                string[] parts = base64Decoded.Split(':');
                if (parts.Length != 2)
                {
                    return RespondError(request, OAuth2ClientCredentialsGrantError.MessageHasIncorrectNumberOfParts, "Message has incorrect number of parts. Please use the ':' delimiter only once.");
                }

                #region Url Decode
                //Url decode
                string consumer_Key = null;
                string consumer_Secret = null;
                try
                {
                    consumer_Key = HttpUtility.UrlDecode(parts[0]);
                    consumer_Secret = HttpUtility.UrlDecode(parts[1]);
                }
                catch (Exception)
                {
                }

                if (string.IsNullOrEmpty(consumer_Key) || string.IsNullOrEmpty(consumer_Secret))
                {
                    return RespondError(request, OAuth2ClientCredentialsGrantError.UrlDecodingFailed, "Url decoding failed");
                }
                #endregion

                OAuth2Credential credentials = null;
                try
                {
                    credentials = GetCredentials(request, consumer_Key, consumer_Secret);
                }
                catch (SecurityException sex)
                {
                    return RespondError(request, HttpStatusCode.InternalServerError, OAuth2ClientCredentialsGrantError.ErrorSettingSecurityPrincipal, "Error setting security principle");
                }


                if (credentials != null)
                {
                    if (request.RequestUri.Segments[2].ToLower().StartsWith("invalidate_token"))
                    {
                        if (InvalidateToken(request, nvc["access_token"]))
                        {
                            return RespondInvalidateToken(request, nvc["access_token"]);
                        }
                        else
                        {
                            return RespondError(request, HttpStatusCode.InternalServerError, OAuth2ClientCredentialsGrantError.InvalidAccessToken, "Invalid token or token could not be invalidated");
                        }
                    }
                    else
                    {
                        string access_token = IssueToken(request, credentials);
                        if (!string.IsNullOrEmpty(access_token))
                        {
                            return RespondAuthentication(request, access_token);
                        }
                        else
                        {
                            return RespondError(request, HttpStatusCode.InternalServerError, OAuth2ClientCredentialsGrantError.ErrorIssuingToken, "Access token could not be issued");
                        }
                    }
                }
                else
                {
                    return RespondError(request, HttpStatusCode.Forbidden, OAuth2ClientCredentialsGrantError.InvalidCredentials, "Invalid consumer_Key, consumer_Secret pair");
                }
            }
            else
            {
                if (request.RequestUri.Segments.Length >= 3 && request.RequestUri.Segments[2].ToLower().StartsWith("token"))
                    return RespondError(request, HttpStatusCode.BadRequest, OAuth2ClientCredentialsGrantError.RequestMissingAuthorizationHeader, "Request has missing or incorrect Authorization header");

                if (request.RequestUri.Segments.Length >= 3 && request.RequestUri.Segments[2].ToLower().StartsWith("invalidate_token"))
                    return RespondError(request, HttpStatusCode.BadRequest, OAuth2ClientCredentialsGrantError.InvalidAccessToken, "Request has missing token");

                //403 Forbidden
                return RespondError(request, HttpStatusCode.Forbidden, OAuth2ClientCredentialsGrantError.UnAuthenticatedRequest, "Please authenticate by a request to /api/token before calling any API methods");
            }
        }
    }
}

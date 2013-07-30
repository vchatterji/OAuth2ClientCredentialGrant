using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2ClientCredentialsGrant
{
    /// <summary>
    /// The abstract class for a token manager
    /// </summary>
    public abstract class OAuth2TokenManager
    {
        ObjectCache cache = MemoryCache.Default;


        /// <summary>
        /// Implementation should create or retrieve an OAuth2Token object based on the specified parameters.
        /// The object should not stored in the persistent store until a call is made to SaveToken(). 
        /// If this method returns isNew = true, then caller must make sure SaveToken is called
        /// to persist the token in the underlying storage
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <param name="isNew">Out parameter which indicates if the token was retrieved from store (isNew = false).
        /// If the token was not retrieved from the store, the caller must persist the token to the store by calling
        /// SaveToken()
        /// </param>
        /// <returns>The token retrieved or created</returns>
        protected abstract OAuth2Token CreateTokenInStore(string username, string consumerKey, string consumerSecret, out bool isNew);

        /// <summary>
        /// Creates or retrieves an OAuth2Token object based on the specified parameters.
        /// The object is not stored in the persistent store until a call is made to SaveToken. 
        /// If this method returns isNew = true, then caller must make sure SaveToken is called
        /// to persist the token in the underlying storage
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <param name="isNew">Out parameter which indicates if the token was retrieved from store (isNew = false).
        /// If the token was not retrieved from the store, the caller must persist the token to the store by calling
        /// SaveToken()
        /// </param>
        /// <returns></returns>
        public OAuth2Token CreateToken(string username, string consumerKey, string consumerSecret, out bool isNew)
        {
            isNew = false;
            OAuth2Token token = (OAuth2Token)cache.Get(consumerKey + consumerSecret);
            if (token == null || token.ValidTill < DateTime.UtcNow)
            {
                token = CreateTokenInStore(username, consumerKey, consumerSecret, out isNew);
                if (token != null && !isNew)
                {
                    CacheItemPolicy policy = new CacheItemPolicy();
                    DateTime absoluteExpiration = DateTime.UtcNow.AddMinutes(OAuth2Config.OAuth2TokenLocalCacheValidityMinutes);
                    policy.AbsoluteExpiration = new DateTimeOffset(absoluteExpiration);
                    cache.Add(token.AccessToken, token, policy);
                    cache.Add(token.ConsumerKey + token.ConsumerSecret, token, policy);
                }
            }
            return token;

        }

        /// <summary>
        /// Should gets the specified token. If the token is expired, this method shoulds delete it from the storage
        /// </summary>
        /// <param name="accessToken">The access token to get</param>
        /// <returns></returns>
        protected abstract OAuth2Token GetTokenFromStore(string accessToken);

        /// <summary>
        /// Gets the specified token. If the token is expired, this method shoulds delete it from the storage
        /// </summary>
        /// <param name="accessToken">The access token to get</param>
        /// <returns></returns>
        public OAuth2Token GetToken(string accessToken)
        {
            OAuth2Token token = (OAuth2Token)cache.Get(accessToken);
            if (token == null || token.ValidTill < DateTime.UtcNow)
            {
                token = GetTokenFromStore(accessToken);
                if (token != null)
                {
                    CacheItemPolicy policy = new CacheItemPolicy();
                    DateTime absoluteExpiration = DateTime.UtcNow.AddMinutes(OAuth2Config.OAuth2TokenLocalCacheValidityMinutes);
                    policy.AbsoluteExpiration = new DateTimeOffset(absoluteExpiration);
                    cache.Add(accessToken, token, policy);
                    cache.Add(token.ConsumerKey + token.ConsumerSecret, token, policy);
                }
            }
            return token;
        }

        /// <summary>
        /// Should writes the specified token to the underlying storage
        /// </summary>
        /// <param name="token">The token to save</param>
        /// <returns></returns>
        protected abstract bool SaveTokenToStore(OAuth2Token token);

        /// <summary>
        /// Writes the specified token to the underlying storage
        /// </summary>
        /// <param name="token">The token to save</param>
        /// <returns></returns>
        public bool SaveToken(OAuth2Token token)
        {
            bool toRet = SaveTokenToStore(token);
            if (toRet)
            {
                CacheItemPolicy policy = new CacheItemPolicy();
                DateTime absoluteExpiration = DateTime.UtcNow.AddMinutes(OAuth2Config.OAuth2TokenLocalCacheValidityMinutes);
                policy.AbsoluteExpiration = new DateTimeOffset(absoluteExpiration);
                cache.Add(token.AccessToken, token, policy);
                cache.Add(token.ConsumerKey + token.ConsumerSecret, token, policy);
            }
            return toRet;
        }

        /// <summary>
        /// Invalidates the specified token and removes it from the store
        /// </summary>
        /// <param name="accessToken">The access token to invalidate</param>
        /// <returns></returns>
        protected abstract OAuth2Token InvalidateTokenInStore(string accessToken);

        /// <summary>
        /// Invalidates the specified token and removes it from the store
        /// </summary>
        /// <param name="accessToken">The access token to invalidate</param>
        /// <returns></returns>
        public OAuth2Token InvalidateToken(string accessToken)
        {
            OAuth2Token token = InvalidateTokenInStore(accessToken);
            if (token != null)
            {
                cache.Remove(token.AccessToken);
                cache.Remove(token.ConsumerKey + token.ConsumerSecret);
            }
            return token;
        }

        /// <summary>
        /// Invalidates the token for the specified consumer key, consumer secret pair and removes it from the store
        /// </summary>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <returns></returns>
        protected abstract OAuth2Token InvalidateTokenInStore(string consumerKey, string consumerSecret);

        public OAuth2Token InvalidateToken(string consumerKey, string consumerSecret)
        {
            OAuth2Token token = InvalidateTokenInStore(consumerKey, consumerSecret);
            if (token != null)
            {
                cache.Remove(token.AccessToken);
                cache.Remove(token.ConsumerKey + token.ConsumerSecret);
            }
            return token;
        }
    }
}

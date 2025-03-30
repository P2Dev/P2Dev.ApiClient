using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace P2Dev.ApiClient
{
    public class ApiClientBase
    {
        HttpClient httpClient = new HttpClient();
        public ITokenProvider TokenProvider { get; set; }
        public string BaseURL { get; set; }

        public Token Tokens
        {
            get
            {
                return TokenProvider?.Tokens;
            }

            set
            {
                TokenProvider.Tokens = value;
            }
        }

        public ApiClientBase()
        {
        }

        public string WhoAmI()
        {
            if (TokenProvider?.Tokens?.ID == null) return null;

            JObject t = JObject.Parse(Base64Url.Decode(TokenProvider?.Tokens?.ID.Split(new char[] { '.' })[1]));

            return (string)t["sub"];
        }

        public string GenURL(string path)
        {
            return BaseURL + path;
        }

        public async Task GetTokens(string authCode)
        {
            Token t = await TokenProvider?.GetTokens(authCode);

            TokenProvider.Tokens = t;
        }

        public async Task<Token> RefreshTokens()
        {
            if (TokenProvider?.Tokens?.Refresh == null) return null;

            Token t = await TokenProvider?.RefreshTokens();

            return t;
        }

        /// <summary>
        /// Called after the request has been decorated with the Tokens and request body. Override to do additional modifications before sending the request.
        /// </summary>
        /// <param name="hrm"></param>
        /// <returns></returns>
        public virtual Task<HttpRequestMessage> PreSend(HttpRequestMessage hrm)
        {
            return Task.FromResult(hrm);
        }

        /// <summary>
        /// Called after the request has successfully returned, but before the response body is deserialized. Override to pull additional data out of the response, such as headers.
        /// </summary>
        /// <param name="hrm"></param>
        /// <returns></returns>
        public virtual Task<HttpResponseMessage> PostSend(HttpResponseMessage hrm)
        {
            return Task.FromResult(hrm);
        }

        /// <summary>
        /// Send a request.
        /// </summary>
        /// <param name="method">Http method to invoke the request, typically GET, POST, PUT, DELETE</param>
        /// <param name="url">If the url does not begin with http:// or https:// the BaseUrl will be prepended to the supplied url</param>
        /// <param name="body">Optional request body, sent as json</param>
        /// <param name="authRetry">Automatically attempt to refresh the auth token on a 401 before throwing a NoTokenException</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NoTokenException">401, request did not have a valid JWT in the Authorization header. See authRetry.</exception>
        /// <exception cref="NotFoundException">404, the server returned a 404</exception>
        /// <exception cref="NeedSetupException">412, indicates the user's account needs setup</exception>
        /// <exception cref="ServerSideException">500, the server returned a 500</exception>
        /// <exception cref="RestException">All other error response codes will trigger a rest exception</exception>
        public async Task<T> SendAsync<T>(HttpMethod method, string url, object body = null, bool authRetry = true)
        {
            if (!url.ToLower().StartsWith("http://") && !url.ToLower().StartsWith("https://"))
                url = GenURL(url);
            
            HttpRequestMessage req = new HttpRequestMessage(method, url);

            if (TokenProvider?.Tokens != null && TokenProvider?.Tokens.ID != null)
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenProvider?.Tokens.ID);

            if (body != null)
            {
                req.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            }

            req = await PreSend(req);

            HttpResponseMessage res = await httpClient.SendAsync(req);
            res = await PostSend(res);
            string content = await res.Content.ReadAsStringAsync();

            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (authRetry)
                {
                    Token newTokens = await RefreshTokens();

                    if (newTokens != null && newTokens.ID != null)
                    {
                        TokenProvider.Tokens = newTokens;

                        return await SendAsync<T>(method, url, body, false);
                    }
                }

                throw new NoTokenException();
            }

            if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new NotFoundException();
            }

            if (res.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                throw new NeedSetupException();
            }

            if (res.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                throw new ServerSideException(content);
            }

            if (res.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new RestException()
                {
                    Code = (int)res.StatusCode
                };
            }

            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}

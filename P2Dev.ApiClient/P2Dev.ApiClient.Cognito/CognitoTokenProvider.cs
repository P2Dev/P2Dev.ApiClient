using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace P2Dev.ApiClient.Cognito
{
    public class CognitoTokenProvider : ITokenProvider
    {
        public string ClientId { get; set; }
        public string RedirectUri { get; set; }
        public string PoolName { get; set; }
        public string Region { get; set; }
        public string PoolId { get; set; }

        public Token Tokens { get; set; } = null;

        public async Task<Token> GetTokens(string authCode)
        {
            if (authCode.Contains("#"))
                authCode = authCode.Split(new char[] { '#' })[0];

            List<KeyValuePair<string, string>> formData = new List<KeyValuePair<string, string>>();

            formData.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            formData.Add(new KeyValuePair<string, string>("client_id", ClientId));
            formData.Add(new KeyValuePair<string, string>("redirect_uri", RedirectUri));
            formData.Add(new KeyValuePair<string, string>("code", authCode));

            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, "https://" + PoolName + ".auth." + Region + ".amazoncognito.com/oauth2/token");
            req.Content = new FormUrlEncodedContent(formData);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage res = await httpClient.SendAsync(req);
            string bodyData = await res.Content.ReadAsStringAsync();
            JObject bodyObj = JObject.Parse(bodyData);

            Token tokens = new Token()
            {
                ID = (string)bodyObj["id_token"],
                Access = (string)bodyObj["access_token"],
                Refresh = (string)bodyObj["refresh_token"]
            };

            return tokens;
        }

        public async Task<Token> RefreshTokens()
        {
            CognitoClient cc = new CognitoClient(
                PoolName,
                ClientId,
                PoolId,
                RedirectUri,
                Amazon.RegionEndpoint.GetBySystemName(Region));

            Token t = await cc.RefreshTokens(Tokens.Refresh);

            //assignment is done via the client setting it
            //Tokens = t;

            return t;
        }
    }
}


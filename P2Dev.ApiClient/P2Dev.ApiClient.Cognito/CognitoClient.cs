using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace P2Dev.ApiClient.Cognito
{
    public enum CognitoResult
    {
        Unknown,
        Ok,
        PasswordChangeRequred,
        SignupOk,
        NotAuthorized,
        Error,
        UserNotFound,
        UserNameAlreadyUsed,
        PasswordRequirementsFailed,
        NotConfirmed,
        CodeExpired
    }

    public class CognitoContext
    {
        public CognitoContext(CognitoResult res = CognitoResult.Unknown)
        {
            Result = res;
        }
        public CognitoResult Result { get; set; }
    }

    public class SignInContext : CognitoContext
    {
        public SignInContext(CognitoResult res = CognitoResult.Unknown) : base(res)
        {
        }

        public String IdToken { get; set; }
        public String AccessToken { get; set; }
        public String RefreshToken { get; set; }

        public Token AsToken
        {
            get
            {
                return new Token
                {
                    ID = IdToken,
                    Access = AccessToken,
                    Refresh = RefreshToken
                };
            }
        }
    }

    public class CognitoClient
    {
        public string ClientId { get; set; }
        public string PoolId { get; set; }
        public RegionEndpoint RegionEndpoint { get; set; }

        public string RedirectURI { get; set; }
        public string PoolDomain { get; set; }
        public string PoolRegion { get; set; }


        public CognitoClient(string poolDomain, string clientId, string poolId, string redirectUri, RegionEndpoint re)
        {
            PoolDomain = poolDomain;
            ClientId = clientId;
            PoolId = poolId;
            RegionEndpoint = re;
            RedirectURI = redirectUri;
        }

        public async Task<Token> GetTokens(string authCode)
        {
            if (authCode.Contains("#"))
                authCode = authCode.Split(new char[] { '#' })[0];

            List<KeyValuePair<string, string>> formData = new List<KeyValuePair<string, string>>();

            formData.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            formData.Add(new KeyValuePair<string, string>("client_id", ClientId));
            formData.Add(new KeyValuePair<string, string>("redirect_uri", RedirectURI));
            formData.Add(new KeyValuePair<string, string>("code", authCode));

            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, $"https://{PoolDomain}.auth.{PoolRegion}.amazoncognito.com/oauth2/token");
            req.Content = new FormUrlEncodedContent(formData);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage res = await httpClient.SendAsync(req);
            string bodyData = await res.Content.ReadAsStringAsync();
            JObject bodyObj = JObject.Parse(bodyData);

            return new Token()
            {
                ID = (string)bodyObj["id_token"],
                Access = (string)bodyObj["access_token"],
                Refresh = (string)bodyObj["refresh_token"]
            };
        }

        public async Task<SignInContext> SignInCognito(string userName, string password)
        {
            try
            {

                var credentials = new AnonymousAWSCredentials();


                using (var client = new AmazonCognitoIdentityProviderClient(credentials, this.RegionEndpoint))
                {
                    CognitoUserPool userPool = new CognitoUserPool(PoolId, ClientId, client);
                    CognitoUser user = new CognitoUser(userName, ClientId, userPool, client);



                    AuthFlowResponse context = await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest()
                    {
                        Password = password
                    }).ConfigureAwait(false);


                    // TODO handle other challenges
                    if (context.ChallengeName == ChallengeNameType.NEW_PASSWORD_REQUIRED)
                        return new SignInContext(CognitoResult.PasswordChangeRequred)
                        {
                        };
                    else
                    {
                        return new SignInContext(CognitoResult.Ok)
                        {
                            //User = user,
                            IdToken = context.AuthenticationResult?.IdToken,
                            RefreshToken = context.AuthenticationResult?.RefreshToken,
                            AccessToken = context.AuthenticationResult?.AccessToken
                        };
                    }
                }
            }
            catch (NotAuthorizedException)
            {
                return new SignInContext(CognitoResult.NotAuthorized);
            }
            catch (UserNotFoundException)
            {
                return new SignInContext(CognitoResult.UserNotFound);
            }
            catch (UserNotConfirmedException)
            {
                return new SignInContext(CognitoResult.NotConfirmed);
            }
            catch (Exception e)
            {
                Console.WriteLine($"SignIn() threw an exception {e}");
            }
            return new SignInContext(CognitoResult.Unknown);
        }

        public async Task<SignInContext> RefreshToken(string userName, string idToken, string accessToken, String refreshToken, DateTime issued, DateTime expires)
        {
            try
            {
                var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint);

                CognitoUserPool userPool = new CognitoUserPool(PoolId, ClientId, provider);
                CognitoUser user = new CognitoUser("", ClientId, userPool, provider);

                user.SessionTokens = new CognitoUserSession(idToken, accessToken, refreshToken, issued, expires);

                AuthFlowResponse context = await user.StartWithRefreshTokenAuthAsync(new InitiateRefreshTokenAuthRequest
                {
                    AuthFlowType = AuthFlowType.REFRESH_TOKEN_AUTH
                })
                .ConfigureAwait(false);

                // TODO handle other challenges
                return new SignInContext(CognitoResult.Ok)
                {
                    //User = user,
                    IdToken = context.AuthenticationResult?.IdToken,
                    RefreshToken = context.AuthenticationResult?.RefreshToken,
                    AccessToken = context.AuthenticationResult?.AccessToken
                };
            }
            catch (NotAuthorizedException)
            {
                return new SignInContext(CognitoResult.NotAuthorized);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RefreshToken() threw an exception {e}");
            }
            return new SignInContext(CognitoResult.Unknown);
        }

        public async Task<CognitoContext> SignUpCognito(string userName, string password)
        {
            try
            {
                var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint);

                SignUpRequest sur = new SignUpRequest
                {
                    ClientId = ClientId,
                    Password = password,
                    Username = userName
                };

                AttributeType at = new AttributeType(); at.Name = "email"; at.Value = userName;
                sur.UserAttributes.Add(at);

                var result = await provider.SignUpAsync(sur);

                Console.WriteLine("Signed up.");

                return new CognitoContext(CognitoResult.SignupOk);
            }
            catch (UsernameExistsException)
            {
                return new CognitoContext(CognitoResult.UserNameAlreadyUsed);
            }
            catch (InvalidPasswordException ipe)
            {
                Debug.WriteLine(ipe.Message);
                return new CognitoContext(CognitoResult.PasswordRequirementsFailed);
            }
            catch (Exception e)
            {
                Console.WriteLine($"SignUp() threw an exception {e}");
            }
            return new CognitoContext(CognitoResult.Unknown);
        }

        public async Task<CognitoContext> ForgotPassword(string userName)
        {
            try
            {
                var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint);
                CognitoUserPool userPool = new CognitoUserPool(PoolId, ClientId, provider);
                CognitoUser user = new CognitoUser(userName, ClientId, userPool, provider);

                await user.ForgotPasswordAsync();

                return new CognitoContext(CognitoResult.Ok);
            }
            catch (UserNotFoundException unfe)
            {
                Debug.WriteLine(unfe.Message);
                return new CognitoContext(CognitoResult.UserNotFound);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ForgotPassword() threw an exception {e}");
            }
            return new CognitoContext(CognitoResult.Unknown);
        }

        public async Task<CognitoContext> ConfirmForgotPassword(string userName, string code, string newpass)
        {
            try
            {
                var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint);
                CognitoUserPool userPool = new CognitoUserPool(PoolId, ClientId, provider);
                CognitoUser user = new CognitoUser(userName, ClientId, userPool, provider);

                await user.ConfirmForgotPasswordAsync(code, newpass);
                
                return new CognitoContext(CognitoResult.Ok);
            }
            catch(ExpiredCodeException ece)
            {
                Debug.WriteLine(ece.Message);
                return new CognitoContext(CognitoResult.CodeExpired);
            }
            catch(InvalidPasswordException ipe)
            {
                Debug.WriteLine(ipe.Message);
                return new CognitoContext(CognitoResult.PasswordRequirementsFailed);
            }
            catch(UserNotFoundException unfe)
            {
                Debug.WriteLine(unfe.Message);
                return new CognitoContext(CognitoResult.UserNotFound);
            }
            catch(UserNotConfirmedException unce)
            {
                Debug.WriteLine(unce.Message);
                return new CognitoContext(CognitoResult.NotConfirmed);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ForgotPassword() threw an exception {e}");
            }
            return new CognitoContext(CognitoResult.Unknown);
        }

        public async Task<CognitoContext> VerifyWithCode(string userName, string code)
        {
            try
            {
                var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint);

                var result = await provider.ConfirmSignUpAsync(new ConfirmSignUpRequest
                {
                    ClientId = ClientId,
                    Username = userName,
                    ConfirmationCode = code
                });

                return new CognitoContext(CognitoResult.Ok);
            }
            catch (Exception e)
            {
                Console.WriteLine($"VerifyWithCode() threw an exception {e}");
            }
            return new CognitoContext(CognitoResult.Unknown);
        }

        public async Task<CognitoContext> ResendConfirmationCode(string userName)
        {
            try
            {
                var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint);
                var result = await provider.ResendConfirmationCodeAsync(new ResendConfirmationCodeRequest
                {
                    Username = userName,
                    ClientId = ClientId
                });

                return new CognitoContext(CognitoResult.Ok);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ResendConfirmationCode() threw an exception {e}");
            }
            return new CognitoContext(CognitoResult.Unknown);
        }

        public async Task<CognitoContext> UpdatePassword(string userName, string newPassword, string sessionId)
        {
            try
            {
                var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint);

                CognitoUserPool userPool = new CognitoUserPool(PoolId, ClientId, provider);
                CognitoUser user = new CognitoUser(userName, ClientId, userPool, provider);

                var res = await user.RespondToNewPasswordRequiredAsync(new RespondToNewPasswordRequiredRequest
                {
                    SessionID = sessionId,
                    NewPassword = newPassword
                });

                return new CognitoContext(CognitoResult.Ok);
            }
            catch (Exception e)
            {
                Console.WriteLine($"UpdatePassword() threw an exception {e}");
            }
            return new CognitoContext(CognitoResult.Unknown);
        }

        public async Task<Token> RefreshTokens(string refresh)
        {
            HttpClient httpClient = new HttpClient();
            List<KeyValuePair<string, string>> formData = new List<KeyValuePair<string, string>>();

            formData.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
            formData.Add(new KeyValuePair<string, string>("client_id", ClientId));
            formData.Add(new KeyValuePair<string, string>("refresh_token", refresh));

            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, $"https://{PoolDomain}.auth.us-east-1.amazoncognito.com/oauth2/token"); //region error here
            req.Content = new FormUrlEncodedContent(formData);

            HttpResponseMessage res = await httpClient.SendAsync(req);

            if (res.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return null;
            }

            string bodyData = await res.Content.ReadAsStringAsync();
            JObject bodyObj = JObject.Parse(bodyData);

            return new Token()
            {
                ID = (string)bodyObj["id_token"],
                Access = (string)bodyObj["access_token"],
                Refresh = refresh
            };
        }
    }
}


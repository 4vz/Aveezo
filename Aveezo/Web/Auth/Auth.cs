using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Runtime.CompilerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Aveezo
{

    [PathNeutral("/auth"), EnableIf("EnableAuth", true)]
    public class Auth : Api
    {
        public Auth(IServiceProvider i) : base(i) { }

        [Get]
        [Disabled]
        public Result<AuthTokenResponse> Begin(
            [Query("response_type"), Required] string responseType, 
            [Query("client_id")] string clientId,
            [Query("redirect_uri"), Required, Uri(UriProperties.IsAbsolute)] Uri redirectUri,
            [Query("scope"), Required] string scope,
            [Query("state")] string state
            )
        {
            string upuser = null, uppass = null;

            // check for basic authorization 
            var hauth = Request.Headers["Authorization"];
            if (hauth.Count > 0 && hauth[0] != null)
            {
                var auth = hauth[0];
                var authp = auth.Split(Collections.Space, StringSplitOptions.RemoveEmptyEntries);
                                 
                if (authp.Length > 1 && authp[0].ToLower() == "basic")
                {
                    var uphash = authp[1];
                    var uppair = Base64.Decode(uphash).Split(Collections.Colon, 2);

                    if (uppair.Length >= 2)
                    {
                        upuser = uppair[0];
                        uppass = uppair[1];
                    }
                }
            }

            if (upuser != null && uppass != null)
            {
                if (redirectUri.IsAbsoluteUri)
                {
                    var uri = new UriBuilder(redirectUri.AbsoluteUri);
                    var newQuery = HttpUtility.ParseQueryString(uri.Query);
                    newQuery["code"] = "123231";
                    newQuery["state"] = state;
                    uri.Query = newQuery.ToString();

                    return Redirect(uri.Uri.AbsoluteUri);
                }
                else
                {
                    return Ok();
                }
            }
            else
            {
                Response.Headers.Add("WWW-Authenticate", "Basic Realm=\"\"");
                return Unauthorized();
            }
        }

        /// <summary>
        /// Get token response for specified request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Post("/token"), NoCache]
        [Disabled]
        public Result<AuthTokenResponse> GetToken(
            [Body] AuthTokenRequest request
            )
        {
            var auth = Service<IAuthService>();

            var grantType = request.GrantType;

            if (grantType == "client_credentials")
            {
                if (TokenAuthClient(request.ClientId, request.ClientSecret, out Guid? clientId, out Result<AuthTokenResponse> authClientResult))
                {
                    if (auth.AuthenticateClientScope(clientId.Value, request.Scope, out (string, Guid)[] validScope))
                    {
                        var scopes = validScope.Cast();

                        var sessionId = auth.CreateSession(clientId.Value, null, scopes.ToArray<Guid>(1));
                        var refreshToken = auth.CreateRefreshToken(sessionId, null);
                        var accessToken = auth.CreateAccessToken(sessionId, scopes.ToArray<string>(0).Join());

                        if (accessToken != null)
                        {
                            var response = new AuthTokenResponse
                            {
                                AccessToken = accessToken,
                                RefreshToken = refreshToken,
                                ExpiresIn = Options.AuthAccessTokenExpire
                            };

                            return Ok(response);
                        }
                        else
                            return Unavailable();
                    }
                    else
                        return NotFound("grant_client_credentials", "scope_undefined");
                }
                else 
                    return authClientResult;
            }
            else if (grantType == "refresh_token")
            {
                if (auth.AuthenticateRefresh(request.RefreshToken, out Guid? refreshIdc, out Guid? sessionIdc, out bool expired))
                {
                    if (!expired)
                    {
                        var sessionId = sessionIdc.Value;

                        // refresh token is available to use
                        string scope = null;

                        // client want to change scope, check for authentication
                        if (request.Scope != null)
                        {
                            /*TokenAuthClient(request, out Guid? clientIdc);

                            if (clientIdc != null)
                            {
                                var clientId = clientIdc.Value;

                                // client want to reauthenticate
                                // want to change scope
                                if (auth.AuthenticateClientScope(clientId, request.Scope, out (string, Guid)[] validScope))
                                {
                                    var scopes = validScope.Cast();
                                    auth.UpdateSessionScope(sessionId, scopes.ToArray<Guid>(1));

                                    scope = scopes.ToArray<string>(0).Join();
                                }
                            }*/
                        }
                        if (scope == null)
                        {
                            var sxc = auth.GetSessionScope(sessionId);
                            scope = sxc.Cast().ToArray<string>(1).Join();
                        }

                        // create new refresh
                        var refreshToken = auth.CreateRefreshToken(sessionId, refreshIdc.Value);
                        var accessToken = auth.CreateAccessToken(sessionId, scope);

                        if (accessToken != null)
                        {
                            var response = new AuthTokenResponse
                            {
                                AccessToken = accessToken,
                                RefreshToken = refreshToken,
                                ExpiresIn = Options.AuthAccessTokenExpire
                            };

                            return Ok(response);
                        }
                        else
                            return Unavailable();
                    }
                    else
                        return Forbidden("grant_refresh_token", "expired");
                }
                else
                    return Forbidden("grant_refresh_token", "failure");
            }
            else
                return BadRequest("grant", "type is not supported");
        }
                
        private bool TokenAuthClient(string base64ClientId, string base64ClientSecret, out Guid? clientId, out Result<AuthTokenResponse> result)
        {
            clientId = null;
            result = null;
            
            var ret = false;
            var auth = Service<IAuthService>();

            if (Base64.TryUrlGuidDecode(base64ClientId, out clientId) && Base64.TryUrlDecode(base64ClientSecret, out var clientSecret))
            {
                if (auth.AuthenticateClient(clientId.Value, clientSecret))
                    ret = true;
                else
                    result = Forbidden("auth_client", "authentication_failure");
            }
            else
                result = BadRequest("auth_client", "clientId or clientSecret is not in correct format");

            return ret;
        }
    }

    public class AuthTokenRequest
    {
        /// <summary>
        /// Lalalala
        /// </summary>
        [Required, StringRange("client_credentials", "refresh_token")]     
        public string GrantType { get; set; }

        [RequiredIf("GrantType", "client_credentials")]
        public string Scope { get; set; }

        [RequiredIf("GrantType", "client_credentials")]
        public string ClientId { get; set; }

        [RequiredIf("GrantType", "client_credentials")]
        public string ClientSecret { get; set; }

        [Base64, RequiredIf("GrantType", "refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class AuthTokenResponse
    {
        [Base64]
        public string AccessToken { get; set; }

        public string TokenType { get; } = "Bearer";

        public int ExpiresIn { get; set; }

        [Base64]
        public string RefreshToken { get; set; }
    }

}

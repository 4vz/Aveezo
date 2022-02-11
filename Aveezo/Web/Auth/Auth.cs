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
        [NoAuth]
        public Method<AuthTokenResponse> Begin(
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
        [NoAuth]
        public Method<AuthTokenResponse> GetToken(
            [Body] AuthTokenRequest request
            )
        {
            var auth = Service<IAuthService>();

            var grantType = request.GrantType;

            if (grantType == "client_credentials")
            {
                if (TokenAuthClient("auth_token_client_credentials", request.ClientId, request.ClientSecret, out Guid? clientId, out Method<AuthTokenResponse> authClientResult))
                {
                    if (auth.AuthenticateClientScope(clientId.Value, request.Scope, out Guid[] validScope))
                    {
                        string parameters = null;
                        var rparameters = request.Parameters;

                        if (Options.AuthAvailableParameters != null)
                        {
                            if (rparameters != null)
                            {
                                List<string> li = new();

                                var pars = rparameters.Split(';', StringSplitOptions.RemoveEmptyEntries);

                                foreach (var par in pars)
                                {
                                    var pair = par.Split('=', 2);

                                    var key = pair[0];
                                    var value = pair[1];

                                    if (Options.AuthAvailableParameters.Contains(key))
                                    {
                                        li.Add($"{key}={value}");
                                    }
                                }

                                if (li.Count > 0)
                                    parameters = li.Join(";");
                                else
                                    parameters = null;
                            }
                        }

                        // create session
                        if (auth.CreateSession(clientId.Value, null, validScope, parameters, out var sessionId))
                        {
                            // create access token
                            var accessToken = auth.CreateAccessToken(sessionId.Value, out var refreshToken);

                            if (accessToken != null)
                            {
                                var response = new AuthTokenResponse
                                {
                                    AccessToken = accessToken,
                                    RefreshToken = $"{Base64.UrlEncode(sessionId.Value)}:{refreshToken}",
                                    ExpiresIn = Options.AuthAccessTokenExpire
                                };

                                return Ok(response);
                            }
                            else
                                return Unavailable($"accessToken from CreateAccessToken is null. SessionId: {sessionId}");
                        }
                        else
                            return Unavailable("Failed to create sessionId.");                   
                    }
                    else
                        return NotFound("auth_token_client_credentials", "INVALID_SCOPE", "Requested scope is invalid");
                }
                else 
                    return authClientResult;
            }
            else if (grantType == "refresh_token")
            {
                if (request.RefreshToken != null)
                {
                    var ix = request.RefreshToken.Split(':');

                    if (ix.Length == 2)
                    {
                        var b64sid = ix[0];
                        var refreshToken = ix[1];

                        if (Base64.TryUrlGuidDecode(b64sid, out Guid? sid))
                        {
                            var sessionId = sid.Value;

                            if (auth.AuthenticateRefresh(sessionId, refreshToken))
                            {
                                // create new access token
                                var accessToken = auth.CreateAccessToken(sessionId, out var newRefreshToken);

                                if (accessToken != null)
                                {
                                    var response = new AuthTokenResponse
                                    {
                                        AccessToken = accessToken,
                                        RefreshToken = $"{b64sid}:{newRefreshToken}",
                                        ExpiresIn = Options.AuthAccessTokenExpire
                                    };

                                    return Ok(response);
                                }
                                else
                                    return Unavailable($"accessToken from CreateAccessToken is null. SessionId: {sessionId}");
                            }
                            else
                                return Forbidden("auth_token_refresh_token", "REFRESH_TOKEN_FAILED", "Specified refresh token cannot be used");
                        }
                        else
                            return Forbidden("auth_token_refresh_token", "REFRESH_TOKEN_INVALID", "Invalid specified refresh token");
                    }
                    else
                        return Forbidden("auth_token_refresh_token", "REFRESH_TOKEN_INVALID", "Invalid specified refresh token");
                }
                else
                    return Forbidden("auth_token_refresh_token", "REFRESH_TOKEN_INVALID", "Invalid specified refresh token");
            }
            else
                return BadRequest("auth_token_", "GRANT_TYPE_INVALID", "type is not supported");
        }

#if DEBUG
        [Get("/getsecret")]
        [Sql]
        [NoAuth]
        public Method<string> GetSecret(string guid)
        {
            var d = Sql.Select("cl_secret").From("client").Where("cl_id", guid);

            if (d.Execute(out SqlRow row))
            {
                return Base64.UrlEncode(row[0].GetByteArray());
            }
            else
                return NotFound();
        }
#endif

        private bool TokenAuthClient(string source, string base64ClientId, string base64ClientSecret, out Guid? clientId, out Method<AuthTokenResponse> result)
        {
            clientId = null;
            result = null;
            
            var ret = false;
            var auth = Service<IAuthService>();

            if (Base64.TryUrlGuidDecode(base64ClientId, out clientId) && Base64.TryUrlDecode(base64ClientSecret, out string clientSecret))
            {
                if (auth.AuthenticateClient(clientId.Value, clientSecret))
                {
                    clientId = clientId.Value;
                    ret = true;
                }
                else
                    result = Forbidden(source, "AUTH_FAILURE", "authentication_failure");
            }
            else
            {
                result = BadRequest(source, "AUTH_BAD_REQUEST", $"[{nameof(clientId)}] or [{nameof(clientSecret)}] is not in correct format");
            }
                

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

        [RequiredIf("GrantType", "refresh_token")]
        public string RefreshToken { get; set; }

        public string Parameters { get; set; }
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

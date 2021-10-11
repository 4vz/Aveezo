using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System.Runtime.CompilerServices;

namespace Aveezo
{
    public interface IAuthService
    {
        public AuthTokenResponse Authenticate(AuthTokenRequest authRequest, out string error);

        public bool Validate(string token);

        public bool AuthenticateClient(Guid clientId, string clientSecret);

        public bool AuthenticateClientScope(Guid clientId, string scope, out (string, Guid)[] validScopes);

        public bool AuthenticateRefresh(string refreshToken, out Guid? refreshId, out Guid? sessionId, out bool expired);

        public Guid CreateSession(Guid clientId, Guid? identityId, Guid[] scopes);

        public (Guid, string)[] GetSessionScope(Guid sessionId);

        public void UpdateSessionScope(Guid sessionId, Guid[] scopes);

        public string CreateRefreshToken(Guid sessionId, Guid? refreshId);

        public string CreateAccessToken(Guid sessionId, string scope);
    }

    public class AuthService : ApiService, IAuthService
    {
        #region Fields

        private readonly SymmetricSecurityKey key;

        private readonly JwtSecurityTokenHandler tokenHandler;

        #endregion

        #region Constructors

        public AuthService(IServiceProvider provider) : base(provider)
        {
            key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.AuthSecret));

            tokenHandler = new JwtSecurityTokenHandler();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check whether the clientId and clientSecret is correct.
        /// </summary>
        public bool AuthenticateClient(Guid clientId, string clientSecret)
        {
            if (clientSecret != null)
            {
                var bytesClientSecret = clientSecret.ToBytes();

                if (bytesClientSecret.Length == 32)
                {
                    var q = Sql.Select("cl_id", "cl_secret").From("client").Where("cl_id", clientId);

                    if (q.Execute(out SqlRow row))
                    {
                        var secret = row["cl_secret"].GetByteArray();

                        if (secret.SequenceEqual(bytesClientSecret))
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
                return false;
        }

        /// <summary>
        /// Check whether the clientId have authorization to specified scope request, 
        /// validScope will be the allowed.
        /// </summary>
        public bool AuthenticateClientScope(Guid clientId, string scope, out (string, Guid)[] validScopes)
        {
            validScopes = null;

            if (string.IsNullOrEmpty(scope))
                return false;
            else
            {
                var scopeNames = scope.ToLower().Split(Collections.Space, StringSplitOptions.RemoveEmptyEntries);

                SqlTable clientScope = "clientscope";
                SqlTable sc = "scope";

                var q = Sql.Select(sc["sc_name"], sc["sc_id"]).From(clientScope)
                    .Join(sc, sc["sc_id"], clientScope["cs_sc"])
                    .Where(clientScope["cs_cl"], clientId).And(sc["sc_name"] == scopeNames);

                if (q.Execute(out SqlResult res))
                {
                    validScopes = res.ToList<string, Guid>("sc_name", "sc_id").ToArray();
                    return true;
                }
                else
                    return false;                
            }
        }

        /// <summary>
        /// Get sessionId by specified refreshToken.
        /// </summary>
        public bool AuthenticateRefresh(string refreshToken, out Guid? refreshId, out Guid? sessionId, out bool expired)
        {
            refreshId = null;
            sessionId = null;
            expired = true;            

            if (Base64.TryUrlGuidDecode(refreshToken, out refreshId))
            {
                if (Sql.Select("re_se", "re_used").From("refresh").Join("session", "re_se", "se_id").Where("re_id", refreshId).Execute(out SqlRow row))
                {
                    if (row["re_used"].IsNull)
                        expired = false;

                    sessionId = row["re_se"].GetGuid();

                    return true;
                }
                else 
                    return false;
            }
            else 
                return false;
        }

        //
        /// <summary>
        /// Create new session
        /// </summary>
        public Guid CreateSession(Guid clientId, Guid? identityId, Guid[] scopes)
        {
            // insert session in the database
            var q1 = Sql.Insert("session", "se_id", "se_cl", "se_it", "se_created")
                .Values(out Guid seid, clientId, identityId, DateTime.UtcNow)
                .Execute();

            if (q1)
            {
                // id is session id
                foreach (var scope in scopes)
                {
                    Sql.Insert("sessionscope", "ss_id", "ss_se", "ss_sc")
                        .Values(out Guid _, seid, scope)
                        .Execute();
                }
            }

            return seid;
        }

        public void UpdateSessionScope(Guid sessionId, Guid[] scopes)
        {
            var q1 = Sql.Select("ss_id", "ss_sc").From("sessionscope").Where("ss_se", sessionId).Execute();     

            if (q1)
            {
                var qd = Sql.Delete("sessionscope", "ss_id");
                var qi = Sql.Insert("sessionscope", "ss_id", "ss_se", "ss_sc");

                q1.First.ToList<Guid, Guid>("ss_id", "ss_sc").Cast().Diff(scopes, 1, 
                    delete => { qd.Where((Guid)delete[0]); }, 
                    add => { qi.Values(out Guid _, sessionId, add); });

                qd.Execute();
                qi.Execute();
            }
        }

        public (Guid, string)[] GetSessionScope(Guid sessionId)
        {
            var r = Sql.SelectFrom("sessionscope").Join("scope", "ss_sc", "sc_id").Where("ss_se", sessionId).Execute();
            return r.First.ToList<Guid, string>("ss_id", "sc_name").ToArray();
        }

        /// <summary>
        /// Create new refresh token for specified sessionId
        /// </summary>
        public string CreateRefreshToken(Guid sessionId, Guid? refreshId)
        {
            if (refreshId != null)
            {
                Sql.Update("refresh").Set(s => s["re_used"] = DateTime.UtcNow).Where("re_id", refreshId.Value).Execute();
            }

            Sql.Insert("refresh", "re_id", "re_se")
                .Values(out Guid reid, sessionId)
                .Execute();

            return Base64.UrlEncode(reid);
        }

        /// <summary>
        /// Create new access token by specified sessionId
        /// </summary>
        public string CreateAccessToken(Guid sessionId, string scope)
        {
            if (Sql.SelectFrom("session").Where("se_id", sessionId).Execute(out SqlRow row))
            {
                var claims = new Dictionary<string, string>();

                claims.Add("jti", Base64.UrlEncode(Guid.NewGuid()));

                // identity id
                if (!row["se_it"].IsNull)
                    claims.Add("sub", Base64.UrlEncode(row["se_cl"].GetGuid()));

                // client id
                claims.Add("aud", Base64.UrlEncode(row["se_cl"].GetGuid()));

                // scope
                claims.Add("sco", scope);

                var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims.ToList(pair => new Claim(pair.Key, pair.Value)).ToArray()),
                    Expires = DateTime.UtcNow.AddSeconds(Options.AuthAccessTokenExpire),
                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
                }));

                return accessToken;
            }
            else
                return null;
        }

        public AuthTokenResponse Authenticate(AuthTokenRequest request, out string error)
        {
            error = null;

            AuthTokenResponse info = null;

            var grantType = request.GrantType;

            if (grantType == "client_credentials")
            {
                #region client_credentials

                var base64ClientId = request.ClientId;
                var base64ClientSecret = request.ClientSecret;

                string strClientId = Base64.UrlDecode(base64ClientId);

                //var clientId = Guid.TryParse(strClientId);



                //if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                //{
                //    error = "invalid_grant";
                //    errorDescription = "username and password are required";
                //}
                //else
                //{
                //    var id = sql.Select("ident", "id_id", "id_password", "id_salt");
                //    id.Where = (SqlColumn)"id_name" == username;

                //    var r = id.Execute();

                //    if (r.First.Count == 1)
                //    {
                //        var ro = r.First.First;

                //        var spass = ro["id_password"].GetByteArray();
                //        var ssalt = ro["id_salt"].GetByteArray();

                //        if (spass == null || ssalt == null)
                //        {
                //            error = "invalid_request";
                //            errorDescription = "server:spass_ssalt_null";
                //        }
                //        else
                //        {
                //            var hash = Hash.SHA512(ssalt.Concat(password.ToBytes()));

                //            if (!hash.SequenceEqual(spass))
                //            {
                //                error = "invalid_grant";
                //                errorDescription = "invalid username or password";
                //            }
                //            else
                //            {
                //                info = CreateAuthInfo(ro["id_id"].GetGuid());
                //            }
                //        }
                //    }
                //    else
                //    {
                //        error = "invalid_grant";
                //        errorDescription = "invalid username or password";
                //    }

                #endregion
            }
            else if (string.IsNullOrEmpty(grantType))
                error = "invalid_grant";
            else
                error = "unsupported_grant_type";

            return info;
        }

        public bool Validate(string token)
        {
            bool ok = false;

            try
            { 
                var key = Encoding.ASCII.GetBytes(Options.AuthSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,                    
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                ok = true;

                //foreach (var claim in jwtToken.Claims)
                //{
                //    if (claim.Type == "id") id = claim.Value;
                //    else if (claim.Type == "name") name = claim.Value;
                //}

                // attach user to context on successful jwt validation
                //context.Items["Auth"] = id;
            }
            catch
            {
                // do nothing if jwt validation fails
                // user is not attached to context so request won't have access to secure routes
            }

            return ok;
        }

        #endregion

        #region Statics

        #endregion
    }
}

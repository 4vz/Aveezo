using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System.Runtime.CompilerServices;
using LitJWT;
using LitJWT.Algorithms;
using System.Security.Cryptography;
using System.IO;

namespace Aveezo;

internal interface IAuthService
{
    public AuthTokenResponse Authenticate(AuthTokenRequest authRequest, out string error);

    public bool Validate(string token, out string scopes, out string parameters);

    public bool AuthenticateClient(Guid clientId, string clientSecret);

    public bool AuthenticateClientScope(Guid clientId, string requestScope, out Guid[] validClientScopes);

    public bool CreateSession(Guid clientId, Guid? identityId, Guid[] clientScopes, string parameters, out Guid? sessionId);

    public string CreateAccessToken(Guid sessionId, out string refreshToken);

    public bool AuthenticateRefresh(Guid sessionId, string refreshToken);
}

internal class AuthService : ApiService, IAuthService
{
    #region Fields

    private readonly JwtEncoder encoder;

    private readonly JwtDecoder decoder;

    private byte[] iv = new byte[16]
    {
        0xDD, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC,
        0xBB, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC
    };

    private readonly byte[] key = new byte[32]
    {
        0xBB, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC,
        0xBB, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC,
        0xBB, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC,
        0xBB, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC
    };

    #endregion

    #region Constructors

    public AuthService(IServiceProvider provider) : base(provider)
    {
        encoder = new JwtEncoder(new HS256Algorithm(Encoding.UTF8.GetBytes(Options.AuthSecret)));
        decoder = new JwtDecoder(encoder.SignAlgorithm);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Authenticate request.
    /// </summary>
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

    /// <summary>
    /// Validate accessToken.
    /// </summary>
    public bool Validate(string accessToken, out string scopes, out string parameters)
    {
        bool ok = false;
        scopes = null;
        parameters = null;

        try
        {
            var result = decoder.TryDecode(accessToken, o => Utf8Json.JsonSerializer.Deserialize<AuthJwtPayload>(o.ToArray()), out var payload);

            if (result == DecodeResult.Success)
            {
                scopes = payload.Scope;
                
                if (payload.Parameters != null && Base64.TryUrlDecode(payload.Parameters, out byte[] dp))
                {
                    if (Decrypt.TryAes(dp, key, iv, out var decrypted))
                    {
                        parameters = decrypted.ToUTF8String();
                    }
                }

                ok = true;
            }
        }
        catch
        {
            // do nothing if jwt validation fails
            // user is not attached to context so request won't have access to secure routes
        }

        return ok;
    }

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
    public bool AuthenticateClientScope(Guid clientId, string requestScope, out Guid[] validClientScopes)
    {
        validClientScopes = null;

        if (string.IsNullOrEmpty(requestScope))
            return false;
        else
        {
            var scopeNames = requestScope.ToLower().Split(Collections.Space, StringSplitOptions.RemoveEmptyEntries);

            var (cs, sc) = Sql["clientscope", "scope"];

            var q = Sql.Select(cs["cs_id"]).From(cs)
                .Join(sc, sc["sc_id"], cs["cs_sc"])
                .Where(cs["cs_cl"], clientId).And(sc["sc_name"] == scopeNames);

            if (q.Execute(out SqlResult res))
            {
                validClientScopes = res.ToList<Guid>("cs_id").ToArray();
                return true;
            }
            else
                return false;
        }
    }

    /// <summary>
    /// Create new session
    /// </summary>
    public bool CreateSession(Guid clientId, Guid? identityId, Guid[] clientScopes, string parameters, out Guid? sessionId)
    {
        sessionId = null;

        // create scopes string by requested clientScopes
        var (sc, cs) = Sql["scope", "clientscope"];
        var qsc = Sql.Select(sc["sc_name"], cs["cs_level"]).From(cs).Join(sc, cs["cs_sc"], sc["sc_id"]).Where(cs["cs_id"], clientScopes);

        if (qsc.Execute(out SqlResult qscr))
        {
            var scopes = qscr.ToList<string, int>("sc_name", "cs_level").ToArray().Invoke(o => $"{o.Item1}:{o.Item2}").Join();

            string hiddenParameters = null;

            if (parameters != null)
            {
                var bytes = Encrypt.Aes(Encoding.UTF8.GetBytes(parameters), key, iv);
                hiddenParameters = Base64.UrlEncode(bytes);
            }

            // insert session in the database
            var q1 = Sql.Insert("session", "se_id", "se_cl", "se_it", "se_created", "se_scopes", "se_parameters")
                .Values(out Guid seid, clientId, identityId, DateTime.UtcNow, scopes, hiddenParameters)
                .Execute();

            sessionId = seid;

            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// Create new access token by specified sessionId
    /// </summary>
    public string CreateAccessToken(Guid sessionId, out string refreshToken)
    {
        refreshToken = null;

        if (Sql.SelectFrom("session").Where("se_id", sessionId).Execute(out SqlRow row))
        {
            // create new refreshToken
            var refresh = Rnd.String(8, Collections.WordDigit);
            Sql.Update("session").Set(s => s["se_refresh"] = refresh).Where("se_id", sessionId).Execute();

            refreshToken = refresh;

            // create payload
            var payload = new AuthJwtPayload
            {
                Id = Base64.UrlEncode(Guid.NewGuid()),                
                ClientId = Base64.UrlEncode(row["se_cl"].GetGuid()),
                Scope = row["se_scopes"],
                Parameters = row["se_parameters"]
            };

            // create accessToken
            var accessToken = encoder.Encode(
                payload,
                TimeSpan.FromMinutes(5),
                (o, writer) => writer.Write(Utf8Json.JsonSerializer.SerializeUnsafe(o, Utf8Json.Resolvers.StandardResolver.ExcludeNull)));

            return accessToken;
        }
        else
            return null;
    }

    /// <summary>
    /// Get sessionId by specified refreshToken.
    /// </summary>
    public bool AuthenticateRefresh(Guid sessionId, string refreshToken)
    {
        if (refreshToken.Length == 8 && Sql.Select("se_refresh").From("session").Where("se_id", sessionId).Execute(out SqlRow row))
        {
            if (row["se_refresh"] == refreshToken)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// FOR FUTURE REFERENCE
    /// </summary>
    public void UpdateSessionScope(Guid sessionId, Guid[] scopes)
    {
        var q1 = Sql.Select("ss_id", "ss_sc").From("sessionscope").Where("ss_se", sessionId).Execute();

        if (q1)
        {
            var qd = Sql.Delete("sessionscope", "ss_id");
            var qi = Sql.Insert("sessionscope", "ss_id", "ss_se", "ss_sc");

            q1.First.ToList<Guid, Guid>("ss_id", "ss_sc").ToITuple().Diff(scopes, 1,
                delete => { qd.Where((Guid)delete[0]); },
                add => { qi.Values(out Guid _, sessionId, add); });

            qd.Execute();
            qi.Execute();
        }
    }

    #endregion

    #region Statics

    #endregion
}

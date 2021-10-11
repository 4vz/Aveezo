using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class Base64
    {
        public static string UrlToNormal(string str) => str.Replace('-', '+').Replace('_', '/').Append('=', str.Length % 4 > 0 ? 4 - (str.Length % 4) : 0);

        public static string NormalToUrl(string str) => str.Replace('+', '-').Replace('/', '_').TrimEnd('=');

        #region Check

        public static bool Is(string str) => Is(str, out _);

        public static bool Is(string str, out bool? isUrl)
        {
            isUrl = null;

            if (str == null) throw new NullReferenceException();
            if (str.Length == 0) return false;

            var ret = true;
            int trailing = 0;

            foreach (char c in str)
            {
                if (c == '+' || c == '/')
                {
                    if (isUrl == null) isUrl = false;
                    else if (isUrl == true || trailing > 0)
                    {
                        ret = false;
                        break;
                    }
                }
                else if (c == '_' || c == '-')
                {
                    if (isUrl == null) isUrl = true;
                    else if (isUrl == false || trailing > 0)
                    {
                        ret = false;
                        break;
                    }
                }
                else if (c == '=')
                {
                    trailing++;
                    isUrl = false;
                    if (isUrl == true || trailing > 2)
                    {
                        ret = false;
                        break;
                    }
                }
                else if (Collections.WordDigit.Contains(c))
                {
                    if (trailing > 0)
                    {
                        ret = false;
                        break;
                    }
                }
                else
                {
                    ret = false;
                    break;
                }
            }

            if (ret == true)
            {
                if (isUrl == false && trailing > 0 && str.Length % 4 > 0)
                    ret = false;
                else if ((isUrl == null || isUrl == true) && str.Length % 4 == 1)
                    ret = false;
            }

            return ret;
        }

        public static bool IsUrl(string str) => Is(str, out var isUrl) && (isUrl == null || isUrl == true);

        #endregion

        #region Decode

        public static byte[] DecodeToBytes(string str, bool urlEncoded) => Convert.FromBase64String(urlEncoded ? UrlToNormal(str) : str); 

        public static byte[] DecodeToBytes(string str) => DecodeToBytes(str, false);

        public static byte[] UrlDecodeToBytes(string str) => DecodeToBytes(str, true);

        public static string Decode(string str, bool urlEncoded) => DecodeToBytes(str, urlEncoded).ToUTF8String(); 

        public static string Decode(string str) => DecodeToBytes(str, false).ToUTF8String();

        public static string UrlDecode(string str) => Decode(str, true);

        private static bool TryDecode(string str, bool? urlEncoded, out string decoded)
        {
            decoded = null;

            if (str == null)
                return false;

            var ret = false;

            if (Is(str, out var isUrl))
            {
                if (urlEncoded != null && isUrl != null && urlEncoded.Value != isUrl.Value) { }
                else if (isUrl == null || isUrl == true)
                    decoded = Decode(str, true);
                else
                    decoded = Decode(str, false);

                ret = true;
            }

            return ret;
        }

        public static bool TryDecode(string str, out string decoded) => TryDecode(str, null, out decoded);

        public static bool TryUrlDecode(string str, out string decoded) => TryDecode(str, true, out decoded);

        public static bool TryUrlDecode(string str, out string decoded, StringChecks decodedChecks) => TryDecode(str, true, out decoded) && decoded.Is(decodedChecks);

        // GUID

        public static Guid GuidDecode(string str, bool urlEncoded)
        {
            var guidBytes = DecodeToBytes(str, urlEncoded);

            if (guidBytes.Length == 16)
                return new Guid(guidBytes);
            else
            {
                var destr = guidBytes.ToUTF8String();
                if (destr.IsGuid())
                    return new Guid(destr);
                else
                    throw new ArgumentException("Incorrect byte size (or string format) for Guid", nameof(str));
            }
        }

        public static Guid GuidDecode(string str) => GuidDecode(str, false);

        public static Guid UrlGuidDecode(string str) => GuidDecode(str, true);

        public static bool TryGuidDecode(string str, bool urlEncoded, out Guid? guid)
        {
            try
            {
                guid = GuidDecode(str, urlEncoded);
                return true;
            }
            catch
            {
                guid = null;
                return false;
            }
        }

        public static bool TryGuidDecode(string str, out Guid? guid) => TryGuidDecode(str, false, out guid);

        public static bool TryUrlGuidDecode(string str, out Guid? guid) => TryGuidDecode(str, true, out guid);

        #endregion

        #region Encode

        public static string Encode(byte[] bytes, bool urlEncoded)
        {
            var encoded = Convert.ToBase64String(bytes);

            if (urlEncoded)
                return NormalToUrl(encoded);
            else
                return encoded;
        }

        public static string Encode(string str) => Encode(str.ToBytes(), false);

        public static string UrlEncode(byte[] bytes) => Encode(bytes, true);

        public static string UrlEncode(string str) => Encode(str.ToBytes(), true);

        public static string Encode(Guid guid, bool urlEncoded) => Encode(guid.ToByteArray(), urlEncoded);

        public static string Encode(Guid guid) => Encode(guid, false);

        public static string UrlEncode(Guid guid) => Encode(guid, true);

        #endregion
    }
}

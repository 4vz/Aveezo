using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Aveezo
{
    public static unsafe class ByteExtensions
    {
        public static bool SequenceEqual(this byte[] b1, byte[] to)
        {
            if (to == null) return false;
            else
                return ByteArrayCompare(b1, to);
        }

        // byte[] is implicitly convertible to ReadOnlySpan<byte>
        private static bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
        {
            return a1.SequenceEqual(a2);
        }

        public static bool StartsWith(this byte[] b1, byte[] with)
        {
            var compare = new byte[with.Length];
            Buffer.BlockCopy(b1, 0, compare, 0, with.Length);
            return compare.SequenceEqual(with);
        }

        private static readonly uint[] _lookup32Unsafe = CreateLookup32Unsafe();
        private static readonly uint* _lookup32UnsafeP = (uint*)GCHandle.Alloc(_lookup32Unsafe, GCHandleType.Pinned).AddrOfPinnedObject();

        private static uint[] CreateLookup32Unsafe()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                if (BitConverter.IsLittleEndian)
                    result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
                else
                    result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
            }
            return result;
        }

        public static string ToHex(this byte[] bytes)
        {
            var lookupP = _lookup32UnsafeP;
            var result = new char[bytes.Length * 2];
            fixed (byte* bytesP = bytes)
            fixed (char* resultP = result)
            {
                uint* resultP2 = (uint*)resultP;
                for (int i = 0; i < bytes.Length; i++)
                {
                    resultP2[i] = lookupP[bytesP[i]];
                }
            }
            return new string(result);
        }

        public static byte[] Concat(this byte[] b1, byte[] b2)
        {
            return Combine(b1, b2).ToArray();
        }

        private static IEnumerable<byte> Combine(byte[] a1, byte[] a2)
        {
            foreach (byte b in a1)
                yield return b;
            foreach (byte b in a2)
                yield return b;
        }

        public static string ToUTF8String(this byte[] b) => Encoding.UTF8.GetString(b);

        public static string ToASCIIString(this byte[] b) => Encoding.ASCII.GetString(b);
    }
}

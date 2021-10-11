using System;
using System.Text;
using System.Threading;

namespace Aveezo
{
    public static class Rnd
    {
        [ThreadStatic]
        private static Random random;

        private static Random Seed => random ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));

        public static int Int()
        { 
            return Seed.Next();
        }

        public static int Natural()
        {
            return Seed.Next(0, int.MaxValue);
        }

        public static int Int(int maxValue)
        {
            return Seed.Next(maxValue);
        }

        public static int Int(int minValue, int maxValue)
        {
            return Seed.Next(minValue, maxValue);
        }

        public static double Double()
        {
            return Seed.NextDouble();
        }

        public static void Bytes(byte[] buffer)
        {
            Seed.NextBytes(buffer);
        }

        public static string String(int length, string characters)
        {
            var sb = new StringBuilder();
            
            for (var i = 0; i < length; i++)
            {
                sb.Append(characters[Int(0, characters.Length - 1)]);
            }

            return sb.ToString();
        }

        public static string String(int length) => String(length, Collections.Printable);
    }
}

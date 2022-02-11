using Microsoft.OpenApi.Any;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public enum ExampleUseCase
    {
        None,
        Custom,
        Base64
    }

    public class DocumentationExample
    {

        private static bool[] BoolExample => new bool[] { false, true, false };
        private static sbyte[] SbyteExample => new sbyte[] { 88, 127, -64 };
        private static byte[] ByteExample => new byte[] { 32, 64, 128 };
        private static short[] ShortExample => new short[] { -32768, 1234, 10004 };
        private static ushort[] UshortExample => new ushort[] { 4321, 40001, 65535 };
        private static int[] IntExample => new[] { 1234, 456789, -10111213 };
        private static uint[] UintExample => new[] { 123U, 456789U, 4000000000U };
        private static long[] LongExample => new[] { 1251231254212L, 423215123413L, -9223372036854775808L };
        private static ulong[] UlongExample => new[] { 32421431UL, 421521263965657547UL, 18446744073709551615UL };
        private static char[] CharExample => new[] { 'a', 'b', 'c' };
        private static float[] FloatExample => new[] { -19.87F, 20.14F, 120.005F };
        private static double[] DoubleExample => new[] { -4.21951D, 199.9999D, 52303.2D };
        private static decimal[] DecimalExample => new[] { -1567.21951M, 4010.777312M, 1490220.302033M };
        private static DateTime[] DateTimeExample => new[] { DateTime.Now, DateTime.Now - new TimeSpan(3650, 0, 0, 0), DateTime.Now + new TimeSpan(3650, 0, 0, 0) };
        private static DateTimeOffset[] DateTimeOffsetExample => new[] { DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(1)), DateTimeOffset.UtcNow.Subtract(new TimeSpan(3650, 0, 0, 0)).ToOffset(TimeSpan.FromHours(4)), DateTimeOffset.Now.Add(new TimeSpan(3650, 0, 0, 0)) };
        private static TimeSpan[] TimeSpanExample => new[] { TimeSpan.FromMinutes(342.3213), TimeSpan.FromHours(4423.12314), TimeSpan.FromDays(242450.3252324) };
        private static string[] StringExample => new[] { "ExampleString1", "ExampleString2", "ExampleString3" };
        private static Guid[] GuidExample => new[] { "6d74262d-1a4f-4743-8c89-adc209d1e1b7".ToGuid(), "9089597a-7bac-4073-9f9e-e777b62dbb42".ToGuid(), "ad0f4b24-9ff4-4edf-8f8a-27c8e581b143".ToGuid() };
        private static BitArray[] BitArrayExample => new[] { "01011110 00010111 10101".ToBitArray(), "01011110 00".ToBitArray(), "11101".ToBitArray() };
        private static PhysicalAddress[] PhysicalAddressExample => new[] { new PhysicalAddress(new byte[] { 145, 42, 55, 55, 12, 255 }), new PhysicalAddress(new byte[] { 165, 242, 155, 155, 44, 15 }), new PhysicalAddress(new byte[] { 95, 77, 75, 25, 145, 159 }) };
        private static IPAddressCidr[] IPAddressCidrExample => new[] { IPAddressCidr.Parse("192.168.100.1/24"), IPAddressCidr.Parse("10.50.192.4/16"), IPAddressCidr.Parse("168.152.33.44/30") };
        private static IPAddress[] IPAddressExample => new[] { IPAddress.Parse("192.168.200.42"), IPAddress.Parse("10.192.162.30"), IPAddress.Parse("99.81.65.188") };


        private static int internalCounter = 0;



        // use case examples
        private static string[] Base64Example => new[] { "YWZpcyBnYW50ZW5n", "dHNhbWluYW1pbmEga2VrZQ==", "anVhbm5jb29vb2trCg==" };

        // main
        public static string[] KeyExamples(Type type, ExampleUseCase useCase, int counter)
        {
            string[] keys = null;
            if (type == typeof(bool)) keys = BoolExample.Invoke((bool o) => o.ToString());
            else if (type == typeof(sbyte)) keys = SbyteExample.Invoke((sbyte o) => o.ToString());
            else if (type == typeof(byte)) keys = ByteExample.Invoke((byte o) => o.ToString());
            else if (type == typeof(short)) keys = ShortExample.Invoke((short o) => o.ToString());
            else if (type == typeof(ushort)) keys = UshortExample.Invoke((ushort o) => o.ToString());
            else if (type == typeof(int)) keys = IntExample.Invoke((int o) => o.ToString());
            else if (type == typeof(uint)) keys = UintExample.Invoke((uint o) => o.ToString());
            else if (type == typeof(long)) keys = LongExample.Invoke((long o) => o.ToString());
            else if (type == typeof(ulong)) keys = UlongExample.Invoke((ulong o) => o.ToString());
            else if (type == typeof(char)) keys = CharExample.Invoke((char o) => o.ToString());
            else if (type == typeof(float)) keys = FloatExample.Invoke((float o) => o.ToString());
            else if (type == typeof(double)) keys = DoubleExample.Invoke((double o) => o.ToString());
            else if (type == typeof(decimal)) keys = DecimalExample.Invoke((decimal o) => o.ToString());
            else if (type == typeof(DateTime)) keys = DateTimeExample.Invoke(o => o.ToString("o"));
            else if (type == typeof(DateTimeOffset)) keys = DateTimeOffsetExample.Invoke(o => o.ToString("o"));
            else if (type == typeof(TimeSpan)) keys = TimeSpanExample.Invoke(o => o.ToISO8601());
            else if (type == typeof(string))
            {
                if (useCase == ExampleUseCase.Base64) keys = Base64Example;
                else keys = StringExample;
            }
            else if (type == typeof(Guid)) keys = GuidExample.Invoke((Guid o) => o.ToString());
            else if (type == typeof(BitArray)) keys = BitArrayExample.Invoke(o => o.ToString('0', '1'));
            else if (type == typeof(PhysicalAddress)) keys = PhysicalAddressExample.Invoke((PhysicalAddress o) => o.ToString());
            else if (type == typeof(IPAddressCidr)) keys = IPAddressCidrExample.Invoke((IPAddressCidr o) => o.ToString());
            else if (type == typeof(IPAddress)) keys = IPAddressExample.Invoke((IPAddress o) => o.ToString());
            else keys = null;

            if (counter == -1 || keys == null)
                return keys;
            else
                return keys[counter % keys.Length].Array();
        }
        public static IOpenApiAny[] ValueExamples(Type type, ExampleUseCase useCase, int counter)
        {
            IOpenApiAny[] values = null;
            if (type == typeof(bool)) values = BoolExample.Invoke(o => new OpenApiBoolean(o));
            else if (type == typeof(sbyte)) values = SbyteExample.Invoke(o => new OpenApiInteger(o));
            else if (type == typeof(byte)) values = ByteExample.Invoke(o => new OpenApiInteger(o));
            else if (type == typeof(short)) values = ShortExample.Invoke(o => new OpenApiInteger(o));
            else if (type == typeof(ushort)) values = UshortExample.Invoke(o => new OpenApiInteger(o));
            else if (type == typeof(int)) values = IntExample.Invoke(o => new OpenApiInteger(o));
            else if (type == typeof(uint)) values = UintExample.Invoke(o => new OpenApiLong(o));
            else if (type == typeof(long)) values = LongExample.Invoke(o => new OpenApiLong(o));
            else if (type == typeof(ulong)) values = UlongExample.Invoke((ulong o) => new OpenApiString(o.ToString()));
            else if (type == typeof(char)) values = CharExample.Invoke((char o) => new OpenApiString(o.ToString()));
            else if (type == typeof(float)) values = FloatExample.Invoke(o => new OpenApiFloat(o));
            else if (type == typeof(double)) values = DoubleExample.Invoke(o => new OpenApiDouble(o));
            else if (type == typeof(decimal)) values = DecimalExample.Invoke((decimal o) => new OpenApiString(o.ToString()));
            else if (type == typeof(DateTime)) values = DateTimeExample.Invoke(o => new OpenApiDateTime(o));
            else if (type == typeof(DateTimeOffset)) values = DateTimeOffsetExample.Invoke(o => new OpenApiDateTime(o));
            else if (type == typeof(TimeSpan)) values = TimeSpanExample.Invoke(o => new OpenApiString(o.ToISO8601()));
            else if (type == typeof(string))
            {
                if (useCase == ExampleUseCase.Base64) values = Base64Example.Invoke(o => new OpenApiString(o));
                else values = StringExample.Invoke(o => new OpenApiString(o));
            }
            else if (type == typeof(Guid)) values = GuidExample.Invoke((Guid o) => new OpenApiString(o.ToString()));
            else if (type == typeof(BitArray)) values = BitArrayExample.Invoke(o => new OpenApiString(o.ToString('0', '1')));
            else if (type == typeof(PhysicalAddress)) values = PhysicalAddressExample.Invoke(o => new OpenApiString(o.ToString(":")));
            else if (type == typeof(IPAddressCidr)) values = IPAddressCidrExample.Invoke((IPAddressCidr o) => new OpenApiString(o.ToString()));
            else if (type == typeof(IPAddress)) values = IPAddressExample.Invoke((IPAddress o) => new OpenApiString(o.ToString()));
            else values = null;

            if (counter == -1 || values == null)
                return values;
            else
                return values[counter % values.Length].Array();
        }

        public static string[] CustomKeyExamples(Type type, object[] example)
        {
            string[] keys = null;

            try
            {
                if (type == typeof(bool)) keys = example.Invoke(o => ((bool)o).ToString());
                else if (type == typeof(sbyte)) keys = example.Invoke(o => ((sbyte)o).ToString());
                else if (type == typeof(byte)) keys = example.Invoke(o => ((byte)o).ToString());
                else if (type == typeof(short)) keys = example.Invoke(o => ((short)o).ToString());
                else if (type == typeof(ushort)) keys = example.Invoke(o => ((ushort)o).ToString());
                else if (type == typeof(int)) keys = example.Invoke(o => ((int)o).ToString());
                else if (type == typeof(uint)) keys = example.Invoke(o => ((uint)o).ToString());
                else if (type == typeof(long)) keys = example.Invoke(o => ((long)o).ToString());
                else if (type == typeof(ulong)) keys = example.Invoke(o => ((ulong)o).ToString());
                else if (type == typeof(char)) keys = example.Invoke(o => ((char)o).ToString());
                else if (type == typeof(float)) keys = example.Invoke(o => ((float)o).ToString());
                else if (type == typeof(double)) keys = example.Invoke(o => ((double)o).ToString());
                else if (type == typeof(decimal)) keys = example.Invoke(o => ((decimal)o).ToString());
                else if (type == typeof(DateTime)) keys = example.Invoke(o => ((DateTime)o).ToString("o"));
                else if (type == typeof(DateTimeOffset)) keys = example.Invoke(o => ((DateTimeOffset)o).ToString("o"));
                else if (type == typeof(TimeSpan)) keys = example.Invoke(o => ((TimeSpan)o).ToISO8601());
                else if (type == typeof(string)) keys = example.Invoke(o => (string)o);
                else if (type == typeof(Guid)) keys = example.Invoke(o => ((Guid)o).ToString());
                else if (type == typeof(BitArray)) keys = example.Invoke(o => ((BitArray)o).ToString('0', '1'));
                else if (type == typeof(PhysicalAddress)) keys = example.Invoke(o => ((PhysicalAddress)o).ToString());
                else if (type == typeof(IPAddressCidr)) keys = example.Invoke(o => ((IPAddressCidr)o).ToString());
                else if (type == typeof(IPAddress)) keys = example.Invoke(o => ((IPAddress)o).ToString());
                else keys = null;
            }
            catch
            {
            }

            if (keys == null)
                keys = KeyExamples(type, ExampleUseCase.None);

            return keys;
        } 

        public static IOpenApiAny[] CustomValueExamples(Type type, object[] example)
        {
            IOpenApiAny[] values = null;

            try
            {
                if (type == typeof(bool)) values = example.Invoke(o => new OpenApiBoolean((bool)o));
                else if (type == typeof(sbyte)) values = example.Invoke(o => new OpenApiInteger((sbyte)o));
                else if (type == typeof(byte)) values = example.Invoke(o => new OpenApiInteger((byte)o));
                else if (type == typeof(short)) values = example.Invoke(o => new OpenApiInteger((short)o));
                else if (type == typeof(ushort)) values = example.Invoke(o => new OpenApiInteger((ushort)o));
                else if (type == typeof(int)) values = example.Invoke(o => new OpenApiInteger((int)o));
                else if (type == typeof(uint)) values = example.Invoke(o => new OpenApiLong((uint)o));
                else if (type == typeof(long)) values = example.Invoke(o => new OpenApiLong((long)o));
                else if (type == typeof(ulong)) values = example.Invoke(o => new OpenApiString(o.ToString()));
                else if (type == typeof(char)) values = example.Invoke(o => new OpenApiString(o.ToString()));
                else if (type == typeof(float)) values = example.Invoke(o => new OpenApiFloat((float)o));
                else if (type == typeof(double)) values = example.Invoke(o => new OpenApiDouble((double)o));
                else if (type == typeof(decimal)) values = example.Invoke(o => new OpenApiString(o.ToString()));
                else if (type == typeof(DateTime)) values = example.Invoke(o => new OpenApiDateTime((DateTime)o));
                else if (type == typeof(DateTimeOffset)) values = example.Invoke(o => new OpenApiDateTime((DateTimeOffset)o));
                else if (type == typeof(TimeSpan)) values = example.Invoke(o => new OpenApiString(((TimeSpan)o).ToISO8601()));
                else if (type == typeof(string)) values = example.Invoke(o => new OpenApiString((string)o));
                else if (type == typeof(Guid)) values = example.Invoke(o => new OpenApiString(o.ToString()));
                else if (type == typeof(BitArray)) values = example.Invoke(o => new OpenApiString(((BitArray)o).ToString('0', '1')));
                else if (type == typeof(PhysicalAddress)) values = example.Invoke(o => new OpenApiString(((PhysicalAddress)o).ToString(":")));
                else if (type == typeof(IPAddressCidr)) values = example.Invoke(o => new OpenApiString(o.ToString()));
                else if (type == typeof(IPAddress)) values = example.Invoke(o => new OpenApiString(o.ToString()));
                else values = example.Invoke(o => new OpenApiString(o.ToString()));
            }
            catch
            {
            }

            if (values == null)
                values = ValueExamples(type, ExampleUseCase.None);

            return values;
        }

        // overloads
        public static string[] KeyExamples(Type type) => KeyExamples(type, ExampleUseCase.None);
        public static string[] KeyExamples(Type type, ExampleUseCase useCase) => KeyExamples(type, useCase, -1);
        public static string KeyExample(Type type) => KeyExamples(type, ExampleUseCase.None, internalCounter++)?[0];
        public static string KeyExample(Type type, ExampleUseCase useCase) => KeyExamples(type, useCase, internalCounter++)?[0];
        public static IOpenApiAny[] ValueExamples(Type type) => ValueExamples(type, ExampleUseCase.None);
        public static IOpenApiAny[] ValueExamples(Type type, ExampleUseCase useCase) => ValueExamples(type, useCase, -1);
        public static IOpenApiAny ValueExample(Type type) => ValueExamples(type, ExampleUseCase.None, internalCounter++)?[0];
        public static IOpenApiAny ValueExample(Type type, ExampleUseCase useCase) => ValueExamples(type, useCase, internalCounter++)?[0];

    }
}

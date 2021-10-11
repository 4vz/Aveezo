using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{

    public sealed class SqlCell : IPrintable
    {
        #region Fields

        private object value;

        private bool isNumeric;

        private bool isArray = false;

        private Type elementType;

        public Type Type { get; }

        public bool IsNull { get; }

        #endregion

        #region Constructor

        internal SqlCell(Type type, object value)
        {
            Type = type;
            this.value = value;
            IsNull = value == null;

            if (Type.IsNumeric()) isNumeric = true;
            else if (Type.IsArray)
            {
                isArray = true;
                elementType = Type.GetElementType();
            }   

        }

        #endregion

        #region Methods

        #region Boolean

        public bool? GetNullableBool() => IsNull || Type != typeof(bool) ? null : (bool)value;
        public bool GetBool() => GetNullableBool() ?? default;
        public static implicit operator bool?(SqlCell d) => d.GetNullableBool();
        public static implicit operator bool(SqlCell d) => d.GetBool();

        #endregion

        #region Numeric

        private T? GetNullableNumericType<T>() where T : struct => IsNull || !isNumeric ? null : Type == typeof(T) ? (T)value : value.TryCast<T>(out var toValue) ? toValue : null;

        public sbyte? GetNullableSbyte() => GetNullableNumericType<sbyte>();
        public sbyte GetSbyte() => GetNullableSbyte() ?? default;
        public static implicit operator sbyte?(SqlCell d) => d.GetNullableSbyte();
        public static implicit operator sbyte(SqlCell d) => d.GetSbyte();

        public byte? GetNullableByte() => GetNullableNumericType<byte>();
        public byte GetByte() => GetNullableByte() ?? default;
        public static implicit operator byte?(SqlCell d) => d.GetNullableByte();
        public static implicit operator byte(SqlCell d) => d.GetByte();

        public short? GetNullableShort() => GetNullableNumericType<sbyte>();
        public short GetShort() => GetNullableShort() ?? default;
        public static implicit operator short?(SqlCell d) => d.GetNullableShort();
        public static implicit operator short(SqlCell d) => d.GetShort();

        public ushort? GetNullableUshort() => GetNullableNumericType<ushort>();
        public ushort GetUshort() => GetNullableUshort() ?? default;
        public static implicit operator ushort?(SqlCell d) => d.GetNullableUshort();
        public static implicit operator ushort(SqlCell d) => d.GetUshort();

        public int? GetNullableInt() => GetNullableNumericType<int>();
        public int GetInt() => GetNullableInt() ?? default;
        public static implicit operator int?(SqlCell d) => d.GetNullableInt();
        public static implicit operator int(SqlCell d) => d.GetInt();

        public uint? GetNullableUint() => GetNullableNumericType<uint>();
        public uint GetUint() => GetNullableUint() ?? default;
        public static implicit operator uint?(SqlCell d) => d.GetNullableUint();
        public static implicit operator uint(SqlCell d) => d.GetUint();

        public long? GetNullableLong() => GetNullableNumericType<long>();
        public long GetLong() => GetNullableLong() ?? default;
        public static implicit operator long?(SqlCell d) => d.GetNullableLong();
        public static implicit operator long(SqlCell d) => d.GetLong();

        public ulong? GetNullableUlong() => GetNullableNumericType<ulong>();
        public ulong GetUlong() => GetNullableUlong() ?? default;
        public static implicit operator ulong?(SqlCell d) => d.GetNullableUlong();
        public static implicit operator ulong(SqlCell d) => d.GetUlong();

        public char? GetNullableChar() => Type == typeof(string) ? GetString()?[0] : GetNullableNumericType<char>();
        public char GetChar() => GetNullableChar() ?? default;
        public static implicit operator char?(SqlCell d) => d.GetNullableChar();
        public static implicit operator char(SqlCell d) => d.GetChar();

        public float? GetNullableFloat() => GetNullableNumericType<float>();
        public float GetFloat() => GetNullableFloat() ?? default;
        public static implicit operator float?(SqlCell d) => d.GetNullableFloat();
        public static implicit operator float(SqlCell d) => d.GetFloat();

        public double? GetNullableDouble() => GetNullableNumericType<double>();
        public double GetDouble() => GetNullableDouble() ?? default;
        public static implicit operator double?(SqlCell d) => d.GetNullableDouble();
        public static implicit operator double(SqlCell d) => d.GetDouble();

        public decimal? GetNullableDecimal() => GetNullableNumericType<decimal>();
        public decimal GetDecimal() => GetNullableDecimal() ?? default;
        public static implicit operator decimal?(SqlCell d) => d.GetNullableDecimal();
        public static implicit operator decimal(SqlCell d) => d.GetDecimal();

        #endregion

        #region DateTime

        public DateTimeOffset? GetNullableDateTimeOffset()
        {
            if (IsNull) return null;
            else if (Type == typeof(DateTimeOffset)) return (DateTimeOffset)value;
            else if (Type == typeof(DateTime))
            {
                var dateTime = (DateTime)value;
                if (dateTime.Kind == DateTimeKind.Unspecified)
                    return new DateTimeOffset((DateTime)value, TimeSpan.Zero);
                else
                    return dateTime;
            }
            else
                return default;
        }
        public DateTimeOffset GetDateTimeOffset() => GetNullableDateTimeOffset() ?? default;
        public static implicit operator DateTimeOffset?(SqlCell d) => d.GetNullableDateTimeOffset();
        public static implicit operator DateTimeOffset(SqlCell d) => d.GetDateTimeOffset();

        public DateTime? GetNullableDateTime()
        {
            if (IsNull) return null;
            else if (Type == typeof(DateTime)) return (DateTime)value;
            else if (Type == typeof(DateTimeOffset))
            {
                var dateTimeOffset = (DateTimeOffset)value;
                return dateTimeOffset.DateTime;
            }
            else if (Type == typeof(TimeSpan))
            {
                var timeSpan = (TimeSpan)value;
                return DateTime.MinValue + timeSpan;
            }
            else return default;
        }
        public DateTime GetDateTime() => GetNullableDateTime() ?? default;
        public static implicit operator DateTime?(SqlCell d) => d.GetNullableDateTime();
        public static implicit operator DateTime(SqlCell d) => d.GetDateTime();

        public TimeSpan? GetNullableTimeSpan()
        {
            if (IsNull) return default;
            else if (Type == typeof(TimeSpan))
                return (TimeSpan)value;
            else
                return default;
        }
        public TimeSpan GetTimeSpan() => GetNullableTimeSpan() ?? default;
        public static implicit operator TimeSpan?(SqlCell d) => d.GetNullableTimeSpan();
        public static implicit operator TimeSpan(SqlCell d) => d.GetTimeSpan();

        #endregion

        #region String

        public string GetString()
        {
            if (IsNull)
                return null;
            else if (Type == typeof(string))
                return (string)value;
            else if (Type == typeof(char))
                return ((char)value).ToString();
            else if (Type.IsNumeric())
                return value.ToString();
            else if (isArray)
            {
                if (elementType == typeof(byte))
                    return GetByteArray().ToHex();
                else
                    return GetStringArray().Join(',');
            }
            else
                return value.ToString();
        }
        public static implicit operator string(SqlCell d) => d.GetString();

        #endregion

        #region Guid

        public Guid? GetNullableGuid() => IsNull ? null : (Guid)value;
        public Guid GetGuid() => GetNullableGuid() ?? default;
        public static implicit operator Guid?(SqlCell d) => d.GetNullableGuid();
        public static implicit operator Guid(SqlCell d) => d.GetGuid();

        #endregion

        #region Bit
        
        public BitArray GetBitArray() => IsNull ? null : Type == typeof(BitArray) ? (BitArray)value : default;
        public static implicit operator BitArray(SqlCell d) => d.GetBitArray();

        #endregion

        #region Net

        public PhysicalAddress GetPhysicalAddress() => GetExactTypeValue<PhysicalAddress>();
        public static implicit operator PhysicalAddress(SqlCell d) => d.GetPhysicalAddress();

        public IPAddressCidr GetIPAddressCidr() => GetExactTypeValue<IPAddressCidr>() ?? GetExactTypeValue<IPAddress>()?.ToCidr() ?? IPAddressCidr.Parse(GetString());
        public static implicit operator IPAddressCidr(SqlCell d) => d.GetIPAddressCidr();

        public IPAddress GetIPAddress()
        {
            var s = GetExactTypeValue<IPAddress>();
            if (s == null) s = GetExactTypeValue<IPAddressCidr>();
            if (s == null && IPAddress.TryParse(GetString(), out var ipAddress)) s = ipAddress;
            if (s == null && IPAddressCidr.TryParse(GetString(), out var ipAddressCidr)) s = ipAddressCidr;
            return s;
        }
        public static implicit operator IPAddress(SqlCell d) => d.GetIPAddress();

        #endregion

        #region Array  

        public T[] GetArray<T>(Func<object, T> cast)
        {
            if (IsNull)
                return null;
            else if (isArray)
                return ((Array)value).Cast(cast);
            else
                return null;
        }

        public object[] GetArray() => GetArray(o => o);
        public static implicit operator object[](SqlCell d) => d.GetArray();

        public byte[] GetByteArray() => GetArray(v => (byte)v);
        public static implicit operator byte[](SqlCell d) => d.GetByteArray();

        public int[] GetIntArray() => GetArray(v => (int)v);
        public static implicit operator int[](SqlCell d) => d.GetIntArray();

        public string[] GetStringArray() => GetArray(v => v.ToString());
        public static implicit operator string[](SqlCell d) => d.GetStringArray();

        #endregion

        #region Object

        public T Get<T>() 
        {
            var to = typeof(T);
            object val = null;

            if (IsNull)
                return default;
            else
            {
                // numeric
                if (to == typeof(sbyte))
                    val = GetNullableSbyte();
                else if (to == typeof(byte))
                    val = GetNullableByte();
                else if (to == typeof(short))
                    val = GetNullableShort();
                else if (to == typeof(ushort))
                    val = GetNullableUshort();
                else if (to == typeof(int))
                    val = GetNullableInt();
                else if (to == typeof(uint))
                    val = GetNullableUint();
                else if (to == typeof(long))
                    val = GetNullableLong();
                else if (to == typeof(ulong))
                    val = GetNullableUlong();
                else if (to == typeof(float))
                    val = GetNullableFloat();
                else if (to == typeof(double))
                    val = GetNullableDouble();
                else if (to == typeof(decimal))
                    val = GetNullableDecimal();
                else
                {
                    if (Type == to)
                        val = (T)value;
                }
            }

            return (T)val;
        }

        private T GetExactTypeValue<T>() where T : class => IsNull || Type != typeof(T) ? null : (T)value;

        public object GetObject() => value;

        #endregion

        public string[] Print() => new []{ $"{(IsNull ? "<null>" : GetString())}" };

        public override string ToString() => GetString();

        #endregion
    }
}
 
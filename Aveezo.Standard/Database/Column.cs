using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [Serializable]
    public sealed class Column
    {
        #region Fields

        private object value;

        public bool IsNull { get; private set; }

        #endregion

        #region Constructor

        public Column(object value, bool isNull)
        {
            this.value = value;
            IsNull = isNull;
        }

        #endregion

        #region Methods

        private T GetValue<T>(T ifNull)
        {
            if (IsNull) return ifNull;
            else
            {
                if (value.GetType() == typeof(T)) return (T)value;
                else return default;
            }
        }

        private T GetValue<T>()
        {
            return GetValue<T>(default);
        }

        public object ToObject()
        {
            return ToObject(null);
        }

        public object ToObject(object ifNull)
        {
            if (IsNull) return ifNull;
            else return value;
        }

        public override string ToString()
        {
            if (IsNull) return null;
            else
            {
                if (value.GetType() == typeof(string))
                    return GetValue<string>();
                else
                    return value.ToString();
            }
        }

        public string ToString(string ifNull)
        {
            if (IsNull) return ifNull;
            else return ToString();
        }

        public static implicit operator string(Column d) => d.ToString();

        public char ToChar()
        {
            return GetValue<string>()[0];
        }

        public char ToChar(char ifNull)
        {
            if (IsNull) return ifNull;
            else return ToChar();
        }

        public char? ToNullableChar()
        {
            if (IsNull) return null;
            else return new char?(ToChar());
        }

        public static implicit operator char(Column d) => d.ToChar();

        public static implicit operator char?(Column d) => d.ToNullableChar();

        public long ToLong()
        {
            if (value.GetType() == typeof(long))
                return GetValue<long>();
            else if (value.GetType() == typeof(int))
                return GetValue<int>();
            else if (value.GetType() == typeof(short))
                return GetValue<short>();
            else if (value.GetType() == typeof(decimal))
                return (int)GetValue<decimal>();
            else
                return 0;
        }

        public long ToLong(long ifNull)
        {
            if (IsNull) return ifNull;
            else return ToLong();
        }

        public long? ToNullableLong()
        {
            if (IsNull) return null;
            else return new long?(ToLong());
        }

        public static implicit operator long(Column d) => d.ToLong();

        public static implicit operator long?(Column d) => d.ToNullableLong();

        public int ToInt()
        {
            if (value.GetType() == typeof(int))
                return GetValue<int>();
            else if (value.GetType() == typeof(short))
                return GetValue<short>();
            else if (value.GetType() == typeof(byte))
                return (int)GetValue<byte>();
            else if (value.GetType() == typeof(decimal))
                return (int)GetValue<decimal>();
            else
                return 0;
        }

        public int ToInt(int ifNull)
        {
            if (IsNull) return ifNull;
            else return ToInt();
        }

        public int? ToNullableInt()
        {
            if (IsNull) return null;
            else return new int?(ToInt());
        }

        public static implicit operator int(Column d) => d.ToInt();

        public static implicit operator int?(Column d) => d.ToNullableInt();

        public double ToDouble()
        {
            if (value.GetType() == typeof(decimal))
                return (double)GetValue<decimal>();
            else
                return 0;
        }

        public double ToDouble(double ifNull)
        {
            if (IsNull) return ifNull;
            else return ToDouble();
        }

        public double? ToNullableDouble()
        {
            if (IsNull) return null;
            else return new double?(ToDouble());
        }

        public static implicit operator double(Column d) => d.ToDouble();

        public static implicit operator double?(Column d) => d.ToNullableDouble();

        public float ToFloat()
        {
            if (value.GetType() == typeof(double))
                return (float)GetValue<double>();
            else
                return 0;
        }

        public float ToFloat(float ifNull)
        {
            if (IsNull) return ifNull;
            else return ToFloat();
        }

        public float? ToNullableFloat()
        {
            if (IsNull) return null;
            else return new float?(ToFloat());
        }

        public static implicit operator float(Column d) => d.ToFloat();

        public static implicit operator float?(Column d) => d.ToNullableFloat();

        public bool ToBool()
        {
            if (value.GetType() == typeof(bool))
                return GetValue<bool>();
            else
                return false;
        }

        public bool ToBool(bool ifNull)
        {
            if (IsNull) return ifNull;
            else return ToBool();
        }

        public bool? ToNullableBool()
        {
            if (IsNull) return null;
            else return new bool?(ToBool());
        }
               
        public static implicit operator bool(Column d) => d.ToBool();

        public static implicit operator bool?(Column d) => d.ToNullableBool();

        public DateTime ToDateTime()
        {
            return GetValue<DateTime>();
        }

        public DateTime ToDateTime(DateTime ifNull)
        {
            if (IsNull) return ifNull;
            else return ToDateTime();
        }

        public DateTime? ToNullableDateTime()
        {
            if (IsNull) return null;
            else return new DateTime?(ToDateTime());
        }

        public static implicit operator DateTime(Column d) => d.ToDateTime();

        public static implicit operator DateTime?(Column d) => d.ToNullableDateTime();

        // obsolete

        public int ToIntShort()
        {
            if (value.GetType() == typeof(short))
                return GetValue<short>();
            else if (value.GetType() == typeof(decimal))
                return (int)GetValue<decimal>();
            else
                return 0;
        }

        public int ToIntShort(int ifNull)
        {
            if (IsNull) return ifNull;
            else return ToIntShort();
        }

        public int ToIntByte()
        {
            if (value.GetType() == typeof(byte))
                return (int)GetValue<byte>();
            else
                return 0;
        }

        public int ToIntByte(int ifNull)
        {
            if (IsNull) return ifNull;
            else return ToIntByte();
        }

        #endregion
    }
}

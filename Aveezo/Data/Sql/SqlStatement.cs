using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class SqlStatement
    {
        #region Fields

        private readonly StringBuilder builder = new();

        public string Value => builder.ToString();

        #endregion

        #region Constructors

        public SqlStatement()
        {
        }

        public SqlStatement(string str) => builder.Append(str);

        #endregion

        #region Operators

        public static implicit operator string(SqlStatement str) => str.ToString();

        public static implicit operator SqlStatement(string str) => new(str);

        public static SqlStatement operator +(SqlStatement text1, SqlStatement text2) => text1.Append(text2);

        #endregion

        #region Methods

        public void Clear() => builder.Clear();

        public SqlStatement Append(string str)
        {
            if (str != null)
            {
                if (builder.Length > 0)
                {
                    if (!builder.ToString().EndsWith(' '))
                        builder.Append(' ');
                }

                builder.Append(str);
            }

            return this;
        }

        public override string ToString()
        {
            return builder.ToString();
        }

        #endregion

        #region Statics

        #endregion
    }
}

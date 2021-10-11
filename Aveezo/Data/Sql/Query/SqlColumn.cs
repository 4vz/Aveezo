using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public enum SqlColumnOperation
    {
        Concat
    }

    public class SqlColumn : SqlColumnBase
    {
        #region Fields

        public SqlTable Table { get; internal set; }

        public string Name { get; }

        public bool All { get; }

        public string Alias { get; set; }

        internal SqlColumnBase[] ConcatColumns { get; }

        #endregion

        #region Constructors

        public SqlColumn(SqlTable table, string name)
        {
            Table = table;
            Name = name;
            All = false;
        }

        public SqlColumn(SqlTable table)
        {
            Table = table;
            All = true;
        }

        public SqlColumn(SqlColumnOperation operation, SqlColumnBase[] args)
        {
            if (operation == SqlColumnOperation.Concat)
                ConcatColumns = args;
        }

        #endregion

        #region Operators

        public static implicit operator SqlColumn(string name)
        {
            return new SqlColumn(null, name);
        }

        #endregion

        #region Methods
        public override string ToString() => $"{Alias ?? ($"{(Table != null ? $"{Table}." : "")}{(All ? " *" : Name)}")   }";

        #endregion

        #region Statics

        public static SqlColumn Concat(string alias, params SqlColumnBase[] args)
        {
            var column = new SqlColumn(SqlColumnOperation.Concat, args);
            column.Alias = alias;
            return column;
        }


        #endregion
    }

    public class SqlColumnBase : SqlCondition
    {
        #region Fields

        public new object Value { get; }

        #endregion

        #region Constructors

        internal SqlColumnBase(object value)
        {
            Value = value;
        }

        public SqlColumnBase()
        {

        }

        #endregion

        #region Operators

        public static implicit operator SqlColumnBase(string value) => new SqlColumnBase(value);

        public static implicit operator SqlColumnBase(int value) => new SqlColumnBase(value);

        public static implicit operator SqlColumnBase(DateTime value) => new SqlColumnBase(value);

        #endregion

        #region Methods

        #endregion

        #region Statics

        #endregion
    }
}

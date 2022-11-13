using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo;

public enum SqlColumnOperation
{
    None,
    Concat
}

public class SqlColumn : SqlCondition
{
    #region Fields

    public SqlTable Table { get; internal set; }

    public string Name { get; } = null;

    public string Alias { get; set; } = null;

    public bool IsValue { get; internal set; } = false;

    public bool IsAll { get; } = false;

    public new object Value { get; internal set; } = null;

    /// <summary>
    /// Column alias if available, otherwise returns column name;
    /// </summary>
    public string Ident => Alias ?? Name;

    public SqlColumnOperation Operation { get; } = SqlColumnOperation.None;

    public SqlColumn[] OperationColumns { get; }

    #endregion

    #region Constructors

    public SqlColumn(SqlTable table, string name, string alias)
    {
        Table = table;
        Name = name;
        Alias = alias;
        IsAll = false;
    }

    public SqlColumn(SqlTable table, string name)
    {
        Table = table;
        Name = name;
        IsAll = false;
    }

    public SqlColumn(SqlTable table)
    {
        Table = table;
        IsAll = true;
    }

    private SqlColumn(SqlColumnOperation operation, SqlColumn[] columns)
    {
        Operation = operation;
        OperationColumns = columns;
    }

    #endregion

    #region Operators

    public static implicit operator SqlColumn(string name)
    {
        return new SqlColumn(null, name);
    }

    public static implicit operator SqlColumn(int value) => Static(value);

    public static implicit operator SqlColumn(DateTime value) => Static(value);

    #endregion

    #region Statics

    public static SqlColumn Concat(string alias, params SqlColumn[] columns)
    {
        var column = new SqlColumn(SqlColumnOperation.Concat, columns)
        {
            Alias = alias
        };
        return column;
    }

    public static SqlColumn Static(object value)
    {
        var col = new SqlColumn(null, "", null)
        {
            IsValue = true,
            Value = value
        };
        return col;
    }

    public static SqlColumn Static(object value, string alias)
    {
        var col = new SqlColumn(null, "", alias)
        {
            IsValue = true,
            Value = value
        };
        return col;
    }

    /// <summary>
    /// Only used for SqlColumn select, not for condition.
    /// </summary>
    public static SqlColumn Null => Static(null);

    /// <summary>
    /// Only used for SqlColumn select, not for condition.
    /// </summary>
    public static SqlColumn Empty => Static("");

    #endregion
}

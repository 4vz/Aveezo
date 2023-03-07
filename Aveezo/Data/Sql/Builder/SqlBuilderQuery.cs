using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

public sealed class SqlBuilderQuery
{
    #region Fields

    private Dictionary<string, SqlColumn> builderColumns;

    public SqlQueryType Type { get; }

    public string Query { get; }

    private string name;

    public SqlColumn this[string name] => builderColumns.ContainsKey(name) ? builderColumns[name] : null;

    #endregion

    #region Constructors

    internal SqlBuilderQuery(Dictionary<string, SqlColumn> builderColumns, SqlQueryType type, string query, string name)
    {
        this.builderColumns = builderColumns;
        Type = type;
        Query = query;
        this.name = name;
    }

    #endregion

    #region Methods

    public SqlCondition Equal(object value)
    {
        return this[name] == value;
    }

    #endregion

    #region Operators

    public static implicit operator SqlColumn(SqlBuilderQuery query) => query[query.name];

    #endregion

    #region Statics

    /// <summary>
    /// Function hostBuilder for query parameter in select hostBuilder using query value pair, with default options: StringConvertOptions.ToLower.
    /// </summary>
    public static Func<SqlBuilderQuery, SqlCondition> Pair<T>(params (Values<string>, T)[] values) => Pair(StringConvertOptions.ToLower, values);

    /// <summary>
    /// Function hostBuilder for query parameter in select hostBuilder.
    /// </summary>
    public static Func<SqlBuilderQuery, SqlCondition> Pair<T>(StringConvertOptions queryConvertOptions, params (Values<string>, T)[] values)
    {
        return o =>
        {
            var query = o.Query.Convert(queryConvertOptions);

            bool yes = false;
            T withThis = default;

            foreach (var (choices, comp) in values)
            {
                foreach (var choice in choices)
                {
                    if (query == choice)
                    {
                        yes = true;
                        break;
                    }
                }

                if (yes)
                {
                    withThis = comp;
                    break;
                }
            }

            if (yes)
            {
                return ((SqlColumn)o) == withThis;
            }
            else
                return null;
        };
    }

    /// <summary>
    /// Function hostBuilder for query parameter in select hostBuilder using equality with specified string format, {0} as query input.
    /// </summary>
    public static Func<SqlBuilderQuery, SqlCondition> Format(string format)
    {
        return
        o => o.Equal(o.Query.Format(format));

    }

    #endregion
}

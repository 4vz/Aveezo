using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

public class SqlBuilderFormatter
{
    #region Fields

    protected Dictionary<string, SqlCell> values;

    protected string name;

    public SqlCell this[string name] => values.ContainsKey(name) ? values[name] : null;

    public SqlCell This => this[name];

    #endregion

    #region Constructors

    internal SqlBuilderFormatter(Dictionary<string, SqlCell> values, string name)
    {
        this.values = values;
        this.name = name;
    }

    #endregion

    #region Operators

    public static implicit operator SqlCell(SqlBuilderFormatter formatter) => formatter.This;

    #endregion

    #region Statics

    /// <summary>
    /// Function builder for formatter parameter in select builder using query value pair.
    /// </summary>
    public static Func<SqlBuilderFormatter, object> Pair<T>(object defaultValue, params (T, object)[] values)
    {
        return o =>
        {
            var select = o.This.Get<T>();
            object found = null;

            foreach (var (key, value) in values)
            {
                if (Equals(key, select))
                {
                    found = value;
                    break;
                }
            }

            if (found == null)
                found = defaultValue;

            return found;
        };
    }

    #endregion
}

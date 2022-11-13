using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

public sealed class SqlBuilder
{
    #region Fields

    public string Name { get; init; }

    public SqlColumn Column { get; init; } = SqlColumn.Null;

    public Func<Dictionary<string, SqlCell>, object> Formatter { get; init; }

    public Action<object, Dictionary<string, SqlCell>, Dictionary<string, object>> Binder { get; init; }

    public Action<SqlSelect> Select { get; init; }

    public Func<SqlBuilderQuery, SqlCondition> Query { get; init; }

    public Values<string> Requires { get; init; }

    public Func<Dictionary<string, object>, object> Ext { get; init; }

    #endregion
}


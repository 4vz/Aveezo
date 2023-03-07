using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

public class SqlTableDefinition
{
    #region Fields

    public SqlColumnDefinition[] Columns { get; init; }

    #endregion
}

public class SqlColumnDefinition
{
    #region Fields

    public string Name { get; init; }

    public bool IsNullable { get; init; }

    public bool IsPrimaryKey { get; init; }

    #endregion
}
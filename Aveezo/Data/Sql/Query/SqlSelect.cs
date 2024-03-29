﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

using System.Text;

namespace Aveezo;

public sealed class SqlSelectProto : SqlBase
{
    #region Fields

    public SqlSelectOptions Options { get; set; }

    public SqlColumn[] SelectColumns { get; set; }

    #endregion

    #region Constructors

    internal SqlSelectProto(Sql database, SqlSelectOptions options, params SqlColumn[] columns) : base(database)
    {
        Options = options;
        SelectColumns = columns.Length == 0 ? null : columns;
    }

    #endregion

    #region Methods

    public SqlSelect From(SqlTable table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));

        var select = new SqlSelect(Database, table);

        Sql.SetTableWhenUnassigned(SelectColumns, table);

        select.SelectColumns = SelectColumns;
        select.Options = Options;

        return select;
    }

    public SqlSelect From(SqlSelect select)
    {
        if (select == null) throw new ArgumentNullException(nameof(select));

        var outerSelect = new SqlSelect(Database, select);

        Sql.SetTableWhenUnassigned(SelectColumns, outerSelect.Table);

        outerSelect.SelectColumns = SelectColumns;
        outerSelect.Options = Options;

        return outerSelect;
    }

    #endregion
}

public class SqlSelect : SqlQueryBase
{
    #region Fields

    private readonly SqlSelect innerSelect = null;

    private readonly List<SqlJoin> joins = new();

    private readonly List<SqlTable> joinTables = new();

    public Dictionary<string, SqlColumn> filters = new();

    private int limit = 0;

    private int offset = 0;

    public int LimitLength
    {
        get => limit;
        set
        {
            if (value >= 0) limit = value;
        }
    }

    public int OffsetLength
    {
        get => offset;
        set
        {
            if (value >= 0) offset = value;
        }
    }

    public SqlCondition WhereCondition { get; set; } = null;

    internal bool after = false;

    public SqlOrder Order { get; set; }

    public SqlSelectOptions Options { get; set; } = SqlSelectOptions.None;

    private SqlColumn[] selectColumns = null;

    public SqlColumn[] SelectColumns
    {
        get
        {
            if (selectColumns != null)
                return selectColumns;
            else if (innerSelect != null && innerSelect.SelectColumns != null)
            {
                List<SqlColumn> columns = new();

                foreach (var innerColumn in innerSelect.SelectColumns)
                    columns.Add(new SqlColumn(Table, innerColumn.Ident));

                return columns.ToArray();
            }
            else
                return null;
        }
        set
        {
            selectColumns = value;
        }
    }

    private SqlSelect[] unionAlls = null;

    internal readonly Dictionary<string, SqlBuilder> builders = new();

    private Dictionary<string, Dictionary<string, SqlBuilder>> builderCache = new();

    public bool IsBuilder => builders.Count > 0 || (innerSelect != null && innerSelect.IsBuilder);

    public SqlSelect InnerSelect => innerSelect;

    #endregion

    #region Constructors

    internal SqlSelect(Sql database, SqlTable table) : base(database, table, SqlExecuteType.Reader)
    {
    }

    internal SqlSelect(Sql database, SqlSelect select) : base(database, new SqlTable(), SqlExecuteType.Reader) => innerSelect = select;

    #endregion

    #region Methods

    public SqlSelect Join(SqlJoinType type, SqlTable table, SqlCondition where)
    {
        if (!joinTables.Contains(table))
        {
            joins.Add(new SqlJoin(type, table, where));
            joinTables.Add(table);
        }

        return this;
    }

    public SqlSelect Join(SqlJoinType type, SqlTable table, SqlColumn whereColumn, object whereValue) => Join(type, table, whereColumn == whereValue);

    public SqlSelect Join(SqlJoinType type, SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(type, table, leftColumn == rightColumn);

    public SqlSelect Join(SqlTable table, SqlCondition where) => Join(SqlJoinType.Inner, table, where);

    public SqlSelect Join(SqlTable table, SqlColumn whereColumn, object whereValue) => Join(SqlJoinType.Inner, table, whereColumn, whereValue);

    public SqlSelect Join(SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(SqlJoinType.Inner, table, leftColumn, rightColumn);

    public SqlSelect LeftJoin(SqlTable table, SqlCondition where) => Join(SqlJoinType.Left, table, where);

    public SqlSelect LeftJoin(SqlTable table, SqlColumn whereColumn, object whereValue) => Join(SqlJoinType.Left, table, whereColumn, whereValue);

    public SqlSelect LeftJoin(SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(SqlJoinType.Left, table, leftColumn, rightColumn);

    public SqlSelect RightJoin(SqlTable table, SqlCondition where) => Join(SqlJoinType.Right, table, where);

    public SqlSelect RightJoin(SqlTable table, SqlColumn whereColumn, object whereValue) => Join(SqlJoinType.Right, table, whereColumn, whereValue);

    public SqlSelect RightJoin(SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(SqlJoinType.Right, table, leftColumn, rightColumn);

    public SqlSelect FullJoin(SqlTable table, SqlCondition where) => Join(SqlJoinType.Full, table, where);

    public SqlSelect FullJoin(SqlTable table, SqlColumn whereColumn, object whereValue) => Join(SqlJoinType.Full, table, whereColumn, whereValue);

    public SqlSelect FullJoin(SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(SqlJoinType.Full, table, leftColumn, rightColumn);

    public SqlSelect Where(SqlCondition condition)
    {
        WhereCondition = condition;
        return this;
    }

    public SqlSelect And(SqlCondition condition)
    {
        if (WhereCondition is not null)
            WhereCondition = WhereCondition && condition;
        else
            throw new InvalidOperationException();

        return this;
    }

    public SqlSelect And(SqlColumn whereColumn, object whereValue) => And(whereColumn == whereValue);

    public SqlSelect Or(SqlCondition condition)
    {
        if (WhereCondition is not null)
            WhereCondition = WhereCondition || condition;
        else
            throw new InvalidOperationException();

        return this;
    }

    public SqlSelect Or(SqlColumn whereColumn, object whereValue) => Or(whereColumn == whereValue);

    public SqlSelect Where(SqlColumn whereColumn, object whereValue) => Where(whereColumn == whereValue);

    public SqlSelect OrderBy(SqlOrder order)
    {
        Order = order;
        return this;
    }

    public SqlSelect OrderBy(SqlColumn column, Order order)
    {
        Order = SqlOrder.By(column, order);
        return this;
    }

    public SqlSelect OrderBy(params (SqlColumn, Order)[] args)
    {
        var order = new SqlOrder();

        foreach (var (col, ord) in args)
        {
            order.Add(col, ord);
        }

        Order = order; 
        return this;
    }

    public SqlSelect Limit(int limit, int offset)
    {
        LimitLength = limit;
        OffsetLength = offset;
        return this;
    }

    public SqlSelect Limit(int limit) => Limit(limit, 0);

    public SqlSelect UnionAll(params SqlSelect[] selects)
    {
        // should match with SelectColumns
        var sc = SelectColumns.IfNotNull(o => o.Length, 0);

        foreach (var select in selects)
        {
            var ssc = select.SelectColumns.IfNotNull(o => o.Length, 0);
            if (ssc != sc) throw new InvalidOperationException("SelectColumns should match with main SqlSelect");
        }

        unionAlls = selects;

        // make hostBuilder keys same for every select in unionAlls
        List<string> names = new();

        // get available keys from this and all unions
        foreach (var (name, _) in builders)
        {
            names.Add(name);
        }

        foreach (var select in unionAlls)
        {
            foreach (var (name, _) in select.builders)
            {
                if (!names.Contains(name))
                    names.Add(name);
            }
        }

        // add missing keys to this and all unions
        var names1 = builders.Keys.ToArray();
        foreach (var name in names)
        {
            if (!names1.Contains(name))
            {
                builders.Add(name, new SqlBuilder());// (SqlColumn.Null, SqlBuilderOptions.None, null, null, null, null, null));
            }
        }

        foreach (var select in unionAlls)
        {
            var names2 = select.builders.Keys.ToArray();
            foreach (var name in names)
            {
                if (!names2.Contains(name))
                {
                    select.builders.Add(name, new SqlBuilder());
                }
            }
        }

        // create a new encapsulating select for these mfs
        return Database.SelectFrom(this);
    }

    private SqlTable GetTableStatement(Values<string> selectBuilders)
    {
        if (innerSelect != null)
        {
            var statement = new StringBuilder();

            statement.AppendLine(innerSelect.GetStatements(selectBuilders)[0].Trim());

            var unionAlls = innerSelect.unionAlls;

            if (unionAlls != null)
            {
                foreach (var s in unionAlls)
                {
                    var dx = s.GetStatements(selectBuilders)[0].Trim();

                    statement.AppendLine("union all");
                    statement.AppendLine(dx);
                } 
            }

            return Table.CreateStatement(statement.ToString());
        }
        else
            return Table;
    }

    /// <summary>
    /// Prepares 
    /// </summary>
    /// <typeparam name="T">Target object when queried.</typeparam>
    /// <param name="add">Add new property</param>
    public void Builder<T>(Action<SqlBuilderAdd<T>> add) where T : class
    {
        if (IsBuilder) throw new InvalidOperationException("Builder has already been created for this instance.");

        add((name, column, formatter, binder, select, query, requires, ext) =>
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (builders.ContainsKey(name)) throw new InvalidOperationException($"Builder with {name} option has already added.");

            var nocolumn = false;
            if (column is null)
            {
                nocolumn = true;
                column = SqlColumn.Static(null);
            }

            if (nocolumn && query != null) throw new ArgumentNullException(nameof(column), $"The {nameof(column)} parameter is required when specifying {nameof(query)} parameter.");

            builders.Add(name, new SqlBuilder
            {
                Name = name,
                Column = column,
                Formatter = formatter != null ? values => formatter.Invoke(new SqlBuilderFormatter(values, name)) : null,
                Binder = binder != null ? (obj, values, formattedValues) => binder.Invoke(obj as T, new SqlBuilderBinder(formattedValues, values, name)) : null,
                Select = select,
                Query = query,
                Requires = requires,
                Ext = ext != null ? formattedValues => ext(new SqlBuilderExt(formattedValues, name)) : null
            });
        });
    }

    /// <summary>
    /// Remove builders.
    /// </summary>
    /// 
    public void RemoveBuilder() => builders.Clear();

    /// <summary>
    /// Get hostBuilder list by name.
    /// </summary>
    public Dictionary<string, SqlBuilder> GetBuilderStack(string name)
    {
        if (builderCache.ContainsKey(name))
            return builderCache[name];
        else
        {
            Dictionary<string, SqlBuilder> v = null;

            if (innerSelect != null)
            {
                if (innerSelect.builders.ContainsKey(name))
                {
                    v = new();
                    v.Add(innerSelect.Table.Alias, innerSelect.builders[name]);

                    if (innerSelect.unionAlls != null)
                    {
                        foreach (var unionSelect in innerSelect.unionAlls)
                        {
                            v.Add(unionSelect.Table.Alias, unionSelect.builders[name]);
                        }
                    }
                }
            }

            if (v == null && builders.ContainsKey(name))
            {
                v = new();
                v.Add(Table.Alias, builders[name]);
            }

            builderCache.Add(name, v);

            return v;
        }
    }

    /// <summary>
    /// Get hostBuilder list by stack name.
    /// </summary>
    public Dictionary<string, SqlBuilder> GetBuildersByStack(string stack)
    {
        if (stack == null || stack == Table.Alias)
            return builders;
        else if (innerSelect != null)
        {
            if (innerSelect.Table.Alias == stack)
                return innerSelect.builders;
            else if (innerSelect.unionAlls != null)
            {
                foreach (var unionSelect in innerSelect.unionAlls)
                {
                    if (unionSelect.Table.Alias == stack)
                        return unionSelect.builders;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Get hostBuilder by name for current row.
    /// </summary>
    public SqlBuilder GetBuilder(string name, SqlRow row)
    {
        var builderStack = GetBuilderStack(name);

        if (builderStack != null && builderStack.Count > 0)
        {
            if (row != null && row.ContainsColumn("___select"))
            {
                var selectId = row["___select"].GetString();

                if (selectId != null)
                {
                    if (builderStack.ContainsKey(selectId))
                        return builderStack[selectId];
                }
            }

            return builderStack.Get(0).Item2;
        }

        return null;
    }

    /// <summary>
    /// Prepares select for query. 
    /// </summary>
    public void BuilderQuery(Dictionary<string, (SqlQueryType, Values<string>)[]> queries, out Values<string> useBuilders) 
    {
        if (!IsBuilder) throw new InvalidOperationException("This select instance is not in builder mode.");

        Dictionary<string, SqlCondition> stackConditions = new();
        
        List<string> useBuilderList = new();

        // field1 => state1, state2, field2 => state1, state2
        foreach (var (name, states) in queries)
        {
            var builderStack = GetBuilderStack(name);

            if (builderStack == null)
                continue;
            else
            {
                // name should be in selectbuilders
                useBuilderList.Add(name);
            }

            // select1, select2
            foreach (var (stack, builder) in builderStack)
            {
                SqlCondition condition = stackConditions.ContainsKey(stack) ? stackConditions[stack] : null;
                SqlCondition sumCondition = null;

                SqlColumn column = builder.Column;
                Dictionary<string, SqlColumn> columns = new();

                var stackBuilders = GetBuildersByStack(stack);

                if (stackBuilders != null)
                {
                    foreach (var (stackBuilderName, stackBuilder) in stackBuilders)
                    {
                        columns.Add(stackBuilderName, stackBuilder.Column);
                    }
                }

                foreach (var (type, query) in states)
                {
                    SqlCondition stateCondition = null;

                    // true is and, false is or
                    bool and = false;

                    if (type == SqlQueryType.NotLike || type == SqlQueryType.NotEqual)
                        and = true;

                    if (builder.Query == null)
                    {
                        //
                        // TODO check column type then convert query to column type then compare
                        //

                        foreach (var localQuery in query)
                        {
                            SqlCondition localCondition = null;

                            if (type == SqlQueryType.Equal)
                                localCondition = column == localQuery;
                            else if (type == SqlQueryType.Like)
                                localCondition = column % $"%{localQuery}%";
                            else if (type == SqlQueryType.StartsWith)
                                localCondition = column % $"{localQuery}%";
                            else if (type == SqlQueryType.EndsWith)
                                localCondition = column % $"%{localQuery}";
                            else if (type == SqlQueryType.NotLike)
                                localCondition = column ^ $"%{localQuery}%";
                            else if (type == SqlQueryType.NotEqual)
                                localCondition = column != localQuery;

                            if (localCondition is not null)
                            {
                                if (and)
                                    stateCondition = stateCondition && localCondition;
                                else
                                    stateCondition = stateCondition || localCondition;
                            }
                        }
                    }
                    else
                    {
                        foreach (var localQuery in query)
                        {
                            SqlCondition localCondition = builder.Query(new SqlBuilderQuery(columns, type, localQuery, name));

                            if (localCondition is not null)
                            {
                                if (and)
                                    stateCondition = stateCondition && localCondition;
                                else
                                    stateCondition = stateCondition || localCondition;
                            }
                        }                        
                    }

                    sumCondition = sumCondition && stateCondition;
                    
                }

                condition = condition && sumCondition;

                if (!stackConditions.ContainsKey(stack))
                    stackConditions.Add(stack, condition);
                else
                    stackConditions[stack] = condition;
            }
        } 

        foreach (var (stack, condition) in stackConditions)
        {
            if (Table.Alias == stack)
            {
                WhereCondition = condition && WhereCondition;
            }
            else if (innerSelect != null)
            {
                if (innerSelect.Table.Alias == stack)
                {
                    innerSelect.WhereCondition = condition && innerSelect.WhereCondition;
                }
                else if (innerSelect.unionAlls != null)
                {
                    foreach (var unionAll in innerSelect.unionAlls)
                    {
                        if (unionAll.Table.Alias == stack)
                        {
                            unionAll.WhereCondition = condition && unionAll.WhereCondition;
                            break;
                        }
                    }
                }
            }
        }

        if (useBuilderList.Count > 0)
            useBuilders = useBuilderList.ToArray();
        else
            useBuilders = null;
    }

    private void Extend(List<string> extSelectBuilders, string name)
    {
        if (!extSelectBuilders.Contains(name))
        {
            extSelectBuilders.Add(name);

            var builderStack = GetBuilderStack(name);

            if (builderStack != null)
            {
                foreach (var (_, builder) in builderStack)
                {
                    if (builder != null && builder.Requires != null)
                    {
                        foreach (var require in builder.Requires)
                        {
                            Extend(extSelectBuilders, require);
                        }
                    }
                }
            }
        }
    }

    protected override string[] GetStatements(Values<string> selectBuilders)
    {
        // extend selectBuilders
        if (selectBuilders != null)
        {
            List<string> extSelectBuilders = new();

            foreach (string name in selectBuilders)
            {
                Extend(extSelectBuilders, name);
            }

            selectBuilders = extSelectBuilders.ToArray();
        }

        // Get columns from my hostBuilder
        var builderColumns = new List<SqlColumn>();

        if (selectBuilders != null)
        {
            if (innerSelect != null)
            {
                builderColumns.Add(new SqlColumn(Table, "___select"));

                // only get columns
                foreach (var param in selectBuilders)
                {
                    foreach (var (name, _) in innerSelect.builders)
                    {
                        if (param == name)
                            builderColumns.Add(new SqlColumn(Table, name));
                    }
                }
            }
            else if (builders.Count > 0)
            {
                builderColumns.Add(SqlColumn.Static(Table.Alias, "___select"));

                foreach (var param in selectBuilders)
                {
                    foreach (var (name, builder) in builders)
                    {
                        if (param == name)
                        {
                            var column = builder.Column;
                            var selectColumn = new SqlColumn(column.Table, column.Name, name); 

                            if (column.IsValue)
                            {
                                selectColumn.IsValue = true;
                                selectColumn.Value = column.Value;
                            }

                            builderColumns.Add(selectColumn);
                            builder.Select?.Invoke(this);
                        }
                    }
                }
            }
        }

        // Combine predefined columns with hostBuilder columns
        var allColumnsList = new List<SqlColumn>();

        if (SelectColumns != null)
            allColumnsList.AddRange(SelectColumns);

        allColumnsList.AddRange(builderColumns);

        var allColumns = allColumnsList.ToArray();

        // Assign columns to table
        if (Table != null)
            Sql.SetTableWhenUnassigned(allColumns, Table);

        return Database.Connection.FormatSelect(
            GetTableStatement(selectBuilders),
            allColumns,
            joins.ToArray(),
            WhereCondition,
            Order, limit, offset, Options)
            .Array();
    }

    public int ExecuteCount(Values<string> selectBuilders) => ExecuteCount(selectBuilders, out _);

    public int ExecuteCount(Values<string> selectBuilders, out SqlQuery sqlQuery)
    {
        var dd = Database.Connection.FormatSelect(
            GetTableStatement(selectBuilders),
            new SqlColumn[] { "COUNT(*)" },
            joins.ToArray(),
            WhereCondition, null, 0, 0, Options);

        sqlQuery = Execute(dd);

        if (sqlQuery)
        {
            var rci = ((SqlCell)sqlQuery).GetInt();
            return rci;
        }

        return 0;
    }

    #endregion
     
    #region Statics

    public static SqlSelect UnionAlls(params SqlSelect[] selects)
    {
        if (selects.Length >= 2)
            return selects[0].UnionAll(selects[1..]);
        else if (selects.Length == 1)
            return selects[0];
        else
            return null;
    }

    #endregion
}

public delegate void SqlBuilderAdd<T>(
    string name,
    SqlColumn column = null,
    Func<SqlBuilderFormatter, object> formatter = null,
    Action<T, SqlBuilderBinder> binder = null,
    Action<SqlSelect> select = null,
    Func<SqlBuilderQuery, SqlCondition> query = null,
    Values<string> requires = null,
    Func<SqlBuilderExt, object> ext = null
    );



public class SqlBuilderExt
{
    #region Fields

    private readonly Dictionary<string, object> formattedValues;

    public object this[string name] => formattedValues.ContainsKey(name) ? formattedValues[name] : null;

    public string Id => this[Sql.Id] as string;

    public string Current => this[currentName] as string;

    private string currentName;

    #endregion

    #region Constructors

    public SqlBuilderExt(Dictionary<string, object> formattedValues, string currentName)
    {
        this.formattedValues = formattedValues;
        this.currentName = currentName;
    }

    #endregion
}

public class SqlBuilderBinder : SqlBuilderFormatter
{
    #region Fields

    private readonly Dictionary<string, object> formattedValues;

    public new object this[string name] => formattedValues.ContainsKey(name) ? formattedValues[name] : null;

    public Dictionary<string, SqlCell> Cells => values;

    #endregion

    #region Constructors

    internal SqlBuilderBinder(Dictionary<string, object> formattedValues, Dictionary<string, SqlCell> values, string name) : base(values, name)
    {
        this.formattedValues = formattedValues;

        
    }

    #endregion
}



[Flags]
public enum SqlSelectOptions
{
    None = 0,
    Distinct = 1,
    Random = 2
}

public enum SqlDataType
{
    Numeric,
    String
}

public enum SqlQueryType
{
    None,
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    StartsWith,
    EndsWith,  
    Like,   
    NotLike
}


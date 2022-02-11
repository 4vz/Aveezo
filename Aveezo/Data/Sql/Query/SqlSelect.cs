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

    private SqlCondition innerCondition = null;

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

    internal SqlSelect(Sql database, SqlTable table) : base(database, table, SqlQueryType.Reader)
    {
    }

    internal SqlSelect(Sql database, SqlSelect select) : base(database, new SqlTable(), SqlQueryType.Reader) => innerSelect = select;

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
        var sc = SelectColumns.Format(o => o.Length, 0);

        foreach (var select in selects)
        {
            var ssc = select.SelectColumns.Format(o => o.Length, 0);
            if (ssc != sc) throw new InvalidOperationException("SelectColumns should match with main SqlSelect");
        }

        unionAlls = selects;

        // make builder keys same for every select in unionAlls
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
        return Database.Select().From(this);
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

            return Table.GetStatement(statement.ToString());
        }
        else
            return Table;
    }

    /// <summary>
    /// Prepares <typeparamref name="T"/> object properties, to be called by <see cref="GetBuilderStack(string)"/> method.     
    /// </summary>
    /// <typeparam name="T">Target object when queried.</typeparam>
    /// <param name="add">Add new property</param>
    public void Builder<T>(Action<SqlSelectBuilder<T>> add) where T : class
    {
        if (IsBuilder) throw new InvalidOperationException("Builder has already been created for this instance");

        add((column, name, options, get, select, query, requires, ext) =>
        {
            if (column is null)
                throw new NullReferenceException(nameof(column));

            if (options.HasFlag(SqlBuilderOptions.Id))
            {
                if (builders.ContainsKey("___id"))
                    throw new InvalidOperationException($"Builder with {nameof(SqlBuilderOptions.Id)} option has already added.");

                name = "___id";
            }

            if (!string.IsNullOrEmpty(name) && !builders.ContainsKey(name))
            {
                if (name == "" && column.IsValue && column.Alias == null)
                    column.Alias = name;

                builders.Add(name, new SqlBuilder
                {
                    Name = name,
                    Column = column,
                    Options = options,
                    Get = get != null ? (o, c, r) => get?.Invoke(new SqlObject<T> { Object = o as T, Cell = c, Row = r }) : null,
                    Select = select,
                    Query = query,
                    Requires = requires,
                    Ext = ext != null ? o => ext(o as T) : null
                });
            }
            else
                throw new InvalidOperationException(nameof(name));
        });
    }

    public void RemoveBuilder() => builders.Clear();

    /// <summary>
    /// Get builder list by name.
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
                v.Add("___default", builders[name]);
            }

            builderCache.Add(name, v);

            return v;
        }
    }

    public Dictionary<string, SqlBuilder> GetBuilderStackId() => GetBuilderStack("___id");

    public SqlBuilder GetBuilder(string name, SqlRow row)
    {
        var builderStack = GetBuilderStack(name);

        if (builderStack != null && builderStack.Count > 0)
        {
            if (row != null && row.ContainsKey("___select"))
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

        // Get columns from my builder
        var builderColumns = new List<SqlColumn>();

        if (selectBuilders != null)
        {
            if (innerSelect != null)
            {
                builderColumns.Add(new SqlColumn(Table, "___select"));

                // only get columns
                foreach (var param in selectBuilders.Append("___id"))
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

                foreach (var param in selectBuilders.Append("___id"))
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

        // Combine predefined columns with builder columns
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
            innerCondition && WhereCondition,
            Order, limit, offset, Options)
            .Array();
    }

    public int ExecuteCount(Values<string> selectBuilders)
    {
        var dd = Database.Connection.FormatSelect(
            GetTableStatement(selectBuilders),
            new SqlColumn[] { "COUNT(*)" },
            joins.ToArray(),
            WhereCondition, null, 0, 0, Options);

        var rc = Execute(dd);

        if (rc)
        {
            var rci = ((SqlCell)rc).GetInt();
            return rci;
        }

        return 0;
    }

    #region API

    internal void SetInnerCondition(SqlCondition conditon) => innerCondition = conditon;

    internal SqlColumn GetFilterColumn(string name) => filters.ContainsKey(name) ? filters[name] : null;

    public void Filter<T>(PropFilter<T> filter, string name)
    {
    }

    public void Filter<T>(PropFilter<T> filter, SqlColumn column) => Filter(filter, column, (Func<object, object>)null);

    public void Filter<T>(PropFilter<T> filter, SqlColumn column, Func<object, object> modifier) => Filter(filter, column, new Dictionary<string, Func<object, object>> { { "___default", modifier } });

    public void Filter<T>(PropFilter<T> filter, SqlColumn column, Dictionary<string, Func<object, object>> modifiers)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (modifiers == null) throw new ArgumentNullException(nameof(modifiers));

        filters.Add(filter.Name, column);

        if (filter.Values != null)
        {
            foreach (var (attribute, filterValue) in filter.Values)
            {
                SqlCondition sumCondition = null;

                object value;

                T castValue = filterValue.Cast<T>();

                foreach (var (name, modifier) in modifiers)
                {
                    if (modifier != null)
                        value = modifier(castValue);
                    else
                        value = castValue;

                    if (value is not null)
                    {
                        SqlCondition newCondition = null;

                        if (value is DataObject obj)
                        {
                            if (obj.Data == "NULL")
                                newCondition = column == null;
                            else if (obj.Data == "NOTNULL")
                                newCondition = column != null;
                            else if (obj.Data == "CANCEL")
                                newCondition = new SqlCondition(false);
                        }
                        else
                        {
                            var isNumeric = value.IsNumeric();

                            if (attribute == null)
                                newCondition = column == value;
                            else if (attribute == "like")
                                newCondition = column % $"%{value}%";
                            else if (attribute == "start")
                                newCondition = column % $"{value}%";
                            else if (attribute == "end")
                                newCondition = column % $"%{value}";
                            else if (attribute == "notlike")
                                newCondition = column ^ $"%{value}%";
                            else if (attribute == "not")
                                newCondition = column != value;
                            else if (isNumeric && attribute == "lt")
                                newCondition = column < value;
                            else if (isNumeric && attribute == "gt")
                                newCondition = column > value;
                            else if (isNumeric && attribute == "lte")
                                newCondition = column <= value;
                            else if (isNumeric && attribute == "gte")
                                newCondition = column >= value;
                            else
                                newCondition = column == value;
                        }

                        if (name != "___default")
                            newCondition = newCondition && Table["___select"] == name;

                        if (sumCondition is null)
                            sumCondition = newCondition;
                        else
                            sumCondition = sumCondition || newCondition;
                    }
                }

                WhereCondition = sumCondition && WhereCondition;
            }
        }
    }

    #endregion

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

public class SqlBuilder
{
    #region Fields

    public string Name { get; init; }

    public SqlColumn Column { get; init; } = SqlColumn.Null;

    public SqlBuilderOptions Options { get; init; } = SqlBuilderOptions.None;

    public Action<object, SqlCell, SqlRow> Get { get; init; }

    public Action<SqlSelect> Select { get; init; }

    public Func<object, object> Query { get; init; }

    public Values<string> Requires { get; init; }
    
    public Func<object, object> Ext { get; init; }

    #endregion

    #region Methods

    public SqlCell GetCell(SqlRow row) => row != null ? row.ContainsKey(Name) ? row[Name] : null : null;

    #endregion
}

public delegate void SqlSelectBuilder<T>(   
    SqlColumn column,
    string name = null,
    SqlBuilderOptions options = SqlBuilderOptions.None,
    Action<SqlObject<T>> get = null,
    Action<SqlSelect> select = null,
    Func<object, object> query = null,
    Values<string> requires = null,
    Func<T, object> ext = null    
    );

public class SqlObject<T>
{
    public T Object { get; set; }
    public SqlCell Cell { get; set; }
    public SqlRow Row { get; set; }
}


[Flags]
public enum SqlSelectOptions
{
    None = 0,
    Distinct = 1,
    Random = 2
}

[Flags]
public enum SqlBuilderOptions
{
    None = 0,
    Id = 1
}

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Aveezo;

internal enum SqlExecuteType
{
    Reader,
    Scalar,
    Execute
}

[ModelBinder(typeof(SqlBinder))]
public sealed class Sql
{
    #region Consts

    public const string Id = "___id";

    public static readonly object Null = new DataObject("NULL");

    public static readonly object NotNull = new DataObject("NOTNULL");

    public static readonly object Cancel = new DataObject("CANCEL");

    #endregion

    #region Fields

    public string Name { get; } = null;

    internal Sql Admin { get; set; } = null;

    public SqlDatabaseType DatabaseType { get; private set; }

    public string Database { get => Connection.Database; }

    public int Timeout { get; set; } = 30;

    internal SqlConnectionBase Connection { get; }

    public string User => Connection.User;

    public SqlException LastException { get; private set; } = null;

    private readonly Dictionary<string, string> primaryKeys = new Dictionary<string, string>();

    private readonly Dictionary<Type, (string, string, string[], List<(PropertyInfo, string, SqlColumnOptions, Dictionary<object, object>)>)> tableAttributes = new Dictionary<Type, (string, string, string[], List<(PropertyInfo, string, SqlColumnOptions, Dictionary<object, object>)>)>();

    public SqlTable this[string name]
        => new(name);

    public (SqlTable, SqlTable) this[string name1, string name2]
        => (new(name1), new(name2));

    public (SqlTable, SqlTable, SqlTable) this[string name1, string name2, string name3]
        => (new(name1), new(name2), new(name3));

    public (SqlTable, SqlTable, SqlTable, SqlTable) this[string name1, string name2, string name3, string name4]
        => (new(name1), new(name2), new(name3), new(name4));

    public (SqlTable, SqlTable, SqlTable, SqlTable, SqlTable) this[string name1, string name2, string name3, string name4, string name5]
        => (new(name1), new(name2), new(name3), new(name4), new(name5));

    public (SqlTable, SqlTable, SqlTable, SqlTable, SqlTable, SqlTable) this[string name1, string name2, string name3, string name4, string name5, string name6]
        => (new(name1), new(name2), new(name3), new(name4), new(name5), new(name6));

    public (SqlTable, SqlTable, SqlTable, SqlTable, SqlTable, SqlTable, SqlTable) this[string name1, string name2, string name3, string name4, string name5, string name6, string name7]
        => (new(name1), new(name2), new(name3), new(name4), new(name5), new(name6), new(name7));

    public (SqlTable, SqlTable, SqlTable, SqlTable, SqlTable, SqlTable, SqlTable, SqlTable) this[string name1, string name2, string name3, string name4, string name5, string name6, string name7, string name8]
        => (new(name1), new(name2), new(name3), new(name4), new(name5), new(name6), new(name7), new(name8));



    #endregion

    #region Events

    public event SqlExceptionEventHandler Exception;

    #endregion

    #region Constructor

    public Sql(string connectionString, SqlDatabaseType databaseType) : this(connectionString, databaseType, null) { }

    public Sql(string connectionString, SqlDatabaseType databaseType, string name)
    {
        Name = name;

        if (connectionString == null)
            throw new ArgumentNullException("connectionString");

        DatabaseType = databaseType;

        if (databaseType == SqlDatabaseType.SqlServer)
            Connection = new SqlServerSqlConnection(connectionString);
        else if (databaseType == SqlDatabaseType.PostgreSql)
            Connection = new PostgreSqlSqlConnection(connectionString);
        else if (databaseType == SqlDatabaseType.Oracle)
            Connection = new OracleSqlConnection(connectionString);
        else
            throw new NotImplementedException($"{databaseType} is not implemented");

        if (Connection.PacketSize == 0)
            throw new NotImplementedException($"Connection PacketSize is not initialized");
        if (Connection.Database == null)
            throw new NotImplementedException($"Connection Database is not initialized");

        Connection.UseDatabase = Connection.Database;
    }

    #endregion

    #region Operators

    public static implicit operator bool(Sql sql) => sql != null && sql.Test();

    #endregion

    #region Methods

    public void Use(string database) => Connection.Use(database);

    public string Format(object obj) => Connection.FormatValue(obj);

    public string Format(SqlCondition condition) => Connection.FormatCondition(condition);

    public string Format(string sql, params object[] args)
    {
        if (sql == null)
            return null;
        else if (args == null)
            return string.Format(sql, "null");
        else
        {
            var fargs = new List<object>();

            foreach (var arg in args)
            {
                fargs.Add(Format(arg));
            }

            return string.Format(sql, fargs.ToArray());
        }
    }

    private void ProcessException(string sql, SqlQuery result, Exception exception)
    {
        if (exception != null)
        {
            var databaseException = new SqlException(exception, sql);
            databaseException.Type = Connection.ParseMessage(exception.Message);
            Exception?.Invoke(this, new SqlExceptionEventArgs(databaseException));
            result.Exception = databaseException;

            LastException = databaseException;
        }
    }

    internal SqlQuery FormatedQuery(string sql, int limit, int offset, SqlOrder order)
    {
        var query = new SqlQuery();
        var stopwatch = new Stopwatch();

        string executedsql;

        if (limit > 0 || offset > 0)
        {
            // quick and dirty to fix "order by" keyword
            if (order != null)
            {
                // there should be no "order by" keyword in the sql
                if (sql.ToLower().IndexOf("order by") > -1)
                {
                    query.Exception = new SqlException(new InvalidOperationException(), sql);
                    return query;
                }
            }
            else
            {
                // there should be "order by" keyword in the sql
                if (sql.ToLower().IndexOf("order by") == -1)
                {
                    query.Exception = new SqlException(new InvalidOperationException(), sql);
                    return query;
                }
            }

            executedsql = Connection.FormatLimitOffset(sql, limit, offset, order);
        }
        else
            executedsql = sql;

        stopwatch.Start();
        Connection.Query(executedsql, query, SqlExecuteType.Reader, out Exception exception, Timeout);
        stopwatch.Stop();

        ProcessException(sql, query, exception);

        query.ExecutionTime = stopwatch.Elapsed;
        if (query.Count > 0)
            query[0].ExecutionTime = query.ExecutionTime;

        return query;
    }

    public SqlQuery Query(string sql, params object[] args)
    {
        return FormatedQuery(Format(sql, args), 0, 0, null);
    }

    public SqlQuery PaginationQuery(string sql, int limit, params object[] args) => PaginationQuery(sql, limit, 0, null, args);

    public SqlQuery PaginationQuery(string sql, int limit, SqlOrder order, params object[] args) => PaginationQuery(sql, limit, 0, order, args);

    public SqlQuery PaginationQuery(string sql, int limit, int offset, params object[] args) => PaginationQuery(sql, limit, offset, null, args);

    public SqlQuery PaginationQuery(string sql, int limit, int offset, SqlOrder order, params object[] args)
    {
        return FormatedQuery(Format(sql, args), limit, offset, order);
    }

    internal SqlQuery FormatedExecute(string sql)
    {
        var result = new SqlQuery();
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        Connection.Query(sql, result, SqlExecuteType.Execute, out Exception exception, Timeout);
        stopwatch.Stop();

        ProcessException(sql, result, exception);

        result.ExecutionTime = stopwatch.Elapsed;
        if (result.Count > 0)
            result[0].ExecutionTime = result.ExecutionTime;

        return result;
    }

    public SqlQuery Execute(string sql, params object[] args)
    {
        return FormatedExecute(Format(sql, args));
    }

    public SqlQuery Scalar(string sql, params object[] args)
    {
        var result = new SqlQuery();
        var stopwatch = new Stopwatch();
        var fsql = Format(sql, args);

        stopwatch.Start();
        Connection.Query(fsql, result, SqlExecuteType.Scalar, out Exception exception, Timeout);
        stopwatch.Stop();

        ProcessException(fsql, result, exception);

        result.ExecutionTime = stopwatch.Elapsed;

        if (result.Count > 0)
            result[0].ExecutionTime = result.ExecutionTime;

        return result;
    }

    public SqlRow SelectToRow(SqlTable table, SqlColumn whereColumn, object whereValue)
    {
        var result = SelectFrom(table).Where(whereColumn, whereValue).Execute();

        if (result)
            return result.First.First;
        else
            return null;
    }

    public SqlSelectProto Select(params SqlColumn[] columns) => Select(SqlSelectOptions.None, columns);

    public SqlSelectProto Select(SqlSelectOptions options, params SqlColumn[] columns) => new SqlSelectProto(this, options, columns);

    public SqlSelect SelectFrom(SqlTable table) => Select().From(table);

    public SqlSelect SelectBuilder<T>(SqlTable table, Action<SqlBuilderAdd<T>> add) where T : class
    {
        var select = Select().From(table);
        select.Builder(add);
        return select;
    }

    public SqlSelect SelectBuilder<T>(SqlTable table, SqlCondition condition, Action<SqlBuilderAdd<T>> add) where T : class
    {
        var select = Select().From(table).Where(condition);
        select.Builder(add);
        return select;
    }

    public bool Test() => Test(out _);

    public bool Test(out SqlException exception)
    {
        var rc = Query(Connection.TestStatement);

        exception = rc.Exception;

        return rc;
    }

    public string GetPrimaryKey(string table)
    {
        string keyName = null;

        if (table != null)
        {
            if (primaryKeys.ContainsKey(table)) keyName = primaryKeys[table];
            else if (Connection.GetPrimaryKeyColumn(table, out keyName)) primaryKeys.Add(table, keyName);
        }

        return keyName;
    }

    public bool IsTableExists(string table) => Connection.IsTableExists(table);

    public SqlInsertTable Insert(string table, params string[] columns) => table != null ? new SqlInsertTable(this, table, columns) : null;

    public SqlUpdate Update() => new(this);

    public SqlUpdateEntry Update(SqlTable table) => table != null ? new SqlUpdateEntry(this, table) : null;

    public SqlUpdateTable Update(SqlTable table, string whereColumn) => table != null ? new SqlUpdateTable(this, table, whereColumn) : null;

    public SqlDelete Delete() => new(this);

    public SqlDeleteTable Delete(SqlTable table, string whereColumn) => table != null ? new SqlDeleteTable(this, table, whereColumn) : null;

    public Dictionary<T, U> SelectToDictionary<T, U>(SqlTable table, string columnNameAsValue) => SelectToDictionary<T, U>(table, GetPrimaryKey(table.Name), columnNameAsValue);

    public Dictionary<T, U> SelectToDictionary<T, U>(SqlTable table, string columnNameAsKey, string columnNameAsValue)
    {
        if (table == null) throw new ArgumentNullException("table");
        if (columnNameAsKey == null) throw new ArgumentNullException("columnNameAsKey");
        if (columnNameAsValue == null) throw new ArgumentNullException("columnNameAsValue");

        Dictionary<T, U> dictionary = null;

        if (table != null && columnNameAsKey != null && columnNameAsValue != null)
        {
            if (SelectFrom(table).Where(columnNameAsKey, columnNameAsValue).Execute(out SqlResult result))
                dictionary = result.ToDictionary<T, U>(columnNameAsKey, columnNameAsValue, null);
        }

        return dictionary;
    }

    public Dictionary<T, SqlRow> SelectToDictionary<T>(SqlTable table) => SelectToDictionary<T>(table, null, null);

    public Dictionary<T, SqlRow> SelectToDictionary<T>(SqlTable table, SqlCondition where) => SelectToDictionary<T>(table, where, null);

    public Dictionary<T, SqlRow> SelectToDictionary<T>(SqlTable table, SqlOrder order) => SelectToDictionary<T>(table, null, order);

    public Dictionary<T, SqlRow> SelectToDictionary<T>(SqlTable table, SqlCondition where, SqlOrder order)
    {
        Dictionary<T, SqlRow> dictionary = null;

        if (table != null)
        {
            var pk = GetPrimaryKey(table.Name);
            if (pk == null) throw new NullReferenceException($"Cannot determined {table.Name}'s primary key as dictionary key");

            var se = SelectFrom(table);

            if (where is not null)
                se.WhereCondition = where;

            se.Order = order;

            var rc = se.Execute();

            if (rc)
                dictionary = rc.First.ToDictionary<T>(pk);
        }

        return dictionary;
    }

    public List<T> SelectToList<T>(SqlTable table, SqlColumn column) => SelectToList<T>(table, column, null, null);

    public List<T> SelectToList<T>(SqlTable table, SqlColumn column, SqlCondition where) => SelectToList<T>(table, column, where, null);

    public List<T> SelectToList<T>(SqlTable table, SqlColumn column, SqlOrder order) => SelectToList<T>(table, column, null, order);

    public List<T> SelectToList<T>(SqlTable table, SqlColumn column, SqlCondition where, SqlOrder order) => SelectToList<T>(table, column, where, order, SqlSelectOptions.None);

    public List<T> SelectToList<T>(SqlTable table, SqlColumn column, SqlCondition where, SqlOrder order, SqlSelectOptions options)
    {
        List<T> list = null;

        if (table != null)
        {
            var se = Select(column).From(table);

            se.Options = options;

            if (where is not null)
                se.WhereCondition = where;

            se.Order = order;

            var rc = se.Execute();

            if (rc)
                list = rc.First.ToList<T>(column.Name);
        }

        return list;
    }

    public List<SqlRow> SelectToList(SqlTable table) => SelectToList(table, null, null);

    public List<SqlRow> SelectToList(SqlTable table, SqlCondition where) => SelectToList(table, where, null);

    public List<SqlRow> SelectToList(SqlTable table, SqlOrder order) => SelectToList(table, null, order);

    public List<SqlRow> SelectToList(SqlTable table, SqlCondition where, SqlOrder order) => SelectToList(table, where, order, SqlSelectOptions.None);

    public List<SqlRow> SelectToList(SqlTable table, SqlCondition where, SqlOrder order, SqlSelectOptions options)
    {
        List<SqlRow> list = null;

        if (table != null)
        {
            var se = SelectFrom(table);

            se.Options = options;

            if (where is not null)
                se.WhereCondition = where;

            if (order is not null)
                se.Order = order;

            var rc = se.Execute();

            if (rc)
                list = rc.First.ToList();
        }

        return list;
    }



    public SqlDataCollection<T> Collection<T>() where T : SqlData, new() => new SqlDataCollection<T>(this);

    public SqlDataCollection<T> Pull<T>() where T : SqlData, new() => Pull<T>(null);

    public SqlDataCollection<T> Pull<T>(SqlCondition where) where T : SqlData, new()
    {
        var info = SqlData.GetInfo(typeof(T));

        var table = new SqlTable(info.Table);
        var select = Select(info.Columns.Invoke((column) => table[column])).From(table);
        select.WhereCondition = where;
        var rs = select.Execute().First;

        return Pull<T>(rs, where, info);
    }

    private SqlDataCollection<T> Pull<T>(SqlResult result, SqlCondition where, SqlDataInfo info) where T : SqlData, new()
    {
        var dict = new SqlDataCollection<T>(this, false);

        foreach (var row in result)
        {
            T o = new T() { Id = row[info.IdName].GetGuid() };
            o.New = false;

            foreach (var field in info.Fields)
            {
                var data = row[field.Column].GetObject();

                o.SetValue(field, data);
            }

            dict._Add(o);
        }

        dict.Where = where;

        return dict;
    }

    #endregion

    #region Static

    public static Sql Load(Config config, string name) => Load(config, name, null);

    public static Sql Load(Config config, string name, string database)
    {
        Sql sql = null;
        Sql sqlAdmin = null;

        if (config != null && name != null)
        {
            var dbkey = $"$$SQL_{name}";
            var dbadminkey = $"$$SQLADMIN_{name}";
            var typekey = $"$$SQLTYPE_{name}";

            string cs = config.ContainsKey(dbkey) ? config[dbkey] : null;

            if (cs != null)
            {
                string type = config.ContainsKey(typekey) ? config[typekey].ToLower() : null;

                if (type != null)
                {
                    if (type == "postgres")
                        sql = new Sql(cs, SqlDatabaseType.PostgreSql, name);
                    else if (type == "sqlserver")
                        sql = new Sql(cs, SqlDatabaseType.SqlServer, name);
                    else if (type == "oracle")
                        sql = new Sql(cs, SqlDatabaseType.Oracle, name);

                    if (sql != null)
                    {
                        sql.Connection.UseDatabase = database;

                        string csadmin = config.ContainsKey(dbadminkey) ? config[dbadminkey] : null;

                        if (csadmin != null)
                        {
                            if (type == "postgres")
                                sqlAdmin = new Sql(cs, SqlDatabaseType.PostgreSql);
                            else if (type == "sqlserver")
                                sqlAdmin = new Sql(cs, SqlDatabaseType.SqlServer);
                            else if (type == "oracle")
                                sqlAdmin = new Sql(cs, SqlDatabaseType.Oracle);

                            if (sqlAdmin != null)
                            {
                                sqlAdmin.Connection.UseDatabase = database;

                                sql.Admin = sqlAdmin;
                            }
                        }
                    }
                }
            }
        }

        return sql;
    }

    public static bool Load(Config config, string name, string database, out Sql sql, EventHandler<SqlLoadEventArgs> status)
    {
        sql = Load(config, name, database);

        if (sql)
        {
            status?.Invoke(sql, new SqlLoadEventArgs { Success = true });
            return true;
        }
        else
        {
            status?.Invoke(sql, new SqlLoadEventArgs { Success = false, Exception = sql.LastException?.Exception });
            return false;
        }
    }

    public static object Default(Type type)
    {
        object value;
        if (type == typeof(bool)) value = false;
        else if (type.IsNumeric()) value = 0;
        else if (type == typeof(string)) value = "";
        else if (type == typeof(DateTimeOffset)) value = DateTimeOffset.MinValue;
        else if (type == typeof(DateTime)) value = DateTime.MinValue;
        else if (type == typeof(TimeSpan)) value = TimeSpan.Zero;
        else if (type == typeof(Guid)) value = Guid.Empty;
        else if (type == typeof(BitArray)) value = new BitArray(new[] { false });
        else if (type == typeof(PhysicalAddress)) value = PhysicalAddress.None;
        else if (type == typeof(IPAddressCidr)) value = IPAddress.None;
        else if (type == typeof(IPAddress)) value = IPAddress.None;
        else value = 0;

        return value;
    }

    public static void SetTable(SqlColumn[] columns, SqlTable table)
    {
        foreach (var column in columns)
        {
            if (column is not null)
            {
                column.Table = table;
            }
        }
    }

    internal static void SetTableWhenUnassigned(SqlColumn[] columns, SqlTable table)
    {
        if (columns != null)
        {
            foreach (var column in columns)
            {
                if (column is not null)
                {
                    if (column.Table == null)
                        column.Table = table;

                    if (column.OperationColumns != null)
                    {
                        foreach (var columnColumn in column.OperationColumns)
                        {
                            if (columnColumn is not null)
                            {
                                if (columnColumn is SqlColumn value)
                                {
                                    if (value.Table == null)
                                        value.Table = table;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    #endregion
}

public class SqlLoadEventArgs : EventArgs
{
    #region Fields

    public bool Success { get; set; } = false;

    public Exception Exception { get; set; }

    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;

using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Aveezo
{
    internal abstract class SqlConnectionBase
    {
        #region Fields

        public string ConnectionString { get; set; }

        public string User { get; init; }

        public int PacketSize { get; protected set; }

        public int StatementMaximumLength => 1024 * PacketSize;

        public string UseDatabase { get; set; } = null;

        public string Database { get; protected set; } = null;

        public IPAddressCidr IP { get; }

        #endregion

        #region Enums

        #endregion

        #region Constructor

        public SqlConnectionBase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        #endregion

        #region Methods

        public void Use(string database)
        {
            UseDatabase = database;
        }

        public string[] Insert(SqlTable table, string[] columns, SqlInsertTableEntry[] entries, bool output)
        {
            var statements = new List<string>();
            var entriesIndex = 0;

            while (entriesIndex < entries.Length)
            {
                var main = new StringBuilder(FormatInsertIntoValues(table, columns, output));

                var valuesAppended = 0;
                do
                {
                    var values = $"{(valuesAppended > 0 ? ", " : "")}{FormatInsertValuesEntry(entries[entriesIndex], output)}";

                    if (main.Length + values.Length >= StatementMaximumLength) break;
                    else
                    {
                        main.Append(values);
                        entriesIndex++;
                        valuesAppended++;
                    }
                }
                while (entriesIndex < entries.Length);

                main.Append(FormatInsertEnd(table, columns, output));

                statements.Add(main.ToString());
            }

            return statements.ToArray();
        }

        public string[] Update(SqlUpdateEntry[] entries, bool output)
        {
            var statements = new List<string>();
            var entriesIndex = 0;

            while (entriesIndex < entries.Length)
            {
                var main = new StringBuilder();

                do
                {
                    var values = $"{(main.Length > 0 ? "; " : "")}{FormatUpdateSetWhere(entries[entriesIndex].Table, FormatQuery(entries[entriesIndex].Update.Sets), (entries[entriesIndex].WhereCondition is null) ? null : FormatCondition(entries[entriesIndex].WhereCondition), output)}";

                    if (main.Length + values.Length >= StatementMaximumLength) break;
                    else
                    {
                        main.Append(values);
                        entriesIndex++;
                    }
                }
                while (entriesIndex < entries.Length);

                statements.Add(main.ToString());
            }

            return statements.ToArray();
        }

        public string[] UpdateTable(SqlTable table, string whereColumn, object[] keys, SqlUpdateTableEntry[] entries, bool output)
        {
            var statements = new List<string>();

            var entriesIndex = 0;

            while (entriesIndex < entries.Length)
            {
                var main = $"update {table} set ";
                string setwhere = null;

                var batchKeys = new List<object>();
                var batchSets = new Dictionary<string, List<(object, object)>>();

                do
                {
                    var setwhereSB = new StringBuilder();

                    var key = keys[entriesIndex];
                    batchKeys.Add(key);

                    foreach (var (set, to) in entries.Find(delegate (SqlUpdateTableEntry entry) { return entry.Where == key; }).Update.Sets)
                    {
                        if (!batchSets.ContainsKey(set))
                            batchSets.Add(set, new List<(object, object)>());

                        batchSets[set].Add((key, to));
                    }

                    var setIndex = 0;
                    foreach (var (set, tos) in batchSets)
                    {
                        if (setIndex > 0)
                            setwhereSB.Append(", ");

                        setwhereSB.Append($"{set} = case ");

                        var toIndex = 0;
                        foreach (var (toKey, toValue) in tos)
                        {
                            if (toIndex > 0)
                                setwhereSB.Append(' ');

                            setwhereSB.Append($"when {whereColumn} = {FormatValue(toKey)} then {FormatValue(toValue)}");
                            toIndex++;
                        }

                        setwhereSB.Append($" else {set} end");
                        setIndex++;
                    }

                    setwhereSB.Append(' ');
                    setwhereSB.Append(FormatUpdateTableWhere(table, whereColumn, batchKeys.ToArray(), output));

                    if (main.Length + setwhereSB.Length >= StatementMaximumLength) break;
                    else
                    {
                        setwhere = setwhereSB.ToString();
                        entriesIndex++;
                    }
                }
                while (entriesIndex < entries.Length);

                if (setwhere != null)
                    statements.Add($"{main}{setwhere}");
            }

            return statements.ToArray();
        }

        public string[] Delete(SqlDeleteEntry[] entries, bool output)
        {
            var statements = new List<string>();
            var entriesIndex = 0;

            while (entriesIndex < entries.Length)
            {
                var main = new StringBuilder();

                do
                {
                    var values = $"{(main.Length > 0 ? "; " : "")}{FormatDeleteFromWhere(entries[entriesIndex].Table, FormatCondition(entries[entriesIndex].WhereCondition), output)}";

                    if (main.Length + values.Length >= StatementMaximumLength) break;
                    else
                    {
                        main.Append(values);
                        entriesIndex++;
                    }
                }
                while (entriesIndex < entries.Length);

                statements.Add(main.ToString());
            }

            return statements.ToArray();
        }

        public string[] DeleteTable(SqlTable table, string whereColumn, object[] whereKeys, bool output)
        {
            var statements = new List<string>();

            var entriesIndex = 0;

            while (entriesIndex < whereKeys.Length)
            {
                string currentValue = null;
                var batchEntries = new List<object>();

                do
                {
                    batchEntries.Add(whereKeys[entriesIndex]);

                    var value = FormatDeleteTableFromWhere(table, whereColumn, batchEntries.ToArray(), output);

                    if (value.Length >= StatementMaximumLength) break;
                    else
                    {
                        currentValue = value;
                        entriesIndex++;
                    }
                }
                while (entriesIndex < whereKeys.Length);

                if (currentValue != null)
                    statements.Add(currentValue);
            }

            return statements.ToArray();
        }

        public string FormatValue(object obj)
        {
            if (obj == null)
                return NullValue;
            else if (obj is bool boolean)
                return boolean ? TrueValue : FalseValue;
            else if (obj is DateTime time)
                return FormatValue(time.ToDateTimeOffset());
            else if (obj is DateTimeOffset timeOffset)
                return FormatString(FormatDateTimeOffset(timeOffset));
            else if (obj is string str)
                return FormatString(EscapeString(str));
            else if (obj.IsNumeric())
                return FormatNumber(obj);
            else if (obj is BitArray bitArray)
                return FormatBinary(bitArray);
            else if (obj is byte[] byteArray)
                return FormatByteArray(byteArray);
            else if (obj is Array array)
                return FormatArray(array);
            else
                return FormatValue(obj.ToString());
        }

        public string FormatConditionValue(object obj)
        {
            if (obj is Array array)
            {
                var entries = new List<string>();
                foreach (var o in array) entries.Add(FormatValue(o));
                if (entries.Count == 1)
                    return $"{entries[0]}";
                else
                    return $"({entries.Join(", ")})";
            }
            else if (obj is IList list)
            {
                var entries = new List<string>();
                foreach (var o in list) entries.Add(FormatValue(o));
                if (entries.Count == 1)
                    return $"{entries[0]}";
                else
                    return $"({entries.Join(", ")})";
            }
            else if (obj is ITuple tuple)
            {
                throw new NotImplementedException();
            }
            else
                return FormatValue(obj);
        }

        public string FormatCondition(SqlCondition condition)
        {
            if (condition is null)
                return null;
            else if (condition.Condition1 is not null && condition.Condition2 is not null)
            {
                var a = FormatCondition(condition.Condition1);
                var b = FormatCondition(condition.Condition2);

                if (a != null && b != null)
                {
                    return $"({a}) {FormatBooleanOperator(condition.BooleanOperator)} ({b})";
                }
                else
                    return null;
            }
            else if (condition.Column is not null)
            {
                if (condition.Value == null)
                {
                    return $"{FormatWhere(condition.Column)} {FormatNullComparativeOperator(condition.ComparativeOperator)} {FormatConditionValue(condition.Value)}";
                }
                else if (condition.Value is SqlColumn column)
                {
                    return $"{FormatWhere(condition.Column)} {FormatComparativeOperator(condition.ComparativeOperator)} {FormatWhere(column)}";
                }
                else
                {
                    if (condition.ComparativeOperator == SqlComparasionOperator.In || condition.ComparativeOperator == SqlComparasionOperator.NotIn)
                    {
                        if (condition.Value is Array array && array.Length == 1 || condition.Value is IList list && list.Count == 1)
                            return $"{FormatWhere(condition.Column)} {FormatComparativeOperator(condition.ComparativeOperator == SqlComparasionOperator.In ? SqlComparasionOperator.EqualTo : SqlComparasionOperator.NotEqualTo)} {FormatConditionValue(condition.Value)}";
                        else
                            return $"{FormatWhere(condition.Column)} {FormatInclusionComparativeOperator(condition.ComparativeOperator)} {FormatConditionValue(condition.Value)}";
                    }
                    else
                    {
                        return $"{FormatWhere(condition.Column)} {FormatComparativeOperator(condition.ComparativeOperator)} {FormatConditionValue(condition.Value)}";
                    }
                }
            }
            else if (condition.Value is not null)
            {
                if (condition.Value is bool b)
                {
                    if (b)
                        return $"{TrueValue} = {TrueValue}";
                    else
                        return $"{TrueValue} = {FalseValue}";
                }
                else
                    return null;
            }
            else
                return null;

        }

        public string FormatQuery(object obj)
        {
            if (obj is Array array)
            {
                var entries = new List<string>();
                foreach (var o in array) entries.Add(FormatValue(o));
                return $"({entries.Join(", ")})";
            }
            else if (obj is Dictionary<string, object> dict)
            {
                var entries = new List<string>();
                foreach (var (key, value) in dict) entries.Add($"{key} = {FormatValue(value)}");
                return $"{entries.Join(", ")} ";
            }
            else if (obj is IList list)
                return FormatQuery(list.ToArray<object>());
            else if (obj is ITuple tuple)
                return FormatQuery(tuple.ToArray());
            else if (obj is SqlInsertTableEntry entry)
                return FormatQuery(entry.Values);
            else
                return FormatQuery(obj.Array());
        }

        public string FormatBooleanOperator(SqlConjunctionOperator op) =>
            op switch
            {
                SqlConjunctionOperator.And => "and",
                SqlConjunctionOperator.Or => "or",
                _ => "and"
            };

        public string FormatComparativeOperator(SqlComparasionOperator op) =>
            op switch
            {
                SqlComparasionOperator.EqualTo => "=",
                SqlComparasionOperator.NotEqualTo => "<>",
                SqlComparasionOperator.Like => "like",
                SqlComparasionOperator.NotLike => "not like",
                SqlComparasionOperator.LessThan => "<",
                SqlComparasionOperator.GreaterThan => ">",
                SqlComparasionOperator.LessThanOrEqualTo => "<=",
                SqlComparasionOperator.GreaterThanOrEqualTo => ">=",
                _ => "="
            };

        public string FormatInclusionComparativeOperator(SqlComparasionOperator op) =>
            op switch
            {
                SqlComparasionOperator.In => "in",
                SqlComparasionOperator.NotIn => "not in",
                _ => "in"
            };

        public string FormatNullComparativeOperator(SqlComparasionOperator op) =>
            op switch
            {
                SqlComparasionOperator.EqualTo => "is",
                SqlComparasionOperator.NotEqualTo => "is not",
                _ => "is"
            };

        public string FormatSelect(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, int limit, int offset, SqlSelectOptions options)
        {
            if (limit == 0 && offset == 0)
            {
                return FormatSelect(table, columns, joins, where, order, options);
            }
            else if (limit > 0 && offset == 0)
            {
                return FormatSelectLimit(table, columns, joins, where, order, limit, options);
            }
            else
            {
                return FormatSelectLimitOffset(table, columns, joins, where, order, limit, offset, options);
            }
        }

        public string FormatColumnOperations(SqlColumn column, bool canAlias)
        {
            if (column.Operation == SqlColumnOperation.Concat)
            {
                if (column.OperationColumns != null)
                {
                    var sb = new StringBuilder("concat(");
                    sb.Append(column.OperationColumns.Invoke(o => FormatColumn(o, canAlias)).Filter(s => s is not null).Join(", "));
                    sb.Append(")");
                    return sb.ToString();
                }
                else
                    throw new NotSupportedException();
            }
            else
                throw new NotImplementedException();
        }

        public string FormatColumn(SqlColumn[] columns)
        {
            var sb = new StringBuilder();

            if (columns == null || columns.Length == 0)
                return "*";
            else
            {
                foreach (var column in columns)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");

                    sb.Append(FormatSelectColumn(column));
                }
            }

            return sb.ToString();
        }

        public string FormatFrom(SqlTable table) => FormatFrom(FormatFromAlias(table));

        public string FormatJoin(SqlJoin[] joins)
        {
            if (joins != null && joins.Length > 0)
            {
                var sb = new StringBuilder();

                foreach (var join in joins)
                {
                    if (join.WhereCondition is not null)
                    {
                        if (join.Type == SqlJoinType.Inner) sb.Append($" inner join");
                        else if (join.Type == SqlJoinType.Left) sb.Append($" left join");
                        else if (join.Type == SqlJoinType.Right) sb.Append($" right join");
                        else if (join.Type == SqlJoinType.Full) sb.Append($" full join");

                        sb.Append($" {FormatFromAlias(join.Table)} on {FormatCondition(join.WhereCondition)}");
                    }
                    else
                    {
                        sb.Append($", {FormatFromAlias(join.Table)}");
                    }
                }

                return sb.ToString();
            }
            else
                return null;
        }

        public string FormatWhere(SqlCondition where) => FormatWhere(FormatCondition(where));

        public string FormatOrder(SqlOrder order)
        {
            if (order != null && order.Count > 0)
            {
                var sb = new StringBuilder();

                foreach (var (col, ord) in order.Orders)
                {
                    if (sb.Length > 0) sb.Append(", ");

                    var ascDesc = ord == Order.Ascending ? "asc" : "desc";

                    sb.Append($"{FormatWhere(col)} {ascDesc}");
                }

                return FormatOrder(sb.ToString());
            }
            else
                return null;
        }


        #endregion

        #region Virtuals

        public virtual string NullValue => "null";

        public virtual string AllValue => "*";

        public virtual string TrueValue => "TRUE";

        public virtual string FalseValue => "FALSE";

        public virtual string DefaultSchema => "public";

        public virtual string TestStatement => "select 1";

        public virtual string OrderByNull => "order by (select null)";

        public virtual string FormatFrom(string from) => $"from {from}";

        public virtual string FormatWhere(string where) => where.Invoke(s => $"where {s}");

        public virtual string FormatOrder(string order) => order.Invoke(s => $"order by {s}");

        public virtual string FormatColumn(SqlColumn column) => FormatColumn(column, false);

        public virtual string FormatColumn(SqlColumn column, bool canAlias) => 
            canAlias && column.Alias != null ? column.Alias :
            column.IsValue ? FormatValue(column.Value) :
            column.Operation != SqlColumnOperation.None ? FormatColumnOperations(column, canAlias) :
            $"{column.Table.Invoke(table => $"{table.Alias}.")}{column.Name}";

        public virtual string FormatSelectColumn(SqlColumn column) => $"{FormatColumn(column, false)}{column.Alias.Invoke(s => $" as '{s}'")}";

        public virtual string FormatWhere(SqlColumn column) => $"{FormatColumn(column, true)}";

        public virtual string FormatFromWithSchemaOrNot(SqlTable table) => $"{table.Schema.Invoke(schema => $"{schema}.")}{table.Name}";

        public virtual string FormatFromStatementOrTable(SqlTable table) => table.IsStatement ? $"({table.Name})" : FormatFromWithSchemaOrNot(table);

        public virtual string FormatFromAlias(SqlTable table) => $"{FormatFromStatementOrTable(table)}{table.Alias.Invoke(s => $" as '{s}'")}";

        public virtual string FormatNumber(object number) => number.ToString();

        public virtual string FormatString(string str) => $"'{str}'";

        public virtual string EscapeString(string str) => str.Replace("'", "''"); // standard way to escape '

        public virtual string FormatDateTimeOffset(DateTimeOffset dateTimeOffset) => dateTimeOffset.ToString("o"); // ISO 8601   

        public virtual string FormatByteArray(byte[] byteArray) => FormatArray(byteArray);

        public virtual Type OverrideType(Type type) => type;

        public virtual void Query(string sql, SqlQuery resultCollection, SqlExecuteType queryType, out Exception exception, int commandTimeout)
        {
            using IDisposable connection = GetConnection();

            using IDisposable command = GetCommand(sql, connection, commandTimeout);

            exception = null;

            try
            {
                OpenConnection(connection);

                if (Database != UseDatabase)
                {
                    try
                    {                        
                        Use(Database, connection);
                        Database = UseDatabase;
                    }
                    catch (NotSupportedException)
                    {
                        UseDatabase = Database;
                    }
                }

                if (queryType == SqlExecuteType.Reader)
                {
                    IDisposable reader = null;

                    try
                    {
                        using (reader = GetExecuteReader(command))
                        {
                            do
                            {
                                var columnCount = GetReaderFieldCount(reader);
                                var columnNames = new List<string>();
                                var columnTypes = new List<Type>();
                                var columnIndex = new Dictionary<string, int>();

                                for (var i = 0; i < columnCount; i++)
                                {
                                    string name = GetName(reader, i);
                                    columnNames.Add(name);
                                    columnTypes.Add(OverrideType(GetFieldType(reader, i)));

                                    if (!columnIndex.ContainsKey(name))
                                        columnIndex.Add(name, i);
                                }

                                var result = new SqlResult(sql, queryType)
                                {
                                    ColumnNames = columnNames.ToArray(),
                                    ColumnTypes = columnTypes.ToArray(),
                                    ColumnIndex = columnIndex
                                };

                                while (ReaderRead(reader))
                                {
                                    List<SqlCell> cells = new List<SqlCell>();

                                    for (var i = 0; i < columnCount; i++)
                                    {
                                        cells.Add(new SqlCell(columnTypes[i], GetIsDBNull(reader, i) ? null : GetValue(reader, columnTypes[i], i)));
                                    }

                                    result.Add(new SqlRow(result, cells.ToArray()));
                                }

                                resultCollection.Add(result);

                                ReaderNextResult(reader);
                            }
                            while (GetReaderHasRows(reader));
                        }
                    }
                    catch (Exception rex)
                    {
                        exception = rex;

                        if (reader != null)
                        {
                            if (!GetReaderIsClosed(reader))
                            {
                                CloseReader(reader);
                            }
                        }
                    }
                }
                else if (queryType == SqlExecuteType.Scalar)
                {
                    try
                    {
                        object value = GetExecuteScalar(command);

                        var result = new SqlResult(sql, queryType);   
                        result.Add(new SqlRow(result, new[] { new SqlCell(value.GetType(), value) }));
                        
                        resultCollection.Add(result);

                    }
                    catch (Exception rex)
                    {
                        exception = rex;
                    }
                }
                else if (queryType == SqlExecuteType.Execute)
                {
                    try
                    {
                        resultCollection.Add(new SqlResult(sql, queryType) { affectedRows = GetExecuteNonQuery(command) });
                    }
                    catch (Exception rex)
                    {
                        exception = rex;
                    }
                }
            }
            catch (Exception cex)
            {
                exception = cex;
            }
        }

        public virtual string FormatSelect(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, SqlSelectOptions options) => FormatSelectLimitOffset(table, columns, joins, where, order, 0, 0, options);

        public virtual string FormatSelectLimit(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, int limit, SqlSelectOptions options) => FormatSelectLimitOffset(table, columns, joins, where, order, limit, 0, options);

        public virtual string FormatSelectLimitOffset(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, int limit, int offset, SqlSelectOptions options)
        {
            SqlStatement s = "select";

            if (options.HasFlag(SqlSelectOptions.Distinct)) s += "distinct";

            s += FormatColumn(columns);
            s += FormatFrom(table);
            s += FormatJoin(joins);
            s += FormatWhere(where);

            if (limit > 0 || offset > 0)
            {
                var n = FormatLimitOffset(s, limit, offset, order);
                s.Clear();
                s += n;
            }
            else
            {
                s += FormatOrder(order);
            }

            return s;
        }

        public virtual string FormatLimitOffset(string sql, int limit, int offset, SqlOrder order)
        {
            SqlStatement s = $"select * from (select avz_inner.*, row_number() over ({FormatOrder(order) ?? OrderByNull}) avz_row_number from (";
            s += sql;
            s += $") avz_inner) avz_outer where avz_row_number > {offset} and avz_row_number <= {(offset + limit)}";

            return s;
        }

        public virtual bool IsTableExists(SqlTable table)
        {
            var rc = new SqlQuery();

            var schema = table.Schema;
            var name = table.Name;

            if (schema == null) schema = DefaultSchema;
            
            Query($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE {(schema != null ? $"TABLE_SCHEMA = '{schema}' AND " : "")}TABLE_NAME = '{name}'", rc, SqlExecuteType.Reader, out _, 10000);

            if (rc)
                return rc.First.Count > 0;
            else
                return false;
        }

        public virtual string FormatInsertIntoValues(SqlTable table, string[] columns, bool output) => $"insert into {table.Ident}{(columns.Length > 0 ? $"({columns.Join(", ")})" : "")} values";

        public virtual string FormatInsertValuesEntry(SqlInsertTableEntry entry, bool output) => $"{FormatQuery(entry)}";

        public virtual string FormatInsertEnd(SqlTable table, string[] columns, bool output) => null;

        public virtual string FormatUpdateSetWhere(SqlTable table, string set, string where, bool output) => $"update {table.Ident} set {set}{FormatWhere(where)}";

        public virtual string FormatUpdateTableWhere(SqlTable table, string whereColumn, object[] whereKeys, bool output) => $" where {whereColumn} in {FormatQuery(whereKeys)}";

        public virtual string FormatDeleteFromWhere(SqlTable table, string where, bool output) => $"delete from {table.Ident}{FormatWhere(where)}";

        public virtual string FormatDeleteTableFromWhere(SqlTable table, string whereColumn, object[] whereKeys, bool output) => $"delete from {table.Ident} where {whereColumn} in {FormatQuery(whereKeys)}";

        #endregion

        #region Abstracts

        public abstract bool GetPrimaryKeyColumn(SqlTable table, out string columnName);

        public abstract void Use(string database, object connection);

        public abstract string FormatArray(Array array);

        public abstract string FormatBinary(BitArray bitArray);

        public abstract IDisposable GetConnection();

        public abstract IDisposable GetCommand(string sql, object connection, int commandTimeOut);

        public abstract void OpenConnection(object connection);

        public abstract IDisposable GetExecuteReader(object command);

        public abstract object GetExecuteScalar(object command);

        public abstract int GetExecuteNonQuery(object command);

        public abstract bool GetReaderIsClosed(object reader);

        public abstract bool GetReaderHasRows(object reader);

        public abstract int GetReaderFieldCount(object reader);

        public abstract void CloseReader(object reader);

        public abstract bool ReaderRead(object reader);

        public abstract bool ReaderNextResult(object reader);

        public abstract bool GetIsDBNull(object reader, int ordinal);

        public abstract string GetName(object reader, int ordinal);

        public abstract Type GetFieldType(object reader, int ordinal);

        public abstract object GetValue(object reader, Type type, int ordinal);

        public abstract SqlExceptionType ParseMessage(string message);

        public abstract void OnServiceCreated(SqlService service);

        public abstract void OnServiceStarted(SqlService service, string[] registers);

        #endregion
    }



}

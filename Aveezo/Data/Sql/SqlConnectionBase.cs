using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Aveezo
{
    internal abstract class SqlConnectionBase
    {
        #region Fields

        protected string ConnectionString { get; set; }

        internal string User { get; init; }

        public int PacketSize { get; protected set; }

        public int StatementMaximumLength => 1024 * PacketSize;

        internal string UseDatabase { get; set; } = null;

        public string Database { get; protected set; } = null;

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

        internal void Use(string database)
        {
            UseDatabase = database;
        }
          
        internal string FormatValue(object obj)
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

        internal string FormatConditionValue(object obj)
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

        internal string FormatCondition(SqlCondition condition)
        {
            if (condition is null)
                return null;
            else if (condition.Condition1 is not null && condition.Condition2 is not null)
            {
                var a = FormatCondition(condition.Condition1);
                var b = FormatCondition(condition.Condition2);

                if (a != null && b != null)
                {
                    return $"({a}) {FromBooleanOperator(condition.BooleanOperator)} ({b})";
                }
                else
                    return null;
            }
            else if (condition.Column is not null)
            {
                if (condition.Value == null)
                {
                    return $"{condition.Column} {FromNullComparativeOperator(condition.ComparativeOperator)} {FormatConditionValue(condition.Value)}";
                }
                else if (condition.Value is SqlColumn)
                {
                    return $"{condition.Column} {FromComparativeOperator(condition.ComparativeOperator)} {condition.Value}";
                }
                else 
                {
                    if (condition.ComparativeOperator == SqlComparasionOperator.In || condition.ComparativeOperator == SqlComparasionOperator.NotIn)
                    {
                        if (condition.Value is Array array && array.Length == 1 || condition.Value is IList list && list.Count == 1)
                            return $"{condition.Column} {FromComparativeOperator(condition.ComparativeOperator == SqlComparasionOperator.In ? SqlComparasionOperator.EqualTo : SqlComparasionOperator.NotEqualTo)} {FormatConditionValue(condition.Value)}";
                        else
                            return $"{condition.Column} {FromInclusionComparativeOperator(condition.ComparativeOperator)} {FormatConditionValue(condition.Value)}";
                    }
                    else
                    {
                        return $"{condition.Column} {FromComparativeOperator(condition.ComparativeOperator)} {FormatConditionValue(condition.Value)}";
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

        internal string FormatQuery(object obj)
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
                return $"{entries.Join(", ")}";
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

        private string FromBooleanOperator(SqlConjunctionOperator op) =>
            op switch
            {
                SqlConjunctionOperator.And => "and",
                SqlConjunctionOperator.Or => "or",
                _ => "and"
            };

        private string FromComparativeOperator(SqlComparasionOperator op) =>
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

        private string FromInclusionComparativeOperator(SqlComparasionOperator op) =>
            op switch
            {
                SqlComparasionOperator.In => "in",
                SqlComparasionOperator.NotIn => "not in",
                _ => "in"
            };

        private string FromNullComparativeOperator(SqlComparasionOperator op) =>
            op switch
            {
                SqlComparasionOperator.EqualTo => "is",
                SqlComparasionOperator.NotEqualTo => "is not",
                _ => "is"
            };

        internal string Select(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, int limit, int offset, SqlSelectOptions options)
        {
            if (limit == 0 && offset == 0)
            {
                return Select(table, columns, joins, where, order, options);
            }
            else if (limit > 0 && offset == 0)
            {
                return SelectLimit(table, columns, joins, where, order, limit, options);
            }
            else
            {
                return SelectLimitOffset(table, columns, joins, where, order, limit, offset, options);
            }
        }
       
        private string GetColumnStatement(SqlColumn column)
        {
            var sb = new StringBuilder();

            if (column.ConcatColumns != null)
            {
                sb.Append("concat(");

                var index = 0;

                foreach (var columnColumn in column.ConcatColumns)
                {
                    if (columnColumn is not null)
                    {
                        if (index > 0)
                            sb.Append(", ");

                        if (columnColumn is SqlColumn value)
                            sb.Append(GetColumnStatement(value));
                        else
                            sb.Append(FormatValue(columnColumn.Value));

                        index++;
                    }
                }
                sb.Append(')');
            }
            else
                sb.Append(column.ToString());

            return sb.ToString();
        }

        protected string GetColumnStatement(SqlColumn[] columns)
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

                    sb.Append(FormatColumn(GetColumnStatement(column), column.Alias));
                }
            }

            return sb.ToString();
        }

        protected string GetOrderStatement(SqlOrder order) => (order is not null) ? order.Statement : null;

        protected string GetWhereStatement(SqlCondition where) => (where is not null) ? FormatCondition(where) : null;

        protected string GetJoinStatement(SqlJoin[] joins)
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

                    sb.Append($" {GetTableStatement(join.Table)} on {GetWhereStatement(join.WhereCondition)}");
                }
                else
                {
                    sb.Append($", {GetTableStatement(join.Table)}");
                }
            }

            return sb.ToString();
        }

        protected string GetTableStatement(SqlTable table) => FormatTable(table.GetDefinition(), table.Alias, table.TableSample);

        protected string Where(string where) => where.Cast(x => $" where {x}", "");

        internal string[] Insert(string table, string[] columns, SqlInsertTableEntry[] entries, bool output)
        {
            var statements = new List<string>();
            var entriesIndex = 0;

            while (entriesIndex < entries.Length)
            {
                var main = new StringBuilder(InsertIntoValues(table, columns, output));

                var valuesAppended = 0;
                do
                {
                    var values = $"{(valuesAppended > 0 ? ", " : "")}{InsertValuesEntry(entries[entriesIndex], output)}";

                    if (main.Length + values.Length >= StatementMaximumLength) break;
                    else
                    {
                        main.Append(values);
                        entriesIndex++;
                        valuesAppended++;
                    }
                }
                while (entriesIndex < entries.Length);

                main.Append(InsertEndStatement(table, columns, output));

                statements.Add(main.ToString());
            }

            return statements.ToArray();
        }

        internal string[] Update(SqlUpdateEntry[] entries, bool output)
        {
            var statements = new List<string>();
            var entriesIndex = 0;

            while (entriesIndex < entries.Length)
            {
                var main = new StringBuilder();

                do
                {
                    var values = $"{(main.Length > 0 ? "; " : "")}{UpdateSetWhere(entries[entriesIndex].Table, FormatQuery(entries[entriesIndex].Update.Sets), (entries[entriesIndex].WhereCondition is null) ? null : FormatCondition(entries[entriesIndex].WhereCondition), output)}";

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

        internal string[] UpdateTable(string table, string whereColumn, object[] keys, SqlUpdateTableEntry[] entries, bool output)
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
                    setwhereSB.Append(UpdateTableWhere(table, whereColumn, batchKeys.ToArray(), output));
                        
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

        internal string[] Delete(SqlDeleteEntry[] entries, bool output)
        {
            var statements = new List<string>();
            var entriesIndex = 0;

            while (entriesIndex < entries.Length)
            {
                var main = new StringBuilder();

                do
                {
                    var values = $"{(main.Length > 0 ? "; " : "")}{DeleteFromWhere(entries[entriesIndex].Table, FormatCondition(entries[entriesIndex].WhereCondition), output)}";

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

        internal string[] DeleteTable(string table, string whereColumn, object[] whereKeys, bool output)
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

                    var value = DeleteTableFromWhere(table, whereColumn, batchEntries.ToArray(), output);

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

        #endregion

        #region Virtuals

        public virtual string NullValue => "null";

        public virtual string TrueValue => "TRUE";

        public virtual string FalseValue => "FALSE";

        public virtual string DefaultSchema => "public";

        public virtual string TestStatement => "select 1";

        public virtual string OrderByNull => "(select null)";

        public virtual string FormatColumn(string name, string alias) => $"{name}{alias.Cast(s => $" as {s}")}";

        public virtual string FormatTable(string name, string alias, float tableSample) => $"{name}{alias.Cast(s => $" as {s}")}";

        public virtual string FormatNumber(object number) => number.ToString();

        public virtual string FormatString(string str) => $"'{str}'";

        public virtual string EscapeString(string str) => str.Replace("'", "''"); // standard way to escape '

        public virtual string FormatDateTimeOffset(DateTimeOffset dateTimeOffset) => dateTimeOffset.ToString("o"); // ISO 8601   

        public virtual string FormatByteArray(byte[] byteArray) => FormatArray(byteArray);

        public virtual Type OverrideType(Type type) => type;

        public virtual void Query(string sql, SqlResultCollection resultCollection, SqlQueryType queryType, out Exception exception, int commandTimeout)
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

                if (queryType == SqlQueryType.Reader)
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
                else if (queryType == SqlQueryType.Scalar)
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
                else if (queryType == SqlQueryType.Execute)
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

        public virtual string Select(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, SqlSelectOptions options) => SelectLimitOffset(table, columns, joins, where, order, 0, 0, options);

        public virtual string SelectLimit(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, int limit, SqlSelectOptions options) => SelectLimitOffset(table, columns, joins, where, order, limit, 0, options);

        public virtual string SelectLimitOffset(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, int limit, int offset, SqlSelectOptions options)
        {
            string r;
            var sb = new StringBuilder();

            sb.Append($"select ");

            if (options.HasFlag(SqlSelectOptions.Distinct)) 
                sb.Append("distinct ");

            sb.Append($"{GetColumnStatement(columns)}");

            sb.Append($" from {GetTableStatement(table)}");

            if (joins.Length > 0)
                sb.Append(GetJoinStatement(joins));

            if (where is not null) 
                sb.Append($" where {GetWhereStatement(where)}");

            if (limit > 0 || offset > 0)
            {
                r = FormatLimitOffset(sb.ToString(), limit, offset, order);
            }
            else
            {
                var orderSql = GetOrderStatement(order);

                if (orderSql != null)
                    sb.Append($" order by {orderSql} ");

                r = sb.ToString();
            }

            return r;
        }

        public virtual string FormatLimitOffset(string sql, int limit, int offset, SqlOrder order)
        {
            var sb = new StringBuilder();

            var orderSql = GetOrderStatement(order);

            sb.Append($"select * from (select avz_inner.*, row_number() over (order by {orderSql ?? OrderByNull}) avz_row_number from (");
            sb.Append(sql);
            sb.Append($") avz_inner) avz_outer where avz_row_number > {offset} and avz_row_number <= {(offset + limit)}");

            return sb.ToString();
        }

        public virtual bool IsTableExists(SqlTable table)
        {
            var rc = new SqlResultCollection();

            var schema = table.Schema;
            var name = table.Name;

            if (schema == null) schema = DefaultSchema;
            
            Query($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE {(schema != null ? $"TABLE_SCHEMA = '{schema}' AND " : "")}TABLE_NAME = '{name}'", rc, SqlQueryType.Reader, out _, 10000);

            if (rc)
                return rc.First.Count > 0;
            else
                return false;
        }

        public virtual string InsertIntoValues(string table, string[] columns, bool output) => $"insert into {table}{(columns.Length > 0 ? $"({columns.Join(", ")})" : "")} values";

        public virtual string InsertValuesEntry(SqlInsertTableEntry entry, bool output) => $"{FormatQuery(entry)}";

        public virtual string InsertEndStatement(string table, string[] columns, bool output) => null;

        public virtual string UpdateSetWhere(string table, string set, string where, bool output) => $"update {table} set {set}{Where(where)}";

        public virtual string UpdateTableWhere(string table, string whereColumn, object[] whereKeys, bool output) => $"where {whereColumn} in {FormatQuery(whereKeys)}";

        public virtual string DeleteFromWhere(string table, string where, bool output) => $"delete from {table}{Where(where)}";

        public virtual string DeleteTableFromWhere(string table, string whereColumn, object[] whereKeys, bool output) => $"delete from {table} where {whereColumn} in {FormatQuery(whereKeys)}";

        #endregion

        #region Abstracts

        public abstract bool GetPrimaryKeyColumn(string table, out string columnName);

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

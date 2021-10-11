// https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings

using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aveezo
{
    internal sealed class SqlServerSqlConnection : SqlConnectionBase
    {
        #region Constructor

        public SqlServerSqlConnection(string connectionString) : base(connectionString)
        {
            using var connection = new SqlConnection(ConnectionString);

            Database = connection.Database;
            PacketSize = connection.PacketSize;            
        }

        #endregion

        #region Methods

        public string FormatOutput(bool output) => output ? " output inserted.*" : "";

        // Virtual

        public override string TrueValue => "1";

        public override string FalseValue => "0";

        public override string DefaultSchema => null;

        public override string FormatTable(string name, string alias, float tableSample) => $"{name}{(tableSample > 0 ? $" tablesample({tableSample})" : "")}{alias.Cast(s => $" {s}")}";

        public override string SelectLimit(SqlTable table, SqlColumn[] columns, SqlJoin[] joins, SqlCondition where, SqlOrder order, int limit, SqlSelectOptions options)
        {
            var sb = new StringBuilder();

            sb.Append($"select ");
            if (options.HasFlag(SqlSelectOptions.Distinct))
                sb.Append("distinct ");
            sb.Append($"top {limit} {GetColumnStatement(columns)} ");
            sb.Append($"from {GetTableStatement(table)}");

            if (joins.Length > 0)
                sb.Append(GetJoinStatement(joins));

            if (where is not null)
                sb.Append($" where {GetWhereStatement(where)} ");

            var orderSql = GetOrderStatement(order);
            if (orderSql != null)
                sb.Append($"order by {orderSql} ");

            return sb.ToString();
        }

        public override string FormatLimitOffset(string sql, int limit, int offset, SqlOrder order)
        {
            var sb = new StringBuilder();

            sb.AppendLine(sql);

            var orderSql = GetOrderStatement(order);

            if (orderSql != null)
            {
                sb.AppendLine($"order by {orderSql}");
                sb.AppendLine($"offset {offset} rows");
                sb.AppendLine($"fetch next {limit} rows only");
            }

            return sb.ToString();
        }

        public override string InsertIntoValues(string table, string[] columns, bool output) => $"insert into {table}({columns.Join(", ")}){FormatOutput(output)} values";

        public override string UpdateSetWhere(string table, string set, string where, bool output) => $"update {table} set {set}{FormatOutput(output)}{Where(where)}";

        public override string UpdateTableWhere(string table, string whereColumn, object[] whereKeys, bool output) => $"{FormatOutput(output)} where {whereColumn} in {FormatQuery(whereKeys)}";

        public override string DeleteFromWhere(string table, string where, bool output) => $"delete from {table}{FormatOutput(output)}{Where(where)}";

        public override string DeleteTableFromWhere(string table, string whereColumn, object[] whereKeys, bool output) => $"delete from {table}{FormatOutput(output)} where {whereColumn} in {FormatQuery(whereKeys)}";

        // Abstract 

        public override bool GetPrimaryKeyColumn(string table, out string columnName)
        {
            columnName = null;

            var result = new SqlResultCollection();

            Query(@$"SELECT Col.Column_Name from 
    INFORMATION_SCHEMA.TABLE_CONSTRAINTS Tab, 
    INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col 
WHERE 
    Col.Constraint_Name = Tab.Constraint_Name
    AND Col.Table_Name = Tab.Table_Name
    AND Constraint_Type = 'PRIMARY KEY'
    AND Col.Table_Name = '{table}'", result, SqlQueryType.Reader, out _, 10000);


            if (result)
            {
                columnName = result[0][0]["Column_Name"].GetString();
                return true;
            }
            else
                return false;
        }

        public override void Use(string database, object connection) => ((SqlConnection)connection).ChangeDatabase(database);

        public override string FormatArray(Array array) => throw new NotImplementedException();

        public override string FormatBinary(BitArray bitArray) => throw new NotImplementedException();

        public override IDisposable GetConnection() => new SqlConnection(ConnectionString);

        public override IDisposable GetCommand(string sql, object connection, int commandTimeout) => new SqlCommand(sql, (SqlConnection)connection) { CommandTimeout = commandTimeout };

        public override void OpenConnection(object connection) => ((SqlConnection)connection).Open();

        public override IDisposable GetExecuteReader(object command) => ((SqlCommand)command).ExecuteReader();

        public override object GetExecuteScalar(object command) => ((SqlCommand)command).ExecuteScalar();

        public override int GetExecuteNonQuery(object command) => ((SqlCommand)command).ExecuteNonQuery();

        public override bool GetReaderIsClosed(object reader) => ((SqlDataReader)reader).IsClosed;

        public override bool GetReaderHasRows(object reader) => ((SqlDataReader)reader).HasRows;

        public override int GetReaderFieldCount(object reader) => ((SqlDataReader)reader).FieldCount;

        public override void CloseReader(object reader) => ((SqlDataReader)reader).Close();

        public override bool ReaderRead(object reader) => ((SqlDataReader)reader).Read();

        public override bool ReaderNextResult(object reader) => ((SqlDataReader)reader).NextResult();

        public override string GetName(object reader, int ordinal) => ((SqlDataReader)reader).GetName(ordinal);

        public override bool GetIsDBNull(object reader, int ordinal) => ((SqlDataReader)reader).IsDBNull(ordinal);

        public override Type GetFieldType(object reader, int ordinal) => ((SqlDataReader)reader).GetFieldType(ordinal);

        public override object GetValue(object reader, Type type, int ordinal)
        {
            var r = (SqlDataReader)reader;

            if (type == typeof(bool)) return r.GetBoolean(ordinal);
            //else if (type == typeof(sbyte))            
            else if (type == typeof(byte)) return r.GetByte(ordinal);
            else if (type == typeof(short)) return r.GetInt16(ordinal);
            //else if (type == typeof(ushort)) 
            else if (type == typeof(int)) return r.GetInt32(ordinal);
            else if (type == typeof(uint)) return r.GetFieldValue<uint>(ordinal);
            else if (type == typeof(long)) return r.GetInt64(ordinal);
            //else if (type == typeof(ulong)) 
            else if (type == typeof(char)) return r.GetChar(ordinal);
            else if (type == typeof(float)) return r.GetFloat(ordinal);
            else if (type == typeof(double)) return r.GetDouble(ordinal);
            else if (type == typeof(decimal)) return r.GetDecimal(ordinal);
            else if (type == typeof(double)) return r.GetDouble(ordinal);
            else if (type == typeof(DateTimeOffset)) return r.GetDateTimeOffset(ordinal);
            else if (type == typeof(DateTime)) return r.GetDateTime(ordinal);
            else if (type == typeof(TimeSpan)) return r.GetTimeSpan(ordinal);
            else if (type == typeof(string)) return r.GetString(ordinal);
            else if (type == typeof(Guid)) return r.GetGuid(ordinal);

            else return r.GetValue(ordinal);
        }

        public override SqlExceptionType ParseMessage(string message)
        {
            if (message.IndexOf("login failed") > -1)
                return SqlExceptionType.LoginFailed;
            else if (message.IndexOf("timeout period elapsed") > -1)
                return SqlExceptionType.Timeout;
            else if (message.IndexOf("Cannot insert explicit value for identity column in table") > -1)
                return SqlExceptionType.InsertFailedExplicitIdentity;
            else
                return SqlExceptionType.Unspecified;
        }

        public override void OnServiceCreated(SqlService service)
        {
            throw new NotImplementedException();
        }

        public override void OnServiceStarted(SqlService service, string[] registers)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

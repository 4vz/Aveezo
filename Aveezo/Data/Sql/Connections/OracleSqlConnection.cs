using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    internal sealed class OracleSqlConnection : SqlConnectionBase
    {
        #region Constructors

        public OracleSqlConnection(string connectionString) : base(connectionString)
        {
            using var connection = new OracleConnection(ConnectionString);

            var s = Collections.CreateDictionary(ConnectionString, Collections.Semicolon, Collections.Equal, StringConvertOptions.ToLower, StringConvertOptions.None);

            Database = connection.Database;
            
            PacketSize = 4096;
            User = s.ContainsKey("user id") ? s["user id"] : null;
        }

        #endregion

        #region Methods

        // Virtual

        public override string TestStatement => "SELECT 1 FROM DUAL";

        public override string OrderByNull => "null";

        public override string FormatSelectColumn(SqlColumn column) => $"{FormatColumn(column)}{column.Alias.Format(s => $" '{s}'")}";

        public override string FormatFromAlias(SqlTable table) => $"{FormatFromStatementOrTable(table)}{(table.TableSample > 0 ? $" sample({table.TableSample})" : "")}{table.Alias.Format(s => $" {s}")}";

        // Abstract

        public override bool GetPrimaryKeyColumn(SqlTable table, out string columnName)
        {
            columnName = null;

            var result = new SqlQuery();

            Query(@$"
SELECT cols.column_name
FROM all_constraints cons, all_cons_columns cols
WHERE cols.table_name = '{table.Name.ToUpper()}'
AND cons.constraint_type = 'P'
AND cons.constraint_name = cols.constraint_name
AND cons.owner = cols.owner
ORDER BY cols.table_name, cols.position;
", result, SqlExecuteType.Reader, out _, 10000);

            if (result)
            {
                columnName = result[0][0]["column_name"].GetString();
                return true;
            }
            else
                return false;

        }

        public override void Use(string database, object connection) => ((OracleConnection)connection).ChangeDatabase(database);

        public override string FormatArray(Array array) => throw new NotImplementedException();

        public override string FormatBinary(BitArray bitArray) => throw new NotImplementedException();

        public override IDisposable GetConnection() => new OracleConnection(ConnectionString);

        public override IDisposable GetCommand(string sql, object connection, int commandTimeout) => new OracleCommand(sql, (OracleConnection)connection) { CommandTimeout = commandTimeout };

        public override void OpenConnection(object connection) => ((OracleConnection)connection).Open();

        public override IDisposable GetExecuteReader(object command) => ((OracleCommand)command).ExecuteReader();

        public override object GetExecuteScalar(object command) => ((OracleCommand)command).ExecuteScalar();

        public override int GetExecuteNonQuery(object command) => ((OracleCommand)command).ExecuteNonQuery();

        public override bool GetReaderIsClosed(object reader) => ((OracleDataReader)reader).IsClosed;

        public override bool GetReaderHasRows(object reader) => ((OracleDataReader)reader).HasRows;

        public override int GetReaderFieldCount(object reader) => ((OracleDataReader)reader).FieldCount;

        public override void CloseReader(object reader) => ((OracleDataReader)reader).Close();

        public override bool ReaderRead(object reader) => ((OracleDataReader)reader).Read();

        public override bool ReaderNextResult(object reader) => ((OracleDataReader)reader).NextResult();

        public override string GetName(object reader, int ordinal) => ((OracleDataReader)reader).GetName(ordinal);

        public override bool GetIsDBNull(object reader, int ordinal) => ((OracleDataReader)reader).IsDBNull(ordinal);

        public override Type GetFieldType(object reader, int ordinal) => ((OracleDataReader)reader).GetFieldType(ordinal);

        public override object GetValue(object reader, Type type, int ordinal)
        {
            var r = (OracleDataReader)reader;

            if (type == typeof(bool)) return r.GetBoolean(ordinal);
            //else if (type == typeof(sbyte)) 
            else if (type == typeof(byte)) return r.GetByte(ordinal);
            else if (type == typeof(short)) return r.GetInt16(ordinal);
            //else if (type == typeof(ushort)) 
            else if (type == typeof(int)) return r.GetInt32(ordinal);
            //else if (type == typeof(uint))
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

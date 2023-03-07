// https://www.npgsql.org/doc/types/basic.html

using NpgsqlTypes;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Net.NetworkInformation;
using System.Net;

namespace Aveezo
{
    internal sealed class PostgreSqlSqlConnection : SqlConnectionBase
    {
        #region Constructors

        public PostgreSqlSqlConnection(string connectionString) : base(connectionString)
        {
            using var connection = new NpgsqlConnection(ConnectionString);

            Database = connection.Database;
            PacketSize = 4096;
            User = connection.UserName;
        }

        #endregion

        #region Methods

        private string FormatReturning(bool output) => output ? " returning *" : "";

        // Virtual

        public override string FormatSelectColumn(SqlColumn column) => $"{FormatColumn(column)}{column.Alias.IfNotNull(s => $" as \"{s}\"")}";

        public override string FormatFromAlias(SqlTable table) => $"{FormatFromStatementOrTable(table)}{(table.TableSample > 0 ? $" tablesample system({table.TableSample})" : "")}{table.Alias.IfNotNull(s => $" {s}")}";

        public override Type OverrideType(Type type)
        {
            if (type == typeof((IPAddress, int))) return typeof(IPAddressCidr);
            else return type;
        }

        public override string FormatInsertEnd(SqlTable table, string[] columns, bool output) => FormatReturning(output);

        public override string FormatUpdateSetWhere(SqlTable table, string set, string where, bool output) => $"update {FormatTable(table)} set {set}{FormatWhere(where)}{FormatReturning(output)}";

        public override string FormatUpdateTableWhere(SqlTable table, string whereColumn, object[] whereKeys, bool output) => $"where {whereColumn} in {FormatQuery(whereKeys)}{FormatReturning(output)}";

        public override string FormatDeleteFromWhere(SqlTable table, string where, bool output) => $"delete from {FormatTable(table)}{FormatWhere(where)}{FormatReturning(output)}";

        public override string FormatDeleteTableFromWhere(SqlTable table, string whereColumn, object[] whereKeys, bool output) => $"delete from {FormatTable(table)} where {whereColumn} in {FormatQuery(whereKeys)}{FormatReturning(output)}";

        // Abstract

        public override void Use(string database, object connection) => throw new NotSupportedException();

        public override string FormatArray(Array array)
        {
            var stx = new List<string>();

            foreach (var item in array)
            {
                if (item == null)
                    stx.Add(NullValue);
                else if (item is string itemstr)
                    stx.Add($"\"{itemstr}\"");
            }

            if (stx.Count > 0)
                return $"'{{{stx.ToArray().Join(",")}}}'";
            else
                return NullValue;
        }

        public override string FormatByteArray(byte[] byteArray) => $"decode('{byteArray.ToHex()}', 'hex')";

        public override string FormatBinary(BitArray bitArray) => $"B'{bitArray.ToString('0', '1')}'";

        public override IDisposable GetConnection() => new NpgsqlConnection(ConnectionString);
        
        public override IDisposable GetCommand(string sql, object connection, int commandTimeout) => new NpgsqlCommand(sql, (NpgsqlConnection)connection) { CommandTimeout = commandTimeout };
        
        public override void OpenConnection(object connection) => ((NpgsqlConnection)connection).Open();
        
        public override IDisposable GetExecuteReader(object command) => ((NpgsqlCommand)command).ExecuteReader();
        
        public override object GetExecuteScalar(object command) => ((NpgsqlCommand)command).ExecuteScalar();
        
        public override int GetExecuteNonQuery(object command) => ((NpgsqlCommand)command).ExecuteNonQuery();
        
        public override bool GetReaderIsClosed(object reader) => ((NpgsqlDataReader)reader).IsClosed;
        
        public override bool GetReaderHasRows(object reader) => ((NpgsqlDataReader)reader).HasRows;
        
        public override int GetReaderFieldCount(object reader) => ((NpgsqlDataReader)reader).FieldCount;
        
        public override void CloseReader(object reader) => ((NpgsqlDataReader)reader).Close();

        public override bool ReaderRead(object reader) => ((NpgsqlDataReader)reader).Read();

        public override bool ReaderNextResult(object reader) => ((NpgsqlDataReader)reader).NextResult();

        public override string GetName(object reader, int ordinal) => ((NpgsqlDataReader)reader).GetName(ordinal);

        public override bool GetIsDBNull(object reader, int ordinal) => ((NpgsqlDataReader)reader).IsDBNull(ordinal);

        public override Type GetFieldType(object reader, int ordinal) => ((NpgsqlDataReader)reader).GetFieldType(ordinal); 

        public override object GetValue(object reader, Type type, int ordinal)
        {
            var r = (NpgsqlDataReader)reader;

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
            else if (type == typeof(DateTimeOffset)) return r.GetFieldValue<DateTimeOffset>(ordinal);
            else if (type == typeof(DateTime)) return r.GetDateTime(ordinal);
            else if (type == typeof(TimeSpan)) return r.GetTimeSpan(ordinal);
            else if (type == typeof(string)) return r.GetString(ordinal);
            else if (type == typeof(Guid)) return r.GetGuid(ordinal);
            // Postgres
            else if (type == typeof(BitArray)) return r.GetFieldValue<BitArray>(ordinal);
            else if (type == typeof(PhysicalAddress)) return r.GetFieldValue<PhysicalAddress>(ordinal);
            else if (type == typeof(IPAddress)) return r.GetFieldValue<IPAddress>(ordinal);
            else if (type == typeof(IPAddressCidr))
            {
                var (d, e) = r.GetFieldValue<(IPAddress, int)>(ordinal);
                return new IPAddressCidr(d, (byte)e);
            }

            else return r.GetValue(ordinal);
        }

        public override SqlExceptionType ParseMessage(string message)
        {
            return SqlExceptionType.Unspecified;
        }

        public override void Service(SqlService service, string serviceSchema, SqlTable[] tables)
        {
            var sql = service.Sql;
            var admin = sql.Admin;

            var registers = new List<(string, string, string, string, string)>();

            // DB DATA
            var pgproc = admin["pg_catalog.pg_proc"];
            var pgnamespace = admin["pg_catalog.pg_namespace"];
            var triggers = admin["information_schema.triggers"];

            var currentTriggerFunctions = admin
                .Select(SqlColumn.Concat("schemaandname", pgnamespace["nspname"], SqlColumn.Static("."), pgproc["proname"]), pgnamespace["nspname"], pgproc["proname"])
                .From(pgproc)
                .Join(SqlJoinType.Left, pgnamespace, pgproc["pronamespace"], pgnamespace["oid"])
                .Where(pgproc["proname"] % $"{SqlService.ServiceIdent}%")
                .Execute()
                .First.ToList<string, string, string>("schemaandname", "nspname", "proname");

            var currentTriggers = admin
                .Select(SqlSelectOptions.Distinct, SqlColumn.Concat("schemaandname", triggers["trigger_schema"], SqlColumn.Static("."), triggers["event_object_table"]), triggers["trigger_schema"], triggers["event_object_table"]).From(triggers)
                .Where(triggers["trigger_name"] == $"{SqlService.ServiceIdent}")
                .Execute()
                .First.ToList<string, string, string>("schemaandname", "trigger_schema", "event_object_table");

            // CHECK SERVICE TABLE

            var a = 1;



            /*

            // ADD TRIGGER FUNCTIONS
            foreach (var table in tables)
            {
                var schema = table.Schema;
                var tableFullName = FormatTable(table);
                var name = table.Name;
                var function = $"{SqlService.ServiceTriggerFunctionPrefix}{name}";


                var schemaFunction = $"{schema}.{function}";
                var schemaServiceTable = $"{schema}.{SqlService.ServiceTable}";

                // ADD SERVICE TABLE
                if (!sql.IsTableExists(schemaServiceTable))
                {
                    admin.Execute($@"
CREATE TABLE {schemaServiceTable}
(
    {SqlService.ServiceColumnId} bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    {SqlService.ServiceColumnTimestamp} timestamp without time zone NOT NULL DEFAULT now(),
    {SqlService.ServiceColumnTag} character varying COLLATE pg_catalog.""default"",
    CONSTRAINT {SqlService.ServiceTable}_pkey PRIMARY KEY({SqlService.ServiceColumnId})
)

TABLESPACE pg_default;

ALTER TABLE {schemaServiceTable}
    OWNER to {sql.User};
");
                    service.Event($"Added table {schemaServiceTable}");
                }

                registers.Add((schemaFunction, tableFullName, schema, function, name));

                // create tup function if not
BEGIN
insert into {schemaServiceTable}(s_tag) values('{name}');
return new;
END
$BODY$;
ALTER FUNCTION {schemaFunction}()
OWNER TO {sql.User};
");
                    service.Event($"Added function {schemaFunction}");
                }
            }

            // ADD TRIGGER
            foreach (var (schemaFunction, schemaName, _, _, _) in registers)
            {
                if (!currentTriggers.ToITuple().Contains(schemaName, 0))
                {
                    admin.Execute($@"
CREATE TRIGGER {SqlService.ServiceTrigger}
AFTER INSERT OR DELETE OR UPDATE 
ON {schemaName}
FOR EACH ROW
EXECUTE PROCEDURE {schemaFunction}();
");
                    service.Event($"Added trigger {SqlService.ServiceTrigger} on {schemaName}");
                }
            }

            // DELETE TRIGGERS
            foreach (var tup in currentTriggers)
            {
                if (!registers.ToITuple().Contains(tup.Item1, 1))
                {
                    var schemaName = tup.Item1;
                    admin.Execute($"DROP TRIGGER {SqlService.ServiceTrigger} ON {schemaName}");
                    service.Event($"Removed trigger {SqlService.ServiceTrigger} on {schemaName}");
                }
            }

            // DELETE TRIGGER FUNCTIONS

            foreach (var tup in currentTriggerFunctions)
            {
                if (!registers.ToITuple().Contains(tup.Item1, 0))
                {
                    var schemafunction = tup.Item1;
                    admin.Execute($"DROP FUNCTION {schemafunction}()");
                    service.Event($"Removed function {schemafunction}");
                }
            }
            */
        }

        #endregion
    }
}

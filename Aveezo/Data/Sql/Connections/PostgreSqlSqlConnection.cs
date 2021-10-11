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

        public string FormatReturning(bool output) => output ? " returning *" : "";

        // Virtual

        public override string FormatTable(string name, string alias, float tableSample) => $"{name}{(tableSample > 0 ? $" tablesample system({tableSample})" : "")}{alias.Cast(s => $" {s}")}";

        public override Type OverrideType(Type type)
        {
            if (type == typeof((IPAddress, int))) return typeof(IPAddressCidr);
            else return type;
        }

        public override string InsertEndStatement(string table, string[] columns, bool output) => FormatReturning(output);

        public override string UpdateSetWhere(string table, string set, string where, bool output) => $"update {table} set {set}{Where(where)}{FormatReturning(output)}";

        public override string UpdateTableWhere(string table, string whereColumn, object[] whereKeys, bool output) => $"where {whereColumn} in {FormatQuery(whereKeys)}{FormatReturning(output)}";

        public override string DeleteFromWhere(string table, string where, bool output) => $"delete from {table}{Where(where)}{FormatReturning(output)}";

        public override string DeleteTableFromWhere(string table, string whereColumn, object[] whereKeys, bool output) => $"delete from {table} where {whereColumn} in {FormatQuery(whereKeys)}{FormatReturning(output)}";

        // Abstract

        public override bool GetPrimaryKeyColumn(string table, out string columnName)
        {
            columnName = null;

            var result = new SqlResultCollection();

            Query(@$"
SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
WHERE i.indrelid = '{table}'::regclass AND i.indisprimary;
", result, SqlQueryType.Reader, out _, 10000);

            if (result)
            {
                columnName = result[0][0]["attname"].GetString();
                return true;
            }
            else
                return false;
        }

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

        public override void OnServiceCreated(SqlService service)
        {
            var sql = service.sql;
            var admin = sql.Admin;

            admin.Execute($@"
CREATE TABLE {DefaultSchema}.{SqlService.ServiceTable}
(
    {SqlService.ServiceColumnId} bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    {SqlService.ServiceColumnTimestamp} timestamp without time zone NOT NULL DEFAULT now(),
    {SqlService.ServiceColumnTag} character varying COLLATE pg_catalog.""default"",
    CONSTRAINT {SqlService.ServiceTable}_pkey PRIMARY KEY({SqlService.ServiceColumnId})
)

TABLESPACE pg_default;

ALTER TABLE {DefaultSchema}.{SqlService.ServiceTable}
    OWNER to {sql.User};
");
        }

        public override void OnServiceStarted(SqlService service, string[] registers)
        {
            var sql = service.sql;
            var admin = sql.Admin;

            var pgproc = admin["pg_proc"];
            var pgnamespace = admin["pg_namespace"];

            var triggerfunctionselect = admin
                .Select(SqlColumn.Concat("schemaandname", pgnamespace["nspname"], ".", pgproc["proname"]), pgnamespace["nspname"], pgproc["proname"]).From(pgproc)
                .Join(SqlJoinType.Left, pgnamespace, pgproc["pronamespace"], pgnamespace["oid"])
                .Where(pgproc["proname"] % $"{SqlService.ServiceTriggerFunctionPrefix}%");

            var regs = new List<(string, string, string, string, string)>();

            var rc = triggerfunctionselect.Execute();

            var triggerfunctions = rc.First.ToList<string, string, string>("schemaandname", "nspname", "proname");

            foreach (var reg in registers)
            {
                var dotindex = reg.IndexOf('.');
                string schema, table;

                if (dotindex > -1)
                {
                    schema = reg.Substring(0, dotindex);
                    table = reg.Substring(dotindex + 1);
                }
                else
                {
                    schema = sql.Connection.DefaultSchema;
                    table = reg;
                }

                var function = $"{SqlService.ServiceTriggerFunctionPrefix}{table}";
                var schemafunction = $"{schema}.{function}";
                var schematable = $"{schema}.{table}";

                regs.Add((schemafunction, schematable, schema, function, table));

                if (!triggerfunctions.Cast().Contains(schemafunction, 0))
                {
                    admin.Execute($@"
CREATE FUNCTION {schemafunction}()
RETURNS trigger
LANGUAGE 'plpgsql'
COST 100
VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
insert into {SqlService.ServiceTable}(s_tag) values('{table}');
return new;
END
$BODY$;

ALTER FUNCTION {schemafunction}()
OWNER TO {sql.User};
");
                    service.Event($"Added {schemafunction}");
                }
            }

            var triggers = admin["information_schema.triggers"];

            var triggertableselect = admin
                .Select(SqlSelectOptions.Distinct, SqlColumn.Concat("schemaandname", triggers["trigger_schema"], ".", triggers["event_object_table"]), triggers["trigger_schema"], triggers["event_object_table"]).From(triggers)
                .Where(triggers["trigger_name"] == $"{SqlService.ServiceTrigger}");

            var tabletriggers = triggertableselect.Execute().First.ToList<string, string, string>("schemaandname", "trigger_schema", "event_object_table");

            foreach (var (schemafunction, schematable, _, _, _) in regs)
            {
                if (!tabletriggers.Cast().Contains(schematable, 0))
                {
                    admin.Execute($@"
CREATE TRIGGER {SqlService.ServiceTrigger}
AFTER INSERT OR DELETE OR UPDATE 
ON {schematable}
FOR EACH ROW
EXECUTE PROCEDURE {schemafunction}();
");
                    service.Event($"Added {SqlService.ServiceTrigger} on {schematable}");
                }
            }

            foreach (var tabletrigger in tabletriggers)
            {
                if (!regs.Cast().Contains(tabletrigger.Item1, 1))
                {
                    var schematable = tabletrigger.Item1;
                    admin.Execute($"DROP TRIGGER {SqlService.ServiceTrigger} ON {schematable}");
                    service.Event($"Removed {SqlService.ServiceTrigger} on {schematable}");
                }
            }

            foreach (var triggerfunction in triggerfunctions)
            {
                if (!regs.Cast().Contains(triggerfunction.Item1, 0))
                {
                    var schemafunction = triggerfunction.Item1;
                    admin.Execute($"DROP FUNCTION {schemafunction}()");
                    service.Event($"Removed {schemafunction}");
                }
            }
        }

        #endregion
    }
}

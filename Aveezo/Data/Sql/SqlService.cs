using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

using Aveezo;

namespace Aveezo
{
    public sealed class SqlService : Service
    {
        #region Fields

        internal const string ServiceIdent = "__avz_service";
        internal const string ServiceColumnId = "s_id";
        internal const string ServiceColumnTimestamp = "s_timestamp";
        internal const string ServiceColumnTag = "s_tag";

        internal Sql Sql;

        private Dictionary<string, DateTime> schemas = new Dictionary<string, DateTime>();

        public event EventHandler<SqlServiceTriggerEventArgs> Triggered;

        private readonly List<SqlTable> registeredTables = new();

        #endregion

        #region Constructors

        public SqlService(Sql sql) : this(sql, sql.DefaultSchema) { }

        public SqlService(Sql sql, string serviceSchema)
        {
            Sql = sql ?? throw new ArgumentNullException(nameof(sql));
            if (sql.User == null) throw new NullReferenceException("sql.User is required");
            if (sql.Admin == null) throw new NullReferenceException("sql.Admin is required");

            OnInit(async () =>
            {
                Event($"INIT:Database Service");
                Event($"Service schema:{serviceSchema}");

                var validTables = (List<SqlTable>)registeredTables
                    .Each(o => { o.Schema ??= Sql.DefaultSchema; })
                    .Keep(o => Sql.IsTableExists(o))
                    .Unique(o => Sql.Connection.FormatTable(o));

                schemas.Clear();

                foreach (var table in validTables)
                {
                    var schema = table.Schema;

                    if (!schemas.ContainsKey(schema)) 
                        schemas.Add(schema, DateTime.MinValue);
                }

                Event($"Watching {registeredTables.Count.FormatNumber("{0} table", "{0} tables")} in {schemas.Count.FormatNumber("{0} schema", "{0} schemas")}");

                sql.Connection.Service(this, serviceSchema, validTables.ToArray());

                //foreach (var (schema, _) in schemas)
                //{
                //    var mostRecentResult = Sql.Select(ServiceColumnTimestamp)
                //        .From($"{schema}.{ServiceTable}")
                //        .Limit(1)
                //        .OrderBy(ServiceColumnTimestamp, Order.Descending)
                //        .Execute();

                //    if (mostRecentResult)
                //        schemas[schema] = ((SqlCell)mostRecentResult).GetDateTime();
                //}
            });
            OnLoop(async () =>
            {
                foreach (var (schema, _) in schemas)
                {
                   // var ns = sql.SelectToList($"{schema}.{ServiceTable}", (SqlColumn)ServiceColumnTimestamp > last, SqlOrder.By(ServiceColumnTimestamp, Order.Descending));
                }




                return;

                //var ns = sql.SelectToList(ServiceTable, (SqlColumn)ServiceColumnTimestamp > last, SqlOrder.By(ServiceColumnTimestamp, Order.Descending));

                //if (ns.Count > 0)
                //{
                //    last = ns[0][ServiceColumnTimestamp].GetDateTime();

                //    var tags = new List<string>();

                //    foreach (SqlRow row in ns)
                //    {
                //        var tag = row[ServiceColumnTag].GetString();
                //        if (tag != null && !tags.Contains(tag))
                //            tags.Add(tag);
                //    }

                //    Trigger(tags.ToArray());
                //}
            });

            //// REWRITE
            ///
            //if (!Sql.IsTableExists(ServiceTable)) 
            //    Sql.Connection.OnServiceCreated(this);

            //// set most recent notification time
            //var select = Sql.Select(ServiceColumnTimestamp).From(ServiceTable).Limit(1).OrderBy(ServiceColumnTimestamp, Order.Descending);

            //var result = select.Execute();
            //if (result)
            //    last = ((SqlCell)result).GetDateTime();

            //Started += SqlService_Started;
        }

        #endregion

        #region Methods

        public void Trigger(params string[] tags)
        {
            foreach (var tag in tags)
            {
                Triggered?.Invoke(this, new SqlServiceTriggerEventArgs(tag));
            }
        }

        public void RegisterTable(params SqlTable[] tables)
        {
            if (!IsStarted)
            {
                foreach (var table in tables)
                    registeredTables.Add(table);
            }
            else
            {
                Event("Can't register new table, the service has already started.");
            }
        }

        #endregion
    }
}

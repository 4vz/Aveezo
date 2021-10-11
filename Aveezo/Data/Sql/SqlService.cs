using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aveezo;

namespace Aveezo
{
    public sealed class SqlService : Service
    {
        #region Fields

        internal const string ServiceTable = "avz_service";
        internal const string ServiceTrigger = "avz_service";
        internal const string ServiceTriggerFunctionPrefix = "avz_service_";
        internal const string ServiceColumnId = "s_id";
        internal const string ServiceColumnTimestamp = "s_timestamp";
        internal const string ServiceColumnTag = "s_tag";

        internal Sql sql;

        private bool enabled = false;

        private DateTime last = DateTime.MinValue;

        public event EventHandler<SqlServiceTriggerEventArgs> Triggered;

        private readonly List<string> registers = new List<string>();

        #endregion

        #region Constructors

        public SqlService(Sql sql) : base(10000)
        {
            this.sql = sql ?? throw new ArgumentNullException("sql");
            if (sql.User == null) throw new NullReferenceException("sql.User is required");
            if (sql.Admin == null) throw new NullReferenceException("sql.Admin is required");

            enabled = true;

            if (!sql.IsTableExists(ServiceTable)) 
                sql.Connection.OnServiceCreated(this);

            //// set most recent notification time
            var select = sql.Select(ServiceColumnTimestamp).From(ServiceTable).Limit(1).OrderBy(ServiceColumnTimestamp, Order.Descending);

            var result = select.Execute();
            if (result)
                last = ((SqlCell)result).GetDateTime();
        }

        #endregion

        #region Methods

        protected override async Task OnLoop()
        {
            if (!enabled) return;

            var ns = sql.SelectToList(ServiceTable, (SqlColumn)ServiceColumnTimestamp > last, SqlOrder.By(ServiceColumnTimestamp, Order.Descending));

            if (ns.Count > 0)
            {
                last = ns[0][ServiceColumnTimestamp].GetDateTime();

                var tags = new List<string>();

                foreach (SqlRow row in ns)
                {
                    var tag = row[ServiceColumnTag].GetString();
                    if (tag != null && !tags.Contains(tag))
                        tags.Add(tag);
                }

                foreach (var tag in tags)
                {
                    Triggered?.Invoke(this, new SqlServiceTriggerEventArgs(tag));
                }
            }
        }

        protected override void OnStarted()
        {
            if (!enabled) return;

            Event($"Watching {registers.Count} table(s)");

            sql.Connection.OnServiceStarted(this, registers.ToArray());

            Event("Database service has been started");
        }

        public void Trigger(string tag) => Trigger(new[] { tag });
        
        public void Trigger(string[] tags)
        {
            if (!enabled) return;

            foreach (var tag in tags)
                Triggered?.Invoke(this, new SqlServiceTriggerEventArgs(tag));
        }

        public void RegisterTrigger(string table)
        {
            if (!enabled) return;

            if (!IsStarted)
            {
                registers.Add(table);
            }
        }

        #endregion
    }
}

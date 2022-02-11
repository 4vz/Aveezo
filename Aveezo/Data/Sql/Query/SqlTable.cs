using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class SqlTable 
    {
        #region Fields

        public bool IsStatement { get; private set; }

        public string Schema { get; } = null;

        /// <summary>
        /// If IsStatement is false, gets the name of the table. Otherwise gets the inner statement.
        /// </summary>
        public string Name { get; private set; }

        public string Ident => $"{Schema.Format(schema => $"{schema}.")}{Name}";

        public float TableSample { get; set; } = 0;

        public string Alias { get; set; }

        public SqlColumn this[string name]
        {
            get => new(this, name);
        }

        public SqlColumn this[string name, string alias]
        {
            get => new(this, name, alias);
        }

        #endregion

        #region Constructors

        internal SqlTable() : this(null)
        {
            IsStatement = true;
        }

        public SqlTable(string name) : this(name, 0)
        {
        }

        public SqlTable(string name, float sample)
        {
            TableSample = sample;

            // Split name to schema name
            if (name != null && name.Length > 0)
            {
                var dot = name.IndexOf('.');

                if (name.Length > 2 && dot > -1)
                {
                    Schema = name.Substring(0, dot);
                    Name = name.Substring(dot + 1);
                }
                else
                    Name = name;
            }
            
            Alias = $"t{Rnd.Int(10000, 99999)}";
            IsStatement = false;
        }

        #endregion

        #region Operators

        public static implicit operator SqlTable(string name)
        {
            return new SqlTable(name);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets new table based on this table with specified statement;
        /// </summary>
        public SqlTable GetStatement(string statement)
        {
            var table = new SqlTable(); // create new statement table

            // set statement
            table.Name = statement;

            // get alias from original table
            table.Alias = Alias;

            return table;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class SqlTable 
    {
        #region Fields

        public string Schema { get; }

        public string Name { get; internal set; }

        public string Alias { get; }

        public bool IsStatement { get; }

        public float TableSample { get; set; } = 0;

        public SqlColumn this[string name] => new(this, name);

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

        public SqlColumn All()
        {
            return new SqlColumn(this);
        }

        public string GetDefinition() => $"{(IsStatement ? "(" : "")}{(Schema != null ? $"{Schema}." : "" )}{Name}{(IsStatement ? ")" : "")}";

        public override string ToString() => $"{Alias}";

        #endregion

        #region Statics


        #endregion
    }
}

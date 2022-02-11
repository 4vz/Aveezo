using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public interface IDatabaseService
    {
        public Sql Sql(string name);
    }

    public class DatabaseService : IDatabaseService
    {
        #region Fields
        
        private ApiOptions Options { get; }

        private readonly Dictionary<string, Sql> sqls = new();

        #endregion

        #region Constructors

        public DatabaseService(IOptions<ApiOptions> container)
        {
            Options = container.Value;
        }

        #endregion

        #region Methods

        public Sql Sql(string name)
        {
            if (name == null)
            {
                return Sql(Options.DatabaseConfigName);
            }
            else
            {
                if (sqls.ContainsKey(name))
                    return sqls[name];
                else
                {
                    lock (sqls)
                    {
                        var dx = Aveezo.Sql.Load(Options.Config, name);
                        if (dx != null)
                        {
                            sqls.Add(name, dx);
                            return dx;
                        }
                        else
                            return null;
                    }
                }
            }
        }

        #endregion
    }
}

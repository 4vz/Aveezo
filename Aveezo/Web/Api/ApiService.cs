using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Aveezo
{
    public abstract class ApiService
    {
        #region Fields

        protected IServiceProvider Provider { get; }

        private IDatabaseService databaseService;

        protected ApiOptions Options { get; }

        protected Sql Sql { get; }

        private readonly Dictionary<string, Sql> sqls = new();

        #endregion

        #region Constructors

        public ApiService(IServiceProvider provider)
        {
            Provider = provider;

            Options = Provider.GetService<IOptions<ApiOptions>>().Value;

            databaseService = provider.GetService<IDatabaseService>();

            Sql = databaseService.Sql(null);
        }

        #endregion

        #region Methods

        public Sql SqlLoad(string name) => databaseService.Sql(name);

        #endregion
    }
}

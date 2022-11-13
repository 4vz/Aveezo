using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class ApiOptions
    {
        #region Fields

        public Config Config { get; set; }

        public string DatabaseConfigName { get; set; }

        /// <summary>
        /// The name will be the prefix of group name of controllers depicted right before (V)ersion, in their namespace. If ommited, the group name will always null. For example:<br />
        /// <br />
        /// If value set to <b>Providers</b><br />
        /// Namespace: Aveezo.Providers.Sales.V1.Tasks, Group Name: Sales, Version: V1<br />
        /// Namespace: Aveezo.Controllers.Providers.V2.Report, Group Name: null, Version: V2<br />
        /// Namespace: Aveezo.Sales.V1.Tasks, Group Name: null, Version: V1<br />
        /// Namespace: Finance.Insights, Group Name: null, Version: V1<br />
        /// Namespace: Finance.Providers.Insights, Group Name: Insights, Version: V1<br />
        /// Namespace: Sales.V1.Providers.Insights, Group Name: null, Version: V1<br />
        /// <br />
        /// Default: Controllers
        /// </summary>
        public string ControllerGroupNamespacePrefix { get; set; } = "Controllers";

        public bool EnableAuth { get; set; } = false;

        public string AuthSecret { get; set; }

        public int AuthAccessTokenExpire { get; set; } = 300;

        public int AuthRefreshTokenExpire { get; set; } = 31536000;

        public bool EnableSoapXml { get; set; } = false;

        public string XmlPrefix { get; set; } = "xn";

        public string WsdlDomain { get; set; } = "aveezo.io";

        public bool EnableDocs { get; set; } = false;

        public string DocsName { get; set; } = "Aveezo API";

        public string RoutePrefix { get; set; } = "/docs/api";

        public string[] AuthAvailableParameters { get; set; }

        #endregion

        #region Methods

        public void CopyFrom(ApiOptions options)
        {
            foreach (var property in options.GetType().GetProperties())
            {
                property.SetValue(this, property.GetValue(options));
            }
        }

        #endregion
    }
}

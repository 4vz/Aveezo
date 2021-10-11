using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public sealed class XmlPrefix
    {
        #region Fields

        public string Local { get; set; } = "avz";

        public string LocalNamespace { get; set; } = "http://aveezo.io/xml/schema";

        public string LocalDomain { get; set; } = "aveezo.io";

        public string XmlDefinition { get; set; } = "xsd";

        public string XmlDefinitionNamespace { get; set; } = "http://www.w3.org/2001/XMLSchema";

        public string XmlInstance { get; set; } = "xsi";

        public string XmlInstanceNamespace { get; set; } = "http://www.w3.org/2001/XMLSchema-instance";

        public Dictionary<string, string> Namespaces { get; } = new Dictionary<string, string>();

        #endregion

        #region Constructors

        public XmlPrefix()
        {
            Namespaces.Add("soap", "http://www.w3.org/2003/05/soap-envelope");
        }

        #endregion

    }
}

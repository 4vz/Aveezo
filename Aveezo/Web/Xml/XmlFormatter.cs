using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public abstract class XmlOutputFormatter : TextOutputFormatter
    {
        #region Fields

        protected readonly XmlPrefix
            xmlPrefix;

        protected readonly TypeRepository typeCollections;

        #endregion

        #region Constructors

        public XmlOutputFormatter(XmlPrefix xmlPrefix, TypeRepository typeCollections)
        {
            this.xmlPrefix = xmlPrefix;
            this.typeCollections = typeCollections;

            SupportedMediaTypes.Clear();
            SupportedEncodings.Clear();
        }

        #endregion
    }

    public abstract class XmlInputFormatter : TextInputFormatter
    {
        #region Fields

        protected readonly XmlPrefix xmlPrefix;

        protected readonly TypeRepository typeCollections;

        #endregion

        #region Constructors

        public XmlInputFormatter(XmlPrefix xmlPrefix, TypeRepository typeCollections)
        {
            this.xmlPrefix = xmlPrefix;
            this.typeCollections = typeCollections;

            SupportedMediaTypes.Clear();
            SupportedEncodings.Clear();
        }

        #endregion
    }
}

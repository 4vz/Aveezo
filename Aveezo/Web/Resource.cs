using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aveezo
{
    public abstract class Resource
    {
        #region Fields

        /// <summary>
        /// Id in Base64-URL encoded.
        /// </summary>
        public string Id { get; set; }

        [Hide]
        public Link[] Links { get; set; }

        #endregion
    }

    public sealed class Link
    {
        public string Rel { get; set; }

        public string Href { get; set; }
    }
}

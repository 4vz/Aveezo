using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aveezo;

public abstract class Resource
{
    #region Fields

    /// <summary>
    /// Id in Base64-URL encoded.
    /// </summary>
    [JsonPropertyOrder(-1)]
    [Field(Sql.Id, FieldOptions.Always | FieldOptions.HideInFields | FieldOptions.Encoded)]
    public string Id { get; set; }

    [Hide]
    [JsonPropertyName("_links")]
    public Dictionary<string, ResourceLink> Links { get; set; }

    #endregion

    #region Method

    public virtual SqlSelect Select(Sql sql, Parameters parameters)
    {
        return null;
    }

    #endregion
}

public sealed class ResourceLink
{
    public string Method { get; set; }

    public string Href { get; set; }
}

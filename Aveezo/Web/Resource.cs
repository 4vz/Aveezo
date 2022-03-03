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
    [Field(Sql.Id, FieldOptions.Always | FieldOptions.HideInFields)]
    public string Id { get; set; }

    [Hide]
    public Dictionary<string, ResourceLink> _Links { get; set; }

    #endregion

    #region Method

    public virtual SqlSelect Select(Sql sql, object[] parameters)
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

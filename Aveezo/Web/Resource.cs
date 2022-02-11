using System;
using System.Collections.Generic;
using System.ComponentModel;

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
    public string Id { get; set; }

    [Hide]
    public Dictionary<string, ResourceLink> _Links { get; set; }

    public static object Null { get; } = new DataObject("NULL");

    public static object NotNull { get; } = new DataObject("NOTNULL");

    public static object Cancel { get; } = new DataObject("CANCEL");

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

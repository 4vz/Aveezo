using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FieldAttribute : Attribute
{
    #region Fields

    public string Name { get; }

    public FieldOptions Options { get; }

    #endregion

    #region Constructors

    /// <param name="name">The name of the field. If using SelectBuilder, it should match with hostBuilder name.</param>
    /// <param name="options">Options for the fields.</param>
    public FieldAttribute(string name, FieldOptions options)
    {
        Name = name.ToLower();
        Options = options;
    }

    public FieldAttribute(string name) : this(name, FieldOptions.None)
    {
    }

    #endregion
}

[Flags]
public enum FieldOptions
{
    None = 0,
    Default = 1,
    FieldsOnly = 2,
    Always = Default | FieldsOnly,
    CanQuery = 4,
    CanSort = 8,
    Encoded = 16,
    HideInFields = 32
} 
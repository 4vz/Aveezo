using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreAttribute : Attribute
    {
        #region Fields

        public IgnoreCondition Condition { get; set; } = IgnoreCondition.Always;

        #endregion

        #region Constructors

        public IgnoreAttribute()
        {
        }

        public IgnoreAttribute(IgnoreCondition condition)
        {
            Condition = condition;
        }


        #endregion
    }

    public enum IgnoreCondition
    {
        Always,
        WhenWritingNull
    }

}

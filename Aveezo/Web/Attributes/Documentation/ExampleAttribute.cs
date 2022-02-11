using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class ExampleAttribute : Attribute
    {
        #region Fields

        public object Example { get; }

        #endregion

        #region Constructors

        public ExampleAttribute(object example)
        {
            Example = example;
        }

        public ExampleAttribute(params object[] examples)
        {
            Example = examples;
        }

        #endregion
    }
}

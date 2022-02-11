using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public interface IMethodResult
    {
        int Total { get; set; }

        int Count { get; set; }

        int Offset { get; set; }
    }

    public class MethodResult<T> : IMethodResult
    {
        #region Fields

        public int Total { get; set; } = 0;

        public int Count { get; set; } = 0;

        public int Offset { get; set; } = 0;

        public string[] Fields { get; set; } = null;

        public T Result { get; set; }

        #endregion

        #region Constructors

        public MethodResult()
        {

        }

        #endregion
    }
}

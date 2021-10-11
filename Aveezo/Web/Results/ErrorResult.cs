using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class ErrorResult
    {
        #region Fields

        public ErrorResultEntry[] Entries { get; set; }

        #endregion

        #region Constructors

        public ErrorResult()
        {

        }

        public ErrorResult(string source, string error)
        {
            Entries = new ErrorResultEntry { Source = source, Errors = error.Array() }.Array();
        }

        public ErrorResult(string source, string[] errors)
        {
            Entries = new ErrorResultEntry { Source = source, Errors = errors }.Array();
        }

        #endregion
    }

    public class ErrorResultEntry
    {
        #region Fields

        public string Source { get; set; }

        public string[] Errors { get; set; }

        #endregion
    }
}

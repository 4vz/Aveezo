using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class ErrorResult
    {
        #region Fields

        public ErrorResultError Error { get; set; }

        #endregion

        #region Constructors

        public ErrorResult(int code, string source, string status, string message)
        {
            Error = new ErrorResultError { Code = code, Source = source, Status = status, Message = message };
        }

        #endregion
    }

    public class ErrorResultError
    {
        public int Code { get; set; }

        // source of error
        public string Source { get; set; } 

        // status of error
        public string Status { get; set; }

        public string Message { get; set; } 

        public Dictionary<string, string[]> Details { get; set; }
    }
}

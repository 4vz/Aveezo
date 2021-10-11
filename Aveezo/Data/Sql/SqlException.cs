using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public enum SqlExceptionType
    {
        Unspecified,
        LoginFailed,
        InsertFailedExplicitIdentity,
        Timeout
    }

    public class SqlException
    {
        #region Fields

        public Exception Exception { get; private set; } = null;

        public string Sql { get; private set; } = null;

        public SqlExceptionType Type { get; internal set; } = SqlExceptionType.Unspecified;

        #endregion

        #region Constructors

        public SqlException(Exception exception, string sql)
        {
            Exception = exception;
            Sql = sql;
        }

        #endregion
    }

    public delegate void SqlExceptionEventHandler(object sender, SqlExceptionEventArgs eventArgs);

    public class SqlExceptionEventArgs : EventArgs
    {
        #region Fields

        public SqlException Exception { get; internal set; } = null;

        #endregion

        #region Constructor

        public SqlExceptionEventArgs(SqlException exception)
        {            
            Exception = exception;
        }

        #endregion
    }


}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public interface IPagingResult
    {
        int Total { get; set; }

        int Count { get; set; }

        int Offset { get; set; }
    }

    public class PagingResult<T> : IPagingResult
    {
        #region Fields

        public int Total { get; set; } = 0;

        public int Count { get; set; } = 0;

        public int Offset { get; set; } = 0;

        public T Result { get; set; }

        #endregion

        #region Constructors

        public PagingResult()
        {

        }

        #endregion
    }
}

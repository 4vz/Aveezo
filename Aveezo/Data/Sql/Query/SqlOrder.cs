using System;
using System.Collections;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class SqlOrder : IEnumerable<(SqlColumn, Order)>
    {
        #region Fields

        private readonly List<(SqlColumn, Order)> orders = new();

        internal (SqlColumn, Order)[] Orders => orders.ToArray();

        public int Count => orders.Count;

        #endregion

        #region Constructors

        public SqlOrder()
        {
        }

        #endregion

        #region Operators

        public static SqlOrder operator +(SqlOrder order1, SqlOrder order2)
        {
            if (order1 == null && order2 == null) throw new ArgumentNullException();

            var order = new SqlOrder();

            if (order1 != null)
            {
                foreach (var (col, ord) in order1)
                {
                    order.Add(col, ord);
                }
            }
            if (order2 != null)
            {
                foreach (var (col, ord) in order2)
                {
                    order.Add(col, ord);
                }
            }

            return order;
        }

        #endregion

        #region Methods

        public void Insert(int index, SqlColumn column, Order order) => orders.Insert(index, (column, order));

        public void Add(SqlColumn column, Order order)
        {
            if (!column.IsAll)
                orders.Add((column, order));
        }

        public IEnumerator<(SqlColumn, Order)> GetEnumerator() => orders.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Statics

        public static SqlOrder By(SqlColumn column, Order order) => new SqlOrder { { column, order } };

        #endregion
    }
}

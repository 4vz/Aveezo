using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Aveezo
{
    public class SqlCondition
    {
        #region Fields
        internal SqlCondition Condition1 { get; } = null;
        internal SqlCondition Condition2 { get; } = null;
        internal SqlColumn Column { get; }
        internal object Value { get; } = null;
        internal SqlComparasionOperator ComparativeOperator { get; } = SqlComparasionOperator.EqualTo;
        internal SqlConjunctionOperator BooleanOperator { get; } = SqlConjunctionOperator.And;

        #endregion

        #region Constructors

        private SqlCondition(SqlCondition condition1, SqlConjunctionOperator booleanOperator, SqlCondition condition2)
        {
            Condition1 = condition1;
            BooleanOperator = booleanOperator;
            Condition2 = condition2;
        }

        private SqlCondition(SqlColumn column, SqlComparasionOperator comparativeOperator, object value)
        {
            Column = column;
            ComparativeOperator = comparativeOperator;
            Value = value;
        }

        internal SqlCondition(object value)
        {
            Value = value;
        }

        internal SqlCondition() { }

        private static SqlCondition Conjunction(SqlCondition condition1, SqlConjunctionOperator conjunction, SqlCondition condition2)
        {
            if (condition1 is not null && condition2 is not null)
                return new SqlCondition(condition1, conjunction, condition2);
            else if (condition1 is not null)
                return condition1;
            else if (condition2 is not null)
                return condition2;
            else
                return null;
        }

        public static SqlCondition operator &(SqlCondition condition1, SqlCondition condition2) => Conjunction(condition1, SqlConjunctionOperator.And, condition2);

        public static SqlCondition operator |(SqlCondition condition1, SqlCondition condition2) => Conjunction(condition1, SqlConjunctionOperator.Or, condition2);

        // SqlCondition is not either true or false, so then we can evaluate both left and right operand

        public static bool operator true(SqlCondition condition) => false;

        public static bool operator false(SqlCondition condition) => false;

        public static SqlCondition operator ==(SqlCondition column, object value)
        {
            if ((value is Array array && array.Length == 0) || (value is IList list && list.Count == 0))
                return new SqlCondition((SqlColumn)column, SqlComparasionOperator.EqualTo, null);
            else if (value is Array || value is IList || value is ITuple)
                return new SqlCondition((SqlColumn)column, SqlComparasionOperator.In, value);
            else
                return new SqlCondition((SqlColumn)column, SqlComparasionOperator.EqualTo, value);
        }

        public static SqlCondition operator !=(SqlCondition column, object value)
        {
            if ((value is Array array && array.Length == 0) || (value is IList list && list.Count == 0))
                return new SqlCondition((SqlColumn)column, SqlComparasionOperator.NotEqualTo, null);
            else if (value is Array || value is IList || value is ITuple)
                return new SqlCondition((SqlColumn)column, SqlComparasionOperator.NotIn, value);
            else
                return new SqlCondition((SqlColumn)column, SqlComparasionOperator.NotEqualTo, value);
        }

        public static SqlCondition operator %(SqlCondition column, object value) => new((SqlColumn)column, SqlComparasionOperator.Like, value as string);

        public static SqlCondition operator ^(SqlCondition column, object value) => new((SqlColumn)column, SqlComparasionOperator.NotLike, value as string);

        public static SqlCondition operator <(SqlCondition column, object value) => new((SqlColumn)column, SqlComparasionOperator.LessThan, value);

        public static SqlCondition operator >(SqlCondition column, object value) => new((SqlColumn)column, SqlComparasionOperator.GreaterThan, value);

        public static SqlCondition operator <=(SqlCondition column, object value) => new((SqlColumn)column, SqlComparasionOperator.LessThanOrEqualTo, value);

        public static SqlCondition operator >=(SqlCondition column, object value) => new((SqlColumn)column, SqlComparasionOperator.GreaterThanOrEqualTo, value);

        //public static implicit operator SqlCondition(bool value) => new(value);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is null)
            {
                return false;
            }

            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public enum SqlComparasionOperator
    {
        EqualTo,
        NotEqualTo,
        LessThanOrEqualTo,
        GreaterThanOrEqualTo,
        LessThan,
        GreaterThan,
        Like,
        NotLike,
        In,
        NotIn
    }

    public enum SqlConjunctionOperator
    {
        And,
        Or
    }
}

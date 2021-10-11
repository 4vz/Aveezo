using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aveezo
{
    public sealed class SqlSelectProto : SqlBase
    {
        #region Fields

        public SqlSelectOptions Options { get; set; }

        public SqlColumn[] Columns { get; set; }

        #endregion

        #region Constructors

        public SqlSelectProto(Sql database, SqlSelectOptions options, params SqlColumn[] columns) : base(database)
        {
            Options = options;
            Columns = columns;
        }

        public SqlSelectProto(Sql database, params SqlColumn[] columns) : this(database, SqlSelectOptions.None, columns)
        {
        }

        #endregion

        #region Methods

        public SqlSelect From(SqlTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));

            SqlColumn assumingKey = null;
            if (Columns.Length > 0 && Columns[0].Table != null) assumingKey = Columns[0];

            var select = new SqlSelect(Database, table);

            Sql.SetTableWhenUnassigned(Columns, table);

            select.SelectColumns = Columns;
            select.Options = Options;
            select.KeyColumn = assumingKey;

            return select;
        }

        public SqlSelect From(SqlSelect select)
        {
            if (select == null) throw new ArgumentNullException(nameof(select));

            var table = new SqlTable();

            Sql.SetTableWhenUnassigned(Columns, table);

            var outerSelect = new SqlSelect(Database, select, table);

            outerSelect.SelectColumns = Columns;
            outerSelect.Options = Options;

            return outerSelect;
        }

        #endregion
    }

    public sealed class SqlSelect : SqlQueryBase
    {
        #region Fields

        private readonly SqlTable table = null;

        private readonly SqlSelect tableSelect = null;

        private readonly SqlTable tableSelectTable = null;

        private readonly SortedDictionary<int, SqlJoin> joins = new();

        public Dictionary<string, SqlColumn> filters = new();

        private int key = 0;

        private int limit = 0;

        private int offset = 0;

        public int LimitLength
        {
            get => limit;
            set
            {
                if (value >= 0) limit = value;
            }
        }

        public int OffsetLength
        {
            get => offset;
            set
            {
                if (value >= 0) offset = value;
            }
        }

        public SqlCondition WhereCondition { get; set; } = null;

        private SqlCondition innerCondition = null;

        internal bool after = false;

        public SqlOrder Order { get; set; }

        public SqlColumn[] SelectColumns { get; set; } = null;

        public SqlSelectOptions Options { get; set; } = SqlSelectOptions.None;

        public SqlColumn KeyColumn { get; set; } = null;

        #endregion

        #region Constructors

        internal SqlSelect(Sql database, SqlTable table) : base(database, SqlQueryType.Reader)
        {
            this.table = table ?? throw new ArgumentNullException("table");
        }

        internal SqlSelect(Sql database, SqlSelect select, SqlTable selectTable) : base(database, SqlQueryType.Reader)
        {
            tableSelect = select;
            tableSelectTable = selectTable;
        }

        #endregion

        #region Methods

        public SqlSelect Columns(params SqlColumn[] columns)
        {
            SelectColumns = columns;
            return this;
        }
          
        public SqlSelect Join(SqlJoinType type, SqlTable table, SqlCondition where, out int index)
        {
            var ikey = key++;

            joins.Add(ikey, new SqlJoin(type, table, where));

            index = ikey;

            return this;
        }

        public SqlSelect Join(SqlJoinType type, SqlTable table, SqlCondition where) => Join(type, table, where, out int _);

        public SqlSelect Join(SqlJoinType type, SqlTable table, SqlColumn whereColumn, object whereValue) => Join(type, table, whereColumn == whereValue);

        public SqlSelect Join(SqlJoinType type, SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(type, table, leftColumn == rightColumn);

        public SqlSelect Join(SqlTable table, SqlCondition where) => Join(SqlJoinType.Inner, table, where);

        public SqlSelect Join(SqlTable table, SqlColumn whereColumn, object whereValue) => Join(SqlJoinType.Inner, table, whereColumn, whereValue);

        public SqlSelect Join(SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(SqlJoinType.Inner, table, leftColumn, rightColumn);

        public SqlSelect LeftJoin(SqlTable table, SqlCondition where) => Join(SqlJoinType.Left, table, where);

        public SqlSelect LeftJoin(SqlTable table, SqlColumn whereColumn, object whereValue) => Join(SqlJoinType.Left, table, whereColumn, whereValue);

        public SqlSelect LeftJoin(SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(SqlJoinType.Left, table, leftColumn, rightColumn);

        public SqlSelect RightJoin(SqlTable table, SqlCondition where) => Join(SqlJoinType.Right, table, where);

        public SqlSelect RightJoin(SqlTable table, SqlColumn whereColumn, object whereValue) => Join(SqlJoinType.Right, table, whereColumn, whereValue);

        public SqlSelect RightJoin(SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(SqlJoinType.Right, table, leftColumn, rightColumn);

        public SqlSelect FullJoin(SqlTable table, SqlCondition where) => Join(SqlJoinType.Full, table, where);

        public SqlSelect FullJoin(SqlTable table, SqlColumn whereColumn, object whereValue) => Join(SqlJoinType.Full, table, whereColumn, whereValue);

        public SqlSelect FullJoin(SqlTable table, SqlColumn leftColumn, SqlColumn rightColumn) => Join(SqlJoinType.Full, table, leftColumn, rightColumn);

        public SqlSelect Where(SqlCondition condition)
        {
            WhereCondition = condition;
            return this;
        }

        public SqlSelect And(SqlCondition condition)
        {
            if (WhereCondition is not null)
                WhereCondition = WhereCondition && condition;
            else 
                throw new InvalidOperationException();      
            
            return this;
        }

        public SqlSelect And(SqlColumn whereColumn, object whereValue) => And(whereColumn == whereValue);

        public SqlSelect Or(SqlCondition condition)
        {
            if (WhereCondition is not null)
                WhereCondition = WhereCondition || condition;
            else 
                throw new InvalidOperationException();

            return this;
        }

        public SqlSelect Or(SqlColumn whereColumn, object whereValue) => Or(whereColumn == whereValue);

        public SqlSelect Where(SqlColumn whereColumn, object whereValue) => Where(whereColumn == whereValue);

        public SqlSelect Where(params Func<Func<SqlCondition, bool>, bool>[] wheres)
        {
            SqlCondition final = null;
            SqlCondition c = null;

            foreach (var where in wheres)
            {
                if (where(aa => { c = aa; return true; }))
                {
                    final = c;
                    break;
                }
            }  

            if (final is not null)
                WhereCondition = final;

            return this;
        }

        public SqlSelect OrderBy(SqlColumn column, Order order)
        {
            Order = SqlOrder.By(column, order);
            return this;
        }

        public SqlSelect OrderBy(params (SqlColumn, Order)[] args)
        {
            var order = new SqlOrder();

            foreach (var (col, ord) in args)
            {
                order.Add(col, ord);
            }

            Order = order;
            return this;
        }

        public SqlSelect Limit(int limit, int offset)
        {
            LimitLength = limit;
            OffsetLength = offset;
            return this;
        }

        public SqlSelect Limit(int limit) => Limit(limit, 0);

        public void Filter<T>(Filter<T> filter, SqlColumn column) => Filter(filter, column, null);

        public void Filter<T>(Filter<T> filter, SqlColumn column, T nullIfFilter) => Filter(filter, column, s => Equals(s, nullIfFilter) ? null : s);

        public void Filter(Filter<string> filter, SqlColumn column, Func<string, object> modifier, StringOptions modifierOptions) => Filter(filter, column, s => modifier(s.Convert(modifierOptions)));

        public void Filter<T>(Filter<T> filter, SqlColumn column, Func<T, object> modifier)
        {
            if (filter != null)
            {
                if (column == null) throw new ArgumentNullException(nameof(column));

                if (filter.Values != null)
                {
                    foreach (var (atr, val) in filter.Values)
                    {
                        SqlCondition newCondition = null;

                        var tvalue = val.Cast<T>();
                        object value;

                        if (modifier != null)
                            value = modifier(tvalue);
                        else
                            value = tvalue;

                        if (value is not null)
                        {
                            if (value is DataObject obj)
                            {
                                if (obj.Data == "NULL")
                                    newCondition = column == null;
                                else if (obj.Data == "NOTNULL")
                                    newCondition = column != null;
                                else if (obj.Data == "DISCARD")
                                    newCondition = false;
                            }
                            else
                            {
                                var isNumeric = value.IsNumeric();

                                if (atr == null)
                                    newCondition = column == value;
                                else if (atr == "like")
                                    newCondition = column % $"%{(value is string ? value : val)}%";
                                else if (atr == "start")
                                    newCondition = column % $"{(value is string ? value : val)}%";
                                else if (atr == "end")
                                    newCondition = column % $"%{(value is string ? value : val)}";
                                else if (atr == "notlike")
                                    newCondition = column ^ $"%{(value is string ? value : val)}%";
                                else if (atr == "not")
                                    newCondition = column != value;
                                else if (isNumeric && atr == "lt")
                                    newCondition = column < value;
                                else if (isNumeric && atr == "gt")
                                    newCondition = column > value;
                                else if (isNumeric && atr == "lte")
                                    newCondition = column <= value;
                                else if (isNumeric && atr == "gte")
                                    newCondition = column >= value;
                                else
                                    newCondition = column == value;
                            }
                        }

                        WhereCondition = WhereCondition && newCondition;
                    }
                }

                filters.Add(filter.Name, column);
            }
        }

        public void Key(SqlColumn key) => KeyColumn = key;

        private SqlTable GetTableStatement()
        {
            SqlTable tableStatement;

            if (tableSelect != null)
            {
                tableSelectTable.Name = tableSelect.Statements[0].Trim();
                tableStatement = tableSelectTable;
            }
            else
                tableStatement = table;

            return tableStatement;
        }
        
        protected override string[] GetStatements()
        {
            if (table != null)
                Sql.SetTableWhenUnassigned(SelectColumns, table);      
            
            return Database.Connection.Select(
                GetTableStatement(), 
                SelectColumns, 
                joins.Values.ToArray(), 
                innerCondition && WhereCondition, 
                Order, limit, offset, Options).Array();
        }

        public int ExecuteCount()
        {
            var dd = Database.Connection.Select(
                GetTableStatement(), 
                new SqlColumn[] { "COUNT(*)" }, 
                joins.Values.ToArray(), 
                WhereCondition, null, 0, 0, Options);

            var rc = Execute(dd);

            if (rc)
            {
                var rci = ((SqlCell)rc).GetInt();
                return rci;
            }

            return 0;
        }

        internal void SetInnerCondition(SqlCondition conditon) => innerCondition = conditon;

        internal SqlColumn GetFilterColumn(string name) => filters.ContainsKey(name) ? filters[name] : null;

        #endregion
    }

    [Flags]
    public enum SqlSelectOptions
    {
        None = 0,
        Distinct = 1,
        Random = 2
    }
}

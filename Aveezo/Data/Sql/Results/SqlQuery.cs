using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public sealed class SqlQuery : IPrintable, IEnumerable<SqlResult>
    {
        #region Fields

        internal Values<string> selectBuilders = null;

        internal SqlSelect select = null;

        private readonly List<SqlResult> results = new List<SqlResult>();

        public int Count => results.Count;

        public SqlResult First => Count > 0 ? this[0] : null;

        public SqlResult this[int index] => index >= 0 && index < Count ? results[index] : null;       

        public TimeSpan ExecutionTime { get; internal set; }
        
        public SqlException Exception { get; internal set; } = null;

        public bool Ok => Exception == null && Count > 0;

        /// <summary>
        /// Gets whether the first result returns empty rows.
        /// </summary>
        public bool NoResult => Count > 0 && results[0].Count == 0;

        #endregion

        #region Constructors

        public SqlQuery()
        {
        }

        #endregion

        #region Operators

        public static implicit operator bool(SqlQuery query) => query.Ok && query.First.Count > 0;

        public static implicit operator SqlResult(SqlQuery query) => query.First;

        public static implicit operator SqlRow(SqlQuery query) => (SqlResult)query;

        public static implicit operator SqlCell(SqlQuery query) => (SqlRow)query;

        public static SqlQuery operator +(SqlQuery query1, SqlQuery query2)
        {
            if (query1 == null || query2 == null)
                throw new ArgumentNullException();

            var result = new SqlQuery();

            // combine resultitem
            foreach (var item in query1)
                result.Add(item);

            foreach (var item in query2)
                result.Add(item);

            // combine exception
            if (query1.Exception != null)
                result.Exception = query1.Exception;
            else if (query2.Exception != null)
                result.Exception = query2.Exception;

            // combine execution time
            result.ExecutionTime = query1.ExecutionTime + query2.ExecutionTime;

            return result;
        }

        public static SqlQuery operator +(SqlQuery query, SqlResult result)
        {
            if (query == null || result == null)
                throw new ArgumentNullException();

            var newcollection = new SqlQuery();

            // combine resultitem
            foreach (var item in query)
                newcollection.Add(item);

            newcollection.Add(result);

            // combine execution time
            newcollection.ExecutionTime = query.ExecutionTime + result.ExecutionTime;

            return newcollection;
        }

        public static SqlQuery operator +(SqlResult result, SqlQuery query) => query + result;

        #endregion

        #region Methods

        public IEnumerator<SqlResult> GetEnumerator() => results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => results.GetEnumerator();

        internal void Add(SqlResult item)
        {
            results.Add(item);
        }

        public string[] Print() => Print(100);

        public string[] Print(int limit)
        {
            if (Exception != null)
            {
                return new string[]{ $"Exception: {Exception.Type}: {Exception.Exception.Message}", $"SQL: {Exception.Sql}" };
            }
            else if (limit > Count && Count > 0)
            {
                var lines = new List<string>();

                int[] splits = (limit - Count).Split(Count);

                var i = 0;
                foreach (var item in results)
                {
                    lines.Add($"Result #{(i + 1)}");

                    var itemLines = item.Print(splits[i]);

                    if (itemLines != null) lines.AddRange(itemLines);

                    i++;
                }

                return lines.ToArray();
            }
            else
                return new[] { "No Result" };
        }

        public T[] Builder<T>(Action<SqlPropertyBuilder<T>> propertyBuilder, Action<SqlItemBuilder<T>> itemBuilder)
        {
            if (Ok && selectBuilders != null && select != null)
            {
                var result = First;
                var list = new List<T>();

                foreach (var row in result)
                {
                    T item = (T)Activator.CreateInstance(typeof(T));
                    Context context = new();

                    Dictionary<string, SqlCell> rowdict = new();
                    foreach (var (name, index) in result.ColumnIndex) rowdict.Add(name, row[index]);

                    List<SqlBuilder> builders = new();
                    Dictionary<string, object> formattedValues = new();

                    // get builders
                    foreach (var columnName in result.ColumnNames)
                    {
                        if (columnName != "___select")
                        {
                            var builder = select.GetBuilder(columnName, row);

                            if (builder != null && builder.Name != null)
                                builders.Add(builder);
                        }
                    }

                    // formatter
                    foreach (var builder in builders)
                    {
                        var cell = row.ContainsKey(builder.Name) ? row[builder.Name] : null;
                        object formattedValue = null;

                        if (builder.Formatter != null)
                        {
                            formattedValue = builder.Formatter(rowdict);

                            if (formattedValue is SqlCell valueCell)
                                formattedValue = valueCell.GetObject();
                        }
                        else if (cell != null)
                        {
                            // default formatter
                            formattedValue = cell.GetObject();
                        }

                        formattedValues.Add(builder.Name, formattedValue);
                    }

                    // binder
                    foreach (var builder in builders)
                    {
                        if (builder.Binder != null)
                        {
                            builder.Binder(item, rowdict, formattedValues);
                        }
                        else
                        {
                            // default binder
                        }
                    }

                    // propertyBuilder
                    if (propertyBuilder != null)
                    {
                        foreach (var builder in builders)
                        {
                            propertyBuilder(new SqlPropertyBuilder<T>
                            {
                                Item = item,
                                Context = context,
                                Builder = builder,
                                Values = rowdict,
                                FormattedValues = formattedValues
                            });
                        }
                    }
                    
                    // invoke itemBuilder
                    itemBuilder?.Invoke(new SqlItemBuilder<T>
                    {
                        Context = context,
                        Item = item,
                        Values = rowdict,
                        FormattedValues = formattedValues
                    });

                    list.Add(item);
                }

                return list.ToArray();
            }
            else 
                return null;
        }

        #endregion
    }

    public sealed class SqlItemBuilder<T>
    {
        public T Item { get; init; }

        public Context Context { get; init; }

        public Dictionary<string, SqlCell> Values { get; init; }

        public Dictionary<string, object> FormattedValues { get; init; }
    }

    public sealed class SqlPropertyBuilder<T>
    {
        public T Item { get; init; }

        public Context Context { get; init; }

        public SqlBuilder Builder { get; init; }

        public Dictionary<string, SqlCell> Values { get; init; }        
        
        public Dictionary<string, object> FormattedValues { get; init; }

        public SqlCell Value => Builder.Name != null ? Values.ContainsKey(Builder.Name) ? Values[Builder.Name] : null : null;

        public object FormattedValue => Builder.Name != null ? Values.ContainsKey(Builder.Name) ? FormattedValues[Builder.Name] : null : null;
    }
}

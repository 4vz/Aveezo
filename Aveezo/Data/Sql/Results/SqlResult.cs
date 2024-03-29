﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aveezo
{
    public delegate void KeyDuplicateEventHandler<T>(T key, SqlRow row);

    public sealed class SqlResult : IPrintable, IEnumerable<SqlRow>
    {
        #region Fields

        public string Sql { get; }

        internal SqlExecuteType Type { get; }

        public bool ExecuteOutput { get; internal set; } = false;

        public TimeSpan ExecutionTime { get; internal set; } = TimeSpan.Zero;

        internal int affectedRows = 0;

        private readonly List<SqlRow> rows = null;

        private readonly List<SqlRow> emptyRows = null;

        public string[] ColumnNames { get; init; } = null;

        public Type[] ColumnTypes { get; init; } = null;

        public Dictionary<string, int> ColumnIndexes { get; init; }

        public SqlRow First => (Type == SqlExecuteType.Reader || Type == SqlExecuteType.Scalar) && Count > 0 ? this[0] : null;

        public SqlRow this[int index] => rows == null ? null : index >= 0 && index < Count ? rows[index] : null;

        /// <summary>
        /// Row count or affected row
        /// </summary>
        public int Count => rows == null ? affectedRows : rows.Count;

        public SqlRow Last => rows == null ? null : Count == 0 ? null : rows[Count - 1];

        #endregion

        #region Constructor

        internal SqlResult(string sql, SqlExecuteType type)
        {
            Sql = sql;
            Type = type;

            if (type == SqlExecuteType.Reader || type == SqlExecuteType.Scalar)
                rows = new List<SqlRow>();
            else
                emptyRows = new List<SqlRow>();
        }

        #endregion

        #region Operators

        public static implicit operator bool(SqlResult result) => result.Count > 0;

        public static implicit operator int(SqlResult result) => result.Count;

        public static implicit operator SqlRow(SqlResult result) => result.First;

        public static implicit operator SqlCell(SqlResult result) => (SqlRow)result;

        public static SqlQuery operator +(SqlResult result1, SqlResult result2)
        {
            if (result1 == null || result2 == null)
                return null;

            var result = new SqlQuery();

            // add items
            result.Add(result1);
            result.Add(result2);

            // combine execution time
            result.ExecutionTime = result1.ExecutionTime + result2.ExecutionTime;

            return result;
        }

        #endregion

        #region Methods

        public IEnumerator<SqlRow> GetEnumerator() => rows?.GetEnumerator() ?? emptyRows.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => rows?.GetEnumerator() ?? emptyRows.GetEnumerator();

        internal void Add(SqlRow item)
        {
            if (rows != null)
                rows.Add(item);
        }

        public List<SqlRow> ToList()
        {
            if (Type == SqlExecuteType.Reader)
            {
                var list = new List<SqlRow>();

                foreach (var row in this)
                {
                    list.Add(row);
                }

                return list;
            }
            else
                return null;
        }

        public Type GetType(string column) => ColumnNames.Contains(column) ? ColumnTypes[ColumnNames.IndexOf(column)] : null;

        public int GetColumnIndex(string column) => ColumnIndexes.TryGetValue(column, out var index) ? index : -1;

        public bool ContainsColumn(string column) => ColumnIndexes.ContainsKey(column);

        private T Get<T>(SqlRow row, string column) => row[column] == null ? default : row[column].Get<T>();

        public List<T1> ToList<T1>(string c1)
        {
            if (c1 == null) throw new ArgumentNullException();

            var list = new List<T1>();

            foreach (var row in this)
            {
                list.Add(
                    Get<T1>(row, c1)
                    );
            }

            return list;
        }
        
        public List<(T1, T2)> ToList<T1, T2>(string c1, string c2)
        {
            if (Is.Null(c1, c2)) throw new ArgumentNullException();

            var list = new List<(T1, T2)>();

            foreach (var row in this)
            {
                list.Add((
                    Get<T1>(row, c1),
                    Get<T2>(row, c2)
                    ));
            }

            return list;
        }

        public List<(T1, T2, T3)> ToList<T1, T2, T3>(string c1, string c2, string c3)
        {
            if (Is.Null(c1, c2, c3)) throw new ArgumentNullException();

            var list = new List<(T1, T2, T3)>();

            foreach (var row in this)
            {
                list.Add((
                    Get<T1>(row, c1),
                    Get<T2>(row, c2),
                    Get<T3>(row, c3)
                    ));
            }

            return list;
        }

        public List<(T1, T2, T3, T4)> ToList<T1, T2, T3, T4>(string c1, string c2, string c3, string c4)
        {
            if (Is.Null(c1, c2, c3, c4)) throw new ArgumentNullException();

            var list = new List<(T1, T2, T3, T4)>();

            foreach (var row in this)
            {
                list.Add((
                    Get<T1>(row, c1),
                    Get<T2>(row, c2),
                    Get<T3>(row, c3),
                    Get<T4>(row, c4)
                    ));
            }

            return list;
        }

        public List<(T1, T2, T3, T4, T5)> ToList<T1, T2, T3, T4, T5>(string c1, string c2, string c3, string c4, string c5)
        {
            if (Is.Null(c1, c2, c3, c4, c5)) throw new ArgumentNullException();

            var list = new List<(T1, T2, T3, T4, T5)>();

            foreach (var row in this)
            {
                list.Add((
                    Get<T1>(row, c1),
                    Get<T2>(row, c2),
                    Get<T3>(row, c3),
                    Get<T4>(row, c4),
                    Get<T5>(row, c5)
                    ));
            }

            return list;
        }

        public Dictionary<T, SqlRow> ToDictionary<T>(Func<SqlRow, T> key, KeyDuplicateEventHandler<T> duplicate)
        {
            if (Type == SqlExecuteType.Reader)
            {
                if (key != null)
                {
                    var dictionary = new Dictionary<T, SqlRow>();

                    foreach (var row in this)
                    {
                        var dictionaryKey = key(row);

                        if (dictionaryKey != null)
                        {
                            if (!dictionary.ContainsKey(dictionaryKey))
                            {
                                dictionary.Add(dictionaryKey, row);
                            }
                            else
                            {  
                                duplicate?.Invoke(dictionaryKey, row);
                            }
                        }
                    }

                    return dictionary;
                }
                else
                    return null;
            }
            else
                return null;
        }

        public Dictionary<string, SqlRow> ToDictionary(string[] keys, KeyDuplicateEventHandler<string> duplicate)
        {
            if (Type == SqlExecuteType.Reader && ColumnNames != null && keys != null && ColumnNames.Contains(keys))
            {
                return ToDictionary(row =>
                {
                    // the dictkey will be composited from keys

                    var key = new StringBuilder();

                    foreach (var columnName in keys)
                    {
                        key.Append(row[columnName].GetString());
                    }

                    return key.ToString();

                }, duplicate);
            }
            else
                return null;
        }

        public Dictionary<T, SqlRow> ToDictionary<T>(string key, KeyDuplicateEventHandler<T> duplicate)
        {
            if (Type == SqlExecuteType.Reader && ColumnNames != null && key != null && ColumnNames.Contains(key))
                return ToDictionary(row => row[key].Get<T>(), duplicate);
            else
                return null;
        }

        public Dictionary<string, U> ToDictionary<U>(string[] keys, string value, KeyDuplicateEventHandler<string> duplicate)
        {
            var dict = ToDictionary(keys, duplicate);

            var ok = false;
            var ndict = new Dictionary<string, U>();

            foreach (var (dictkey, row) in dict)
            {
                if (ok == false)
                {
                    if (row.ContainsColumn(value)) ok = true;
                    else break;
                }
                ndict.Add(dictkey, row[value].Get<U>());
            }

            return ndict;
        }

        public Dictionary<T, U> ToDictionary<T, U>(string key, string value, KeyDuplicateEventHandler<T> duplicate)
        {
            var dict = ToDictionary(key, duplicate);

            var ok = false;
            var ndict = new Dictionary<T, U>();

            foreach (var (dictkey, row) in dict)
            {
                if (ok == false)
                {
                    if (row.ContainsColumn(value)) ok = true;
                    else break;
                }
                ndict.Add(dictkey, row[value].Get<U>());
            }

            return ndict;
        }

        public Dictionary<T, SqlRow> ToDictionary<T>(Func<SqlRow, T> key) => ToDictionary(key, null);

        public Dictionary<string, SqlRow> ToDictionary(string[] keys) => ToDictionary(keys, null);

        public Dictionary<T, SqlRow> ToDictionary<T>(string key) => ToDictionary<T>(key, null);

        public T[] To<T>(Func<SqlRow, T> create)
        {
            var list = new List<T>();

            foreach (var row in this) list.Add(create(row));

            return list.ToArray();
        }

        public string[] Print() => Print(100);

        public string[] Print(int limit)
        {
            if (limit < 1) return null;

            var maximumColumnWidth = 20;

            var print = new List<string> { $"Type: {Type}{(ExecuteOutput ? " (Originally as Execute)" : "")}, ExecutionTime: {ExecutionTime}" };

            if (limit < 2) return print.ToArray();

            if (Type == SqlExecuteType.Reader)
            {
                if (limit >= 7)
                {
                    if (Count > 0)
                    {
                        // set visible rows
                        int maximumIndex, totalColumn = 0;
                        if (Count > (limit - 6))
                            maximumIndex = limit - 6;
                        else
                            maximumIndex = Count;

                        var columnWidths = new List<int>();

                        // check column width, for column name, using first row
                        foreach (var key in ColumnNames)
                        {
                            var length = key.Length;
                            columnWidths.Add(length < 4 ? 4 : length > maximumColumnWidth ? maximumColumnWidth : length);
                            totalColumn++;
                        }

                        // check column width, all visible rows
                        for (int index = 0; index < maximumIndex; index++)
                        {
                            var row = this[index];

                            var columnIndex = 0;

                            foreach (var cell in row)
                            {
                                var currentWidth = columnWidths[columnIndex];

                                if (currentWidth < maximumColumnWidth)
                                {
                                    if (!cell.IsNull)
                                    {
                                        var length = cell.ToString().Length;

                                        if (currentWidth < length)
                                        {
                                            if (length < maximumColumnWidth)
                                                columnWidths[columnIndex] = length;
                                            else
                                                columnWidths[columnIndex] = maximumColumnWidth;
                                        }
                                    }
                                }

                                columnIndex++;
                            }
                        }

                        // print column
                        print.Add($"TotalRows: {Count}{(maximumIndex < Count ? $" (Showing first {(maximumIndex > 1 ? $"{maximumIndex} rows" : "row")})": "")}, TotalColumns: {totalColumn}");

                        print.Add(PrintHorizontalBorder(columnWidths));

                        print.Add(PrintContents(columnWidths, ColumnNames));

                        print.Add(PrintHorizontalBorder(columnWidths));

                        for (int index = 0; index < maximumIndex; index++)
                        {
                            var contents = new List<string>();

                            foreach (var cell in this[index])
                            {
                                contents.Add(cell.Print()[0]);
                            }

                            print.Add(PrintContents(columnWidths, contents.ToArray()));
                        }

                        if (maximumIndex < Count)
                        {                            
                            print.Add(PrintContents(columnWidths, Collections.Create("...", totalColumn)));
                        }
                        else
                            print.Add(PrintHorizontalBorder(columnWidths));
                        /* 
                         *  +--------+---------+---------------+
                         *  | Column | Column  | Column        |
                         *  +--------+---------+---------------+
                         *  | Data   | ...     | ...           |
                         *  |        |         |               |
                         *  +--------+---------+---------------+
                         */

                    }
                    else
                    {
                        print.Add($"TotalRows: 0");
                    }
                }
            }
            else if (Type == SqlExecuteType.Scalar)
            {
                var cell = First.First;
                var length = cell.ToString().Length;

                var widths = new List<int> { length < maximumColumnWidth ? length : maximumColumnWidth };

                print.Add(PrintHorizontalBorder(widths));
                print.Add(PrintContents(widths, new string[] { $"{First.First}" }));
                print.Add(PrintHorizontalBorder(widths));
            } 
            else
            {
                print.Add($"AffectedRows: {Count}");
            }

            return print.ToArray();
        }

        private string PrintHorizontalBorder(List<int> columnWidths)
        {
            var sb = new StringBuilder();

            sb.Append('+');

            foreach (var width in columnWidths)
            {
                sb.Append('-');

                for (int count = 0; count < width; count++)
                {
                    sb.Append('-');
                }

                sb.Append("-+");
            }

            return sb.ToString();
        }

        private string PrintContents(List<int> columnWidths, string[] contents)
        {
            var sb = new StringBuilder();

            sb.Append('|');

            int index = 0;

            foreach (var width in columnWidths)
            {
                sb.Append(' ');

                var content = contents[index];

                if (content == null)
                    content = "NULL";

                var contentWidth = content.Length;

                if (contentWidth > width)
                {
                    content = $"{content.Substring(0, width - 3)}...";
                    contentWidth = width;
                }

                sb.Append(content);

                for (int left = 0; left < (width - contentWidth); left++)
                {
                    sb.Append(' ');
                }

                sb.Append(" |");

                index++;
            }

            return sb.ToString();
        }

        #endregion
    }

}

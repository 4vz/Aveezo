using System;
using System.Collections.Generic;

namespace Aveezo;

public sealed class SqlDataCollection<T> where T : SqlData, new()
{
    #region Fields

    private Sql Database { get; }

    private readonly Dictionary<Guid, T> entries = new Dictionary<Guid, T>();

    public SqlCondition Where { get; internal set; } = null;

    public T this[Guid key] { get => entries[key]; set { } }

    public T this[string key] { get => entries[key.ToGuid()]; set { } }

    public T this[int index] { get => index < entries.Count ? entries.Get(index).Item2 : null; set { } }

    public int Count => entries.Count;

    public bool NotQueryResult { get; } = true;

    #endregion

    #region Constructors

    internal SqlDataCollection(Sql sql)
    {
        Database = sql;
    }

    internal SqlDataCollection(Sql sql, bool notQueryResult)
    {
        Database = sql;
        NotQueryResult = notQueryResult;
    }

    #endregion

    #region Operators

    public static implicit operator T(SqlDataCollection<T> collection) => collection.Count > 0 ? collection[0] : null;

    #endregion

    #region Methods

    public IEnumerator<T> GetEnumerator() => entries.Values.GetEnumerator();

    internal void _Add(T value) => entries.Add(value.Id, value);

    /// <summary>
    /// Iterates this collection and return true
    /// </summary>
    /// <param name="func"></param>
    /// <returns></returns>
    public T Find(Func<T, bool> func)
    {
        foreach (var (guid, e) in entries)
        {
            var f = func(e);
            if (f) return e;
        }

        return default;
    }

    public bool ContainsKey(string key) => entries.ContainsKey(key.ToGuid());

    public bool ContainsKey(Guid key) => entries.ContainsKey(key);

    /// <summary>
    /// Mark for deletion for the entry by specified Id in this collection.
    /// </summary>
    public void Delete(string key) => Delete(key.ToGuid());

    /// <summary>
    /// Mark for deletion for the entry by specified Id in this collection.
    /// </summary>
    public void Delete(Guid key) => entries[key].Delete = true;

    /// <summary>
    /// Unmark for deletion for the entry by specified Id in this collection.
    /// </summary>
    public void Undelete(string key) => Undelete(key.ToGuid());

    /// <summary>
    /// Unmark for deletion for the entry by specified Id in this collection.
    /// </summary>
    public void Undelete(Guid key) => entries[key].Delete = false;

    /// <summary>
    /// Mark for deletion for all entries in this collection.
    /// </summary>
    public void DeleteAll()
    {
        foreach (var (_, entry) in entries)
        {
            entry.Delete = true;
        }
    }

    /// <summary>
    /// Unmark for deletion for all entries in this collection.
    /// </summary>
    public void UndeleteAll()
    {
        foreach (var (_, entry) in entries)
        {
            entry.Delete = false;
        }
    }

    #region Changing collection

    /// <summary>
    /// Add specified entry to this collection.
    /// </summary>
    public void Add(T value) => entries.Add(value.Id, value);

    /// <summary>
    /// Remove specified entry to this collection.
    /// </summary>
    public bool Remove(Guid key) => entries.Remove(key);

    #endregion

    /// <summary>
    /// Sync the entries
    /// </summary>
    public SqlQuery Sync()
    {
        var info = SqlData.GetInfo(typeof(T));

        Dictionary<Guid, SqlRow> latestData;

        if (!NotQueryResult)
            latestData = Database.SelectToDictionary<Guid>(info.Table, Where);
        else
        {
            latestData = new Dictionary<Guid, SqlRow>();

            List<Guid> currentList = new List<Guid>();
            foreach (var (pid, entry) in entries)
            {
                if (!entry.New && !entry.Delete)
                    currentList.Add(pid);

                if (currentList.Count == 50) // 50 guid every query
                {
                    foreach (var (guid, row) in Database.SelectToDictionary<Guid>(info.Table, (SqlColumn)info.IdName == currentList)) latestData.Add(guid, row);
                    currentList.Clear();
                }
            }
            if (currentList.Count > 0)
                foreach (var (guid, row) in Database.SelectToDictionary<Guid>(info.Table, (SqlColumn)info.IdName == currentList)) latestData.Add(guid, row);
        }

        var insert = Database.Insert(info.Table, info.Columns);
        var update = Database.Update(info.Table, info.IdName);
        var delete = Database.Delete(info.Table, info.IdName);

        var deletedData = new List<Guid>();

        foreach (var item in this)
        {
            var guid = item.Id;

            item.Sync(info, latestData.ContainsKey(guid) ? latestData[guid] : null, insert, update, delete);

            if (item.IsDeleted)
            {
                deletedData.Add(guid);
            }
        }

        var insertResults = insert.Execute();
        var updateResults = update.Execute();
        var deleteResults = delete.Execute();

        foreach (var guid in deletedData) Remove(guid);

        return insertResults + updateResults + deleteResults;
    }

    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

[Flags]
public enum SqlColumnOptions : short
{
    None = 0,
    ThrowExceptionWhenViolated = 1,
    NotNull = 2,
}

public class SqlDataFieldInfo
{
    #region Fields

    public PropertyInfo PropertyInfo { get; }

    public string Column { get; }

    public SqlColumnOptions Options { get; }

    public Dictionary<object, object> Enums { get; }

    #endregion

    #region Constructors

    public SqlDataFieldInfo(PropertyInfo propertyInfo, string column, SqlColumnOptions options, Dictionary<object, object> enums)
    {
        PropertyInfo = propertyInfo;
        Column = column;
        Options = options;
        Enums = enums;
    }

    #endregion

    #region Operators


    #endregion

    #region Methods

    #endregion

    #region Statics

    #endregion
}

public class SqlDataInfo
{
    #region Fields

    public string Table { get; }

    public string IdName { get; }

    public string[] Columns { get; }

    public SqlDataFieldInfo[] Fields { get; }

    #endregion

    #region Constructors

    public SqlDataInfo(string table, string idName, string[] columns, SqlDataFieldInfo[] fields)
    {
        Table = table;
        IdName = idName;
        Columns = columns;
        Fields = fields;
    }

    #endregion
}

public abstract class SqlBucketData
{
    #region Fields

    public Guid Id { get; internal init; }

    internal readonly Dictionary<string, object> Reference = new Dictionary<string, object>();

    public bool Delete { get; set; } = false;

    internal bool New = true;

    public bool IsDeleted { get; private set; } = false;

    #endregion

    #region Constructors

    public SqlBucketData()
    {
        Id = Guid.NewGuid();
    }

    #endregion

    #region Methods

    internal object GetValue(SqlDataFieldInfo fieldInfo)
    {
        object currentValue = null;

        if (fieldInfo.Enums != null)
        {
            var setto = fieldInfo.PropertyInfo.GetValue(this);

            foreach (var (oname, ovalue) in fieldInfo.Enums)
            {
                if (oname.Equals(setto))
                {
                    currentValue = ovalue;
                    break;
                }
            }
        }
        else
        {
            currentValue = fieldInfo.PropertyInfo.GetValue(this);

            if (fieldInfo.Options.HasFlag(SqlColumnOptions.NotNull) && currentValue is null)
            {
                // pelanggaran berat melanggar pasal 378
                currentValue = Sql.Default(fieldInfo.PropertyInfo.PropertyType);
                fieldInfo.PropertyInfo.SetValue(this, currentValue);
            }
        }

        return currentValue;
    }

    internal void SetValue(SqlDataFieldInfo fieldInfo, object value)
    {
        if (fieldInfo.Enums != null)
        {
            // set reference value
            if (!Reference.ContainsKey(fieldInfo.Column))
                Reference.Add(fieldInfo.Column, value);
            else
                Reference[fieldInfo.Column] = value;

            // convert data to enu
            //var enumType = property.PropertyType;
            foreach (var (oname, ovalue) in fieldInfo.Enums)
            {
                if (ovalue.Equals(value))
                {
                    fieldInfo.PropertyInfo.SetValue(this, oname);
                    break;
                }
            }
        }
        else
        {
            if (fieldInfo.Options.HasFlag(SqlColumnOptions.NotNull) && value == null)
                value = Sql.Default(fieldInfo.PropertyInfo.PropertyType);

            // set reference value
            if (!Reference.ContainsKey(fieldInfo.Column))
                Reference.Add(fieldInfo.Column, value);
            else
                Reference[fieldInfo.Column] = value;

            fieldInfo.PropertyInfo.SetValue(this, value); // set actual value
        }
    }

    internal void Sync(SqlDataInfo info, SqlRow data, SqlInsertTable insert, SqlUpdateTable update, SqlDeleteTable delete)
    {
        var item = this;
        var guid = item.Id;

        if (item.New)
        {
            var values = new List<object>() { guid };

            foreach (var fieldInfo in info.Fields)
            {
                var objValue = item.GetValue(fieldInfo);
                item.Reference.Add(fieldInfo.Column, objValue);
                values.Add(objValue);
            }

            item.New = false;
            insert.Values(values.ToArray());
        }
        else if (item.Delete)
        {
            delete.Where(guid);
            item.Delete = false;

            IsDeleted = true;
        }
        else if (data != null)
        {
            foreach (var fieldInfo in info.Fields)
            {
                var column = fieldInfo.Column;

                var latestValue = data[column].GetObject();
                var objReferenceValue = item.Reference[column];
                var objValue = item.GetValue(fieldInfo);

                if (Equals(latestValue, objReferenceValue))
                {
                    if (!Equals(objReferenceValue, objValue))
                    {
                        // objValue is latest & greatest
                        item.Reference[column] = objValue;

                        // update this column
                        var row = update.Where(guid);
                        row.Set(column, objValue);
                    }
                }
                else
                {
                    // latestValue is latest & greatest
                    item.SetValue(fieldInfo, latestValue);
                }
            }
        }
        else
        {
            IsDeleted = true;
        }
    }

    public SqlQuery Sync(Sql sql)
    {
        var info = GetInfo(GetType());

        var Database = sql;
        var item = this;
        var guid = item.Id;

        var insert = Database.Insert(info.Table, info.Columns);
        var update = Database.Update(info.Table, info.IdName);
        var delete = Database.Delete(info.Table, info.IdName);

        Sync(info, sql.SelectToRow(info.Table, info.IdName, guid), insert, update, delete);

        var insertResults = insert.Execute();
        var updateResults = update.Execute();
        var deleteResults = delete.Execute();

        return insertResults + updateResults + deleteResults;
    }

    #endregion

    #region Statics

    private static Dictionary<Type, SqlDataInfo> sqlDataInfo = new Dictionary<Type, SqlDataInfo>();

    internal static SqlDataInfo GetInfo(Type t)
    {
        SqlDataInfo info = null;

        if (sqlDataInfo.ContainsKey(t)) info = sqlDataInfo[t];
        else
        {
            string table = null;
            string idName = null;
            string[] columns = null;
            SqlDataFieldInfo[] fields = null;

            foreach (var classAttribute in t.GetCustomAttributes(false))
            {
                if (classAttribute is SqlTableAttribute tableAttribute)
                {
                    table = tableAttribute.Name;
                    idName = tableAttribute.IdName;

                    var columnList = new List<string>() { idName };
                    var fieldList = new List<SqlDataFieldInfo>();

                    foreach (var property in t.GetProperties())
                    {
                        string column = null;
                        SqlColumnOptions options = 0;

                        foreach (var propertyAttribute in property.GetCustomAttributes(true))
                        {
                            if (propertyAttribute is SqlColumnAttribute columnAttribute)
                            {
                                column = columnAttribute.Name;
                                options = columnAttribute.Options;
                                columnList.Add(column);

                                break;
                            }
                        }

                        if (column != null)
                        {
                            Dictionary<object, object> enums = null;

                            if (property.PropertyType.IsEnum)
                            {
                                enums = new Dictionary<object, object>();

                                foreach (var value in Enum.GetValues(property.PropertyType))
                                {
                                    object dbValue = null;

                                    foreach (var enumAttribute in property.PropertyType.GetField(Enum.GetName(property.PropertyType, value)).GetCustomAttributes(false))
                                    {
                                        if (enumAttribute is SqlValueAttribute valueAttribute)
                                        {
                                            dbValue = valueAttribute.Value;
                                            break;
                                        }
                                    }

                                    if (dbValue != null) enums.Add(value, dbValue);
                                }
                            }

                            fieldList.Add(new SqlDataFieldInfo(property, column, options, enums));
                        }
                    }

                    columns = columnList.ToArray();
                    fields = fieldList.ToArray();

                    break;
                }
            }

            if (table != null)
            {
                info = new SqlDataInfo(table, idName, columns, fields);
                sqlDataInfo.Add(t, info);
            }
        }

        return info;
    }

    #endregion
}

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class SqlColumnAttribute : Attribute
{
    public string Name { get; }

    public SqlColumnOptions Options { get; } = 0;

    public SqlColumnAttribute(string name, SqlColumnOptions options)
    {
        Name = name;
        Options = options;
    }

    public SqlColumnAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SqlTableAttribute : Attribute
{
    public string Name { get; }

    public string IdName { get; }

    public SqlTableAttribute(string name, string idName)
    {
        Name = name;
        IdName = idName;
    }
}

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class SqlValueAttribute : Attribute
{
    #region Fields

    public object Value { get; }

    #endregion

    #region Constructors

    public SqlValueAttribute(object value)
    {
        Value = value;
    }

    #endregion
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Aveezo;

/// <summary>
/// All API must be derived from this class.
/// </summary>
[ApiController]
public abstract class Api : ControllerBase
{
    #region Fields

    /// <summary>
    /// The IServiceProvider instance.
    /// </summary>
    protected IServiceProvider Provider { get; }

    /// <summary>
    /// The ApiOptions instance.
    /// </summary>
    protected ApiOptions Options { get; }

    /// <summary>
    /// ApiParameters object.
    /// </summary>
    public ApiParameters Parameters { get; set; } = null;

    /// <summary>
    /// Sql object from SqlAttribute.
    /// </summary>
    public Sql Sql { get; set; } = null;

    /// <summary>
    /// Select object from ResourceAttribute.
    /// </summary>
    public ApiSelect Select { get; set; } = null;

    public static object Null { get; } = new DataObject("NULL");

    public static object NotNull { get; } = new DataObject("NOTNULL");

    public static object Cancel { get; } = new DataObject("CANCEL");

    #endregion

    #region Constructors

    public Api(IServiceProvider provider)
    {
        Provider = provider;
        Options = Provider.GetService<IOptions<ApiOptions>>().Value;
    }

    #endregion

    #region Methods

    protected bool Load(string name, out Sql sql)
    {
        sql = Service<IDatabaseService>().Sql(name);

        if (sql) return true;
        else return false;
    }

    protected T Service<T>() => Provider.GetService<T>();

    protected void Debug(string header, string data)
    {
#if DEBUG
        HttpContext.Response.Headers.Add($"x-aveezo-debug-{header}", data?.Replace("\r\n", " "));
#endif
    }

    protected Method<T[]> Query<T>(SqlSelect select, Action<ApiQueryOptions<T>> options, Func<SqlRow, T> create)
    {
        if (select == null)
            return NotFound();

        var opt = new ApiQueryOptions<T>();
        options?.Invoke(opt);

        Values<string> selectBuilders = null;

        if (create != null)
        {

        }
        else
        {
            selectBuilders = Parameters.Fields;

            if (Parameters.Queries != null && Parameters.Queries.Length > 0)
            {
                foreach (var (name, queries) in Parameters.Queries)
                {
                    var builderStack = select.GetBuilderStack(name);

                    if (builderStack != null)
                    {
                        // set up column and modifiers

                        SqlColumn builderColumn = null;
                        SqlColumn column = null;
                        Dictionary<string, Func<object, object>> modifiers = new();

                        foreach (var (stackName, builder) in builderStack)
                        {
                            if (builderColumn is null)
                                builderColumn = builder.Column;

                            if (builder.Query != null)
                            {
                                modifiers.Add(stackName, builder.Query);
                            }
                        }

                        if (modifiers.Count == 0)
                            modifiers.Add("___default", null);

                        if (select.InnerSelect == null)
                            column = builderColumn;
                        else
                            column = select.Table[name];

                        // iterate through queries

                        foreach (var (attribute, queryValue) in queries)
                        {
                            SqlCondition sumCondition = null;
                            object value;


                            foreach (var (modifierName, modifier) in modifiers)
                            {
                                if (modifier != null)
                                    value = modifier(queryValue);
                                else
                                    value = queryValue;

                                if (value is not null)
                                {
                                    SqlCondition newCondition = null;

                                    if (value is DataObject obj)
                                    {
                                        if (obj.Data == "NULL")
                                            newCondition = column == null;
                                        else if (obj.Data == "NOTNULL")
                                            newCondition = column != null;
                                        else if (obj.Data == "CANCEL")
                                            newCondition = new SqlCondition(false);
                                    }
                                    else
                                    {
                                        var isNumeric = value.IsNumeric();

                                        if (attribute == null)
                                            newCondition = column == value;
                                        else if (attribute == "like")
                                            newCondition = column % $"%{value}%";
                                        else if (attribute == "start")
                                            newCondition = column % $"{value}%";
                                        else if (attribute == "end")
                                            newCondition = column % $"%{value}";
                                        else if (attribute == "notlike")
                                            newCondition = column ^ $"%{value}%";
                                        else if (attribute == "not")
                                            newCondition = column != value;
                                        else if (isNumeric && attribute == "lt")
                                            newCondition = column < value;
                                        else if (isNumeric && attribute == "gt")
                                            newCondition = column > value;
                                        else if (isNumeric && attribute == "lte")
                                            newCondition = column <= value;
                                        else if (isNumeric && attribute == "gte")
                                            newCondition = column >= value;
                                        else
                                            newCondition = column == value;
                                    }

                                    if (modifierName != "___default")
                                        newCondition = newCondition && select.Table["___select"] == modifierName;

                                    if (sumCondition is null)
                                        sumCondition = newCondition;
                                    else
                                        sumCondition = sumCondition || newCondition;
                                }
                            }

                            select.WhereCondition = sumCondition && select.WhereCondition;

                        }



                    }
                }
            }

            if (Parameters.Sorts != null && Parameters.Sorts.Length > 0)
            {
                foreach (var (name, positive) in Parameters.Sorts)
                {
                    var builderStack = select.GetBuilderStack(name);

                    if (builderStack != null)
                    {
                        var (_, builder) = builderStack.Get(0);

                        var order = positive ? Order.Descending : Order.Ascending;
                        select.Order.Add(builder.Column, order);
                    }
                }
            }
            else
            {
                var builderStack = select.GetBuilderStackId();

                if (builderStack != null)
                {
                    var (_, builder) = builderStack.Get(0);
                    var column = builder.Column;
                    select.Order = SqlOrder.By(column.Table == select.Table ? column : select.Table[builder.Name], Order.Ascending);
                }
            }
        }

        // pagination
        if (Parameters.IsPaging)
        {
            if (Parameters.Limit > 0 && Parameters.Limit <= opt.MaximumLimit)
                select.LimitLength = Parameters.Limit;
            else if (Parameters.Limit < 1 && select.LimitLength == 0)
                select.LimitLength = opt.DefaultLimit;

            if (Parameters.Offset > -1)
                select.OffsetLength = Parameters.Offset;
            else if (Parameters.Offset < 0 && select.OffsetLength < 0)
                select.OffsetLength = 0;
        }
        else
        {
            select.LimitLength = 1;
            select.OffsetLength = 0;
        }

        var query = select.Execute(selectBuilders);

        if (query.Ok)
        {
            if (query.NoResult)
                return NotFound();
            else
            {
                SqlResult result = query;

                Debug("sql", result.Sql);
                Debug("sql-execution-time", $"{result.ExecutionTime.TotalMilliseconds} ms");

                Dev.Watch(out var sw);

                T[] items = null;

                if (create != null)
                {
                    items = result.To(create);
                }
                else
                {
                    items = query.Builder<T>(
                        itemBuilder =>
                        {
                            var item = itemBuilder.Item;
                            var context = itemBuilder.Context;
                            var select = itemBuilder.Select;
                            var row = itemBuilder.Row;
                            var links = context["links"] as Dictionary<string, ResourceLink>;

                            if (item is Resource resource)
                            {
                                if (links != null && links.Count > 0)
                                    resource._Links = links;
                            }
                        },
                        propertyBuilder =>
                        {
                            var item = propertyBuilder.Item;
                            var context = propertyBuilder.Context;
                            var name = propertyBuilder.Builder.Name;
                            var options = propertyBuilder.Builder.Options;
                            var cell = propertyBuilder.Cell;
                            var ext = propertyBuilder.Builder.Ext;
                            var links = context["links"] as Dictionary<string, ResourceLink>;

                            if (links == null)
                            {
                                links = new Dictionary<string, ResourceLink>();
                                context["links"] = links;
                            }

                            if (options == SqlBuilderOptions.Id)
                            {
                                name = "self";
                                if (item is Resource resource && resource.Id == null)
                                {
                                    resource.Id = Encode(cell);
                                }
                            }
                            if (ext?.Invoke(item) is string href)
                            {
                                links.Add(name, new ResourceLink { Href = href });
                            }
                        });

                    Debug("result-loop", Dev.Watch(sw));
                }

                if (Parameters.IsPaging)
                {
                    return new MethodResult<T[]>
                    {
                        Result = items,
                        Total = select.ExecuteCount(selectBuilders),
                        Count = items.Length,
                        Offset = select.OffsetLength,
                        Fields = Parameters.Fields
                    };
                }
                else
                    return items;
            }
        }
        else
        {
            Debug("sql", query.Exception.Sql);
            return Unavailable($"Query failed: {query.Exception.Exception?.Message}");
        }
    
    }

    protected Method<T[]> Query<T>(SqlSelect select, Func<SqlRow, T> create) => Query(select, null, create);

    protected Method<T[]> Query<T>(SqlSelect select) => Query<T>(select, null, null);

    protected Method<T[]> Query<T>(Action<SqlSelect> init, Action<ApiQueryOptions<T>> options, params object[] parameters)
    {
        if (Select == null)
            return Unavailable("Resource select is not initialized");

        var sqlSelect = Select.SqlSelect;

        if (sqlSelect != null)
        {
            var select = sqlSelect(parameters);

            init?.Invoke(select);

            return Query<T>(select, options, null);
        }
        else
            return Unavailable("Resource select is not initialized");
    }

    protected Method<T[]> Query<T>(Action<SqlSelect> init, params object[] parameters) => Query<T>(init, null, parameters);

    protected Method<T[]> Query<T>(Action<ApiQueryOptions<T>> options, params object[] parameters) => Query<T>(null, options, parameters);

    protected Method<T[]> Query<T>(params object[] parameters) => Query<T>(null, null, parameters);

    #endregion

    #region Statics

    public static string Encode(SqlCell cell)
    {
        var type = cell.Type;
        string r = null;

        if (!cell.IsNull)
        {
            if (type == typeof(short))
                r = Base64.UrlEncode(BitConverter.GetBytes(cell.GetShort()));
            else if (type == typeof(ushort))
                r = Base64.UrlEncode(BitConverter.GetBytes(cell.GetUshort()));
            else if (type == typeof(int))
                r = Base64.UrlEncode(BitConverter.GetBytes(cell.GetInt()));
            else if (type == typeof(uint))
                r = Base64.UrlEncode(BitConverter.GetBytes(cell.GetUint()));
            else if (type == typeof(long))
                r = Base64.UrlEncode(BitConverter.GetBytes(cell.GetLong()));
            else if (type == typeof(ulong))
                r = Base64.UrlEncode(BitConverter.GetBytes(cell.GetUlong()));
            else if (type == typeof(string))
                r = Base64.UrlEncode(cell.GetString().TrimEnd());
            else if (type == typeof(Guid))
                r = Base64.UrlEncode(cell.GetGuid());
        }

        return r;
    }

    public static string Decode(string id) => Base64.UrlDecode(id);

    public static ObjectResult Unavailable(string message)
    {
#if DEBUG
        var stackTrace = new StackTrace();
        var frame = stackTrace.GetFrame(1);
        var imethod = frame.GetMethod();
        var itype = imethod.DeclaringType;

        var res = new ErrorResult(503, $"{itype.Name}.{imethod.Name}", "UNAVAILABLE", message);
#else
            object res = null;
#endif
        var o = new ObjectResult(res) { StatusCode = 503 };
        return o;
    }

    public static ObjectResult NotFound(string source, string status, string message) => new ObjectResult(new ErrorResult(404, source, status, message)) { StatusCode = 404 };

    public static ObjectResult Forbidden(string source, string status, string message) => new ObjectResult(new ErrorResult(403, source, status, message)) { StatusCode = 403 };

    public static ObjectResult BadRequest(string source, string status, string message) => new ObjectResult(new ErrorResult(400, source, status, message)) { StatusCode = 400 };

    #endregion
}

public class ApiQueryOptions<T>
{
    #region Fields

    public int DefaultLimit { get; set; } = 30;

    public int MaximumLimit { get; set; } = 100;

    public SqlColumn ColumnId { get; set; } = null;

    #endregion
}


[ModelBinder(typeof(ApiSelectBinder))]
public sealed class ApiSelect
{
    public Func<object[], SqlSelect> SqlSelect { get; set; } = null;
}


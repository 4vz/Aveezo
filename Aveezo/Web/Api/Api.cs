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
    /// publicsummary>
    public ApiOptions Options { get; }

    /// <summary>
    /// ApiParameters object.
    /// </summary>
    public ApiParameters Parameters { get; set; } = null;

    /// <summary>
    /// Select object from ResourceAttribute.
    /// </summary>
    public ApiSelect Select { get; set; } = null;

    /// <summary>
    /// Sql object from SqlAttribute.
    /// </summary>
    public Sql Sql { get; set; } = null;

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

    protected Result<T[]> Query<T>(SqlSelect select, Action<ApiQueryOptions<T>> options, Func<SqlRow, T> create)
    {
        if (select == null)
            return NotFound();

        var opt = new ApiQueryOptions<T>();
        options?.Invoke(opt);

        List<string> displayFields = null;
        Values<string> queryFields = null;

        if (create != null)
        {

        }
        else
        {
            var idBuilderStack = select.GetBuilderStack(Sql.Id);

            if (idBuilderStack == null)
            {
                return Unavailable($"Builder with the identifier key ({Sql.Id}) is not found in the current builders. Please review the select function for this method.");
            }

            displayFields = new();

            if (Parameters.Queries != null && Parameters.Queries.Length > 0)
            {
                Dictionary<string, (SqlQueryType, Values<string>)[]> queries = new();

                foreach (var (a, b) in Parameters.Queries)
                {
                    List<(SqlQueryType, Values<string>)> states = new();

                    foreach (var (c, d) in b)
                    {
                        var type = c switch
                        {
                            "like" => SqlQueryType.Like,
                            "notlike" => SqlQueryType.NotLike,
                            "not" => SqlQueryType.NotEqual,
                            "start" => SqlQueryType.StartsWith,
                            "end" => SqlQueryType.EndsWith,
                            "lt" =>  SqlQueryType.LessThan,
                            "lte" => SqlQueryType.LessThanOrEqual,
                            "gt" => SqlQueryType.GreaterThan,
                            "gte" => SqlQueryType.GreaterThanOrEqual,
                            _ => SqlQueryType.Equal,
                        };

                        states.Add((type, d));
                    }

                    queries.Add(a, states.ToArray());
                }

                select.BuilderQuery(queries, out queryFields);
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
                if (idBuilderStack != null)
                {
                    var (_, builder) = idBuilderStack.Get(0);
                    var column = builder.Column;
                    select.Order = SqlOrder.By(column.Table == select.Table ? column : select.Table[builder.Name], Order.Ascending);
                }
            }

            foreach (var (field, fieldOptions) in Parameters.FieldOptions)
            {
                if (!fieldOptions.HasFlag(FieldOptions.HideInFields) && Parameters.Fields.Contains(field))
                    displayFields.Add(field);
            }
        }

        // pagination
        if (Parameters.IsPaging)
        {
            if (select.LimitLength == 0) 
            {
                if (Parameters.Limit > 0 && Parameters.Limit > opt.MaximumLimit)
                    select.LimitLength = opt.MaximumLimit;
                else if (Parameters.Limit < 0)
                    select.LimitLength = 1;
                else if (Parameters.Limit == 0)
                    select.LimitLength = opt.DefaultLimit;
                else
                    select.LimitLength = Parameters.Limit;
            }
            else
            {
                if (select.LimitLength > opt.MaximumLimit)
                    select.LimitLength = opt.MaximumLimit;
            }

            if (Parameters.Offset > -1)
                select.OffsetLength = Parameters.Offset;
            else
                select.OffsetLength = 0;
        }
        else
        {
            select.LimitLength = 1;
            select.OffsetLength = 0;
        }

        Dev.Watch(out var sw);

        var queryMain = select.Execute(Parameters.Fields + queryFields);

        if (queryMain.Ok)
        {
            SqlResult resultMain = queryMain;

            Debug("sql-main", resultMain.Sql);

            if (queryMain.NoResult)
                return NotFound();
            else
            {
                Debug("select-main-execute-net", Dev.Watch(sw, resultMain.ExecutionTime.TotalMilliseconds));

                T[] items = null;

                if (create != null)
                {
                    items = resultMain.To(create);
                }
                else
                {
                    items = queryMain.Builder<T>(
                        property =>
                        {
                            var name = property.Builder.Name;

                            if (property.Context["links"] is not Dictionary<string, ResourceLink> links)
                            {
                                links = new Dictionary<string, ResourceLink>();
                                property.Context["links"] = links;
                            }


                            if (property.Builder.Binder == null)
                            {
                                if (Parameters.Properties != null && Parameters.Properties.ContainsKey(name))
                                {
                                    var propertyInfo = Parameters.Properties[name];
                                    var propertyType = propertyInfo.PropertyType;

                                    // convert value to property type
                                    var value = property.FormattedValue;
                                    if (value != null && value.GetType() != propertyType)
                                    {
                                        if (value.TryCast(propertyType, out object cast))
                                            propertyInfo.SetValue(property.Item, cast);
                                    }
                                    else
                                        propertyInfo.SetValue(property.Item, value);

                                    if (property.Ext is string href)
                                    {
                                        links.Add(name == Sql.Id ? "self" : name, new ResourceLink { Href = href });
                                    }
                                }
                            }
                            else
                            {
                                if (property.Ext is string href)
                                {
                                    links.Add(name == Sql.Id ? "self" : name, new ResourceLink { Href = href });
                                }
                            }
                        },
                        item =>
                        {
                            if (item.Item is Resource resource)
                            {
                                if (item.Context["links"] is Dictionary<string, ResourceLink> links && links.Count > 0)
                                    resource.Links = links;
                            }
                        }, 
                        formatter =>
                        {
                            var name = formatter.Builder.Name;
                            object value = null;

                            if (Parameters.FieldOptions.ContainsKey(name) && Parameters.FieldOptions[name].HasFlag(FieldOptions.Encoded) && formatter.FormattedValue == null)
                                value = Encode(formatter.Value);
                            else
                                value = formatter.FormattedValue;

                            return value;
                        }
                    );
                }

                Debug("result-loop", Dev.Watch(sw));

                Result<T[]> result = null;

                Debug("sql-main-execution-time", $"{queryMain.ExecutionTime.TotalMilliseconds} ms");

                if (Parameters.IsPaging)
                {
                    int? total = null;

                    if (Parameters.Total)
                    {
                        total = select.ExecuteCount(Parameters.Fields, out var queryCount);
                        Debug("sql-count-execution-time", $"{queryCount.ExecutionTime.TotalMilliseconds} ms");
                        Debug("sql-count", ((SqlResult)queryCount).Sql);
                    }

                    result = new PagingResult<T[]>
                    {
                        Result = items,
                        Total = total,
                        Count = items.Length,
                        Offset = select.OffsetLength,
                        Fields = displayFields?.ToArray()
                    };
                }
                else
                    result = items;

                

                return result;
            }
        }
        else
        {
            Debug("sql", queryMain.Exception.Sql);
            return Unavailable($"Query failed: {queryMain.Exception.Exception?.Message}");
        }
    
    }

    protected Result<T[]> Query<T>(SqlSelect select, Func<SqlRow, T> create) => Query(select, null, create);

    protected Result<T[]> Query<T>(SqlSelect select) => Query<T>(select, null, null);

    protected Result<T[]> Query<T>(Action<SqlSelect> init, Action<ApiQueryOptions<T>> options, params object[] parameters)
    {
        if (Select == null)
            return Unavailable("Resource select is not initialized");

        var sqlSelect = Select.SqlSelect;

        if (sqlSelect != null)
        {
            var select = sqlSelect(parameters);

            init?.Invoke(select);

            return Query(select, options, null);
        }
        else
            return Unavailable("Resource select is not initialized");
    }

    protected Result<T[]> Query<T>(Action<SqlSelect> init, params object[] parameters) => Query<T>(init, null, parameters);

    protected Result<T[]> Query<T>(Action<ApiQueryOptions<T>> options, params object[] parameters) => Query<T>(null, options, parameters);

    protected Result<T[]> Query<T>(params object[] parameters) => Query<T>(null, null, parameters);

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
    public Func<Parameters, SqlSelect> SqlSelect { get; set; } = null;
}


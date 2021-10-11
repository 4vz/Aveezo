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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Aveezo
{
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
        /// PagingParameters object
        /// </summary>
        public PagingParameters PagingParameters { get; set; } = null;

        /// <summary>
        /// ActionParameters object
        /// </summary>
        public ActionParameters ActionParameters { get; set; } = null;

        /// <summary>
        /// Sql object
        /// </summary>
        public Sql Sql { get; set; } = null;

        protected object Null { get; } = new DataObject("NULL");

        protected object NotNull { get; } = new DataObject("NOTNULL");

        protected object Discard { get; } = new DataObject("DISCARD");

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

        protected StatusCodeResult Unavailable() => StatusCode(503);

        protected ObjectResult Unavailable(object value) => StatusCode(503, value);

        protected ObjectResult NotFound(string source, string error) => StatusCode(404, new ErrorResult(source, error));

        protected ObjectResult Forbidden(string source, string error) => StatusCode(403, new ErrorResult(source, error));

        protected ObjectResult BadRequest(string source, string error) => StatusCode(400, new ErrorResult(source, error));

        public Result<T[]> Query<T>(SqlSelect select, int defaultLimit, params Func<SqlRow, T>[] create) => Query(select, defaultLimit, defaultLimit, which => 0, create);

        public Result<T[]> Query<T>(SqlSelect select, int defaultLimit, int maximumLimit, params Func<SqlRow, T>[] create) => Query(select, defaultLimit, maximumLimit, which => 0, create);

        public Result<T[]> Query<T>(SqlSelect select, params Func<SqlRow, T>[] create) => Query(select, which => 0, create);

        public Result<T[]> Query<T>(SqlSelect select, int defaultLimit, Func<SqlRow, int> which, params Func<SqlRow, T>[] create) => Query(select, defaultLimit, defaultLimit, which, create);

        public Result<T[]> Query<T>(SqlSelect select, Func<SqlRow, int> which, params Func<SqlRow, T>[] create) => Query(select, 30, 30, which, create);

        public Result<T[]> Query<T>(SqlSelect select, int defaultLimit, int maximumLimit, Func<SqlRow, int> which, params Func<SqlRow, T>[] create)
        {
            if (which == null) throw new ArgumentNullException(nameof(which));

            var key = select.KeyColumn;

            if (key is null && select.SelectColumns.Length > 0)
            {
                key = select.SelectColumns[0];
            }

            if (key is null) return Unavailable(new ErrorResult("query", "key is null"));

            bool after = false;

            if (PagingParameters != null)
            {
                if (PagingParameters.Limit == -1)
                    select.LimitLength = defaultLimit;
                else
                    select.LimitLength = PagingParameters.Limit;

                if (select.LimitLength > maximumLimit)
                    select.LimitLength = maximumLimit;

                // if after
                if (PagingParameters.After != null)
                {
                    select.SetInnerCondition(key > Base64.UrlDecode(PagingParameters.After));
                    after = true;
                }
                else
                {
                    // if theres no after, use offset
                    select.OffsetLength = PagingParameters.Offset;
                    after = false;
                }

                select.Order = new SqlOrder();
                var isSort = false;

                if (PagingParameters.Sorts != null)
                {
                    foreach (var sort in PagingParameters.Sorts)
                    {
                        string field = null;
                        var order = Order.Ascending;
                        int dx = -1;
                        if (sort.Length > 1 && (dx = sort[0].IndexIn('+', '-')) > -1)
                        {
                            order = dx == 0 ? Order.Ascending : Order.Descending;
                            field = sort.Substring(1);
                        }
                        else
                            field = sort;

                        var sxp = select.GetFilterColumn(field);

                        if (sxp is not null)
                        {
                            isSort = true;

                            if (select.Order == null)
                                select.Order = SqlOrder.By(sxp, order);
                            else
                                select.Order.Add(sxp, order);
                        }
                    }


                }

                if (!isSort)
                    select.Order = SqlOrder.By(key, Order.Ascending);
            }

            if (select.LimitLength == 0) select.LimitLength = defaultLimit;
            else if (select.LimitLength > maximumLimit) select.LimitLength = maximumLimit;

            if (select.Execute(out SqlResult result))
            {
                var props = typeof(T).GetProperties();

                var current = result.To(row =>
                {
                    var wh = which(row);
                    if (wh >= create.Length)
                        throw new IndexOutOfRangeException(nameof(which));

                    T res = create[wh](row);

                    if (res is Resource re)
                    {
                        if (re.Id == null)
                        {
                            var cell = row[key.Name ?? key.Alias];

                            if (!cell.IsNull)
                            {
                                Type type = cell.Type;
                                string setid = null;

                                if (type == typeof(short))
                                    setid = Base64.UrlEncode(BitConverter.GetBytes(cell.GetShort()));
                                else if (type == typeof(ushort))
                                    setid = Base64.UrlEncode(BitConverter.GetBytes(cell.GetUshort()));
                                else if (type == typeof(int))
                                    setid = Base64.UrlEncode(BitConverter.GetBytes(cell.GetInt()));
                                else if (type == typeof(uint))
                                    setid = Base64.UrlEncode(BitConverter.GetBytes(cell.GetUint()));
                                else if (type == typeof(long))
                                    setid = Base64.UrlEncode(BitConverter.GetBytes(cell.GetLong()));
                                else if (type == typeof(ulong))
                                    setid = Base64.UrlEncode(BitConverter.GetBytes(cell.GetUlong()));
                                else if (type == typeof(string))
                                    setid = Base64.UrlEncode(cell.GetString().TrimEnd());
                                else if (type == typeof(Guid))
                                    setid = Base64.UrlEncode(cell.GetGuid());

                                re.Id = setid;
                            }
                        }

                        var links = new List<Link>();

                        if (ActionParameters.Self != null)
                        {
                            var pe = ActionParameters.Self;

                            foreach (var pt in props)
                            {
                                var val = pt.GetValue(re);

                                string rep = null;
                                if (val != null) rep = val.ToString();

                                pe = pe.Replace($"{{{pt.Name}}}", rep, StringComparison.InvariantCultureIgnoreCase);
                            }

                            links.Add(new Link { Rel = "self", Href = pe });
                        }

                        if (links.Count > 0)
                            re.Links = links.ToArray();
                    }

                    return res;

                });

                if (PagingParameters != null)
                {
                    return new PagingResult<T[]>
                    {
                        Result = current,
                        Total = select.ExecuteCount(),
                        Count = current.Length,
                        Offset = !after ? select.OffsetLength : -1
                    };
                }
                else
                    return current;
            }
            else
            {
                if (result?.Count == 0)
                    return NotFound();
                else
                    return Unavailable(new ErrorResult("query", select.Database.LastException.Exception.Message.Split("\r\n")));
            }
        }


        #endregion
    }

}

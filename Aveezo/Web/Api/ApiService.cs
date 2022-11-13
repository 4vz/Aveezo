using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Aveezo
{
    public abstract class ApiService
    {
        #region Fields

        protected IServiceProvider Provider { get; }

        private IDatabaseService databaseService;

        protected ApiOptions Options { get; }

        /// <summary>
        /// Default Sql.
        /// </summary>
        protected Sql Sql { get; }

        private readonly Dictionary<string, Sql> sqls = new();

        #endregion

        #region Constructors

        public ApiService(IServiceProvider provider)
        {
            Provider = provider;

            Options = Provider.GetService<IOptions<ApiOptions>>().Value;

            databaseService = Provider.GetService<IDatabaseService>();

            Sql = databaseService.Sql(null);
        }

        #endregion

        #region Methods

        public Sql SqlLoad(string name) => databaseService.Sql(name);

        #endregion

        #region Statics

        public static bool IsPagingResult(MethodInfo methodInfo, out Type arrayType)
        {
            arrayType = null;
            if (methodInfo != null && !methodInfo.Has<NoPagingAttribute>() && methodInfo.ReturnType != null && methodInfo.ReturnType.IsAssignableToGenericType(typeof(Result<>), out Type[] rtype) && rtype[0].IsArray)
            {
                arrayType = rtype[0];
                return true;
            }
            else
                return false;
        }

        public static bool IsResourceReturnType(MethodInfo method, out Type resourceType)
        {
            resourceType = null;

            var returnType = method.ReturnType;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Result<>))
                returnType = returnType.GetGenericArguments()[0];
            if (returnType.IsArray)
                returnType = returnType.GetElementType();


            if (returnType.IsAssignableTo(typeof(Resource)))
            {
                resourceType = returnType;
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}

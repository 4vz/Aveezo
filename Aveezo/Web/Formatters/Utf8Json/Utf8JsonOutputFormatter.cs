using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Formatters;
using Utf8Json.Resolvers;

namespace Aveezo
{
    public class Utf8JsonOutputFormatter : TextOutputFormatter
    {
        #region Fields

        private IServiceProvider provider;

        #endregion

        #region Constructors

        public Utf8JsonOutputFormatter()
        {            
            SupportedMediaTypes.Add("application/json");
            SupportedEncodings.Add(Encoding.UTF8);
        }

        #endregion

        #region Methods

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var obj = context.Object;

            if (obj != null)
            {
                await context.HttpContext.Response.WriteAsync(selectedEncoding.GetString(JsonSerializer.SerializeUnsafe(obj, Utf8JsonResolver.Default)));
            }
        }

        #endregion
    }

    public class Utf8JsonResolver : IJsonFormatterResolver
    {
        #region Fields        

        private static IJsonFormatterResolver defaultResolver = null;

        public static IJsonFormatterResolver Default
        {
            get
            {
                if (defaultResolver == null)
                {
                    defaultResolver = new Utf8JsonResolver();
                }

                return defaultResolver;
            }
        }

        public Dictionary<Type, IJsonFormatter> formatters;

        #endregion

        #region Constructors

        public Utf8JsonResolver()
        {
            formatters = new();

            formatters.Add(typeof(object), new Utf8Formatter<object>());
        }

        #endregion

        #region Methods

        public IJsonFormatter<T> GetFormatter<T>()
        {
            var type = typeof(T);

            if (formatters.TryGetValue(type, out var formatter))
                return (IJsonFormatter<T>)formatter;
            else
                return StandardResolver.Default.GetFormatter<T>();
        }

        public string GetPropertyName(PropertyInfo propertyInfo)
        {
            return propertyInfo.Name.ToSnakeCase();
        }

        #endregion
    }

    public class Utf8Formatter<T> : IJsonFormatter<T>
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
        {
            var resolver = (Utf8JsonResolver)formatterResolver;

            if (value == null)
                writer.WriteNull();
            else
            {
                writer.WriteBeginObject();

                var type = value.GetType();

                var index = 0;
                foreach (var prop in type.GetProperties())
                {
                    if (index > 0)
                        writer.WriteValueSeparator();

                    writer.WritePropertyName(resolver.GetPropertyName(prop));

                    var childValue = prop.GetValue(value);
                    var childType = prop.PropertyType;

                    var innerFormatter = formatterResolver.GetFormatterDynamic(childType);

                    if (innerFormatter != null)
                    {
                        if (childType.IsNullable())
                        {

                        }


                        //if (childType.IsGenericType && childType.Is 

                        //var formatter = (IJsonFormatter<TResult>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(childType), new object[] { innerFormatter });


                    }



                    /*var childTypeInfo = childType.GetTypeInfo();



                    IJsonFormatter<TResult> formatter = null;

                    if (childTypeInfo.IsNullable())
                    {
                        childTypeInfo = childTypeInfo.GenericTypeArguments[0].GetTypeInfo();

                        if (!childTypeInfo.IsEnum)
                        {
                            var childTypeInfoType = childTypeInfo.AsType();
                            var innerFormatter = formatterResolver.GetFormatterDynamic(childTypeInfoType);

                            if (innerFormatter != null)
                            {
                                formatter = (IJsonFormatter<TResult>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(childTypeInfoType), new object[] { innerFormatter });
                            }
                        }
                    }*/







                    /*

                    

                    var innerFormatter = formatterResolver.GetFormatterDynamic(childTypeInfo.AsType());

                   
                    if (innerFormatter != null)
                    {
                        var wrappedFormatter = (IJsonFormatter<TResult>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(childTypeInfo.AsType()), new object[] { innerFormatter });

                        wrappedFormatter.Serialize(ref writer, (TResult)childValue, formatterResolver);
                    }*/

                    //StaticNullableFormatter<>

                    //IJsonFormatter<> childFormatter = (IJsonFormatter<>)formatterResolver.GetFormatterDynamic(childType);


                    //formatterResolver.GetFormatterWithVerify<TResult>().Serialize(ref writer, (TResult), formatterResolver);

                    index++;
                }




                writer.WriteEndObject();
            }
        }
    }
}
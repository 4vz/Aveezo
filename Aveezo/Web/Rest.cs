using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Xml.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Formatters;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Utf8Json;
using Utf8Json.Resolvers;
using Utf8Json.AspNetCoreMvcFormatter;

namespace Aveezo;

public abstract class Rest : App
{
    #region Fields

    protected List<Assembly> Assemblies { get; } = new();

    private readonly ApiOptions apiOptions = new();

    protected string DatabaseConfigName { get; init; } = null;

    #endregion

    #region Constructors

    public Rest()
    {
        Starting += () =>
        {
            Event("Rest.Start");

            if (DatabaseConfigName == null)
            {
                FatalError("DatabaseConfigName is required");
            }
        };
        Started += () =>
        {
            apiOptions.DatabaseConfigName = DatabaseConfigName;

            Event("Rest.Started");
        };

        Assemblies.Add(Assembly.GetCallingAssembly());
    }

    #endregion

    #region Methods

    public void Options(Action<ApiOptions> apiOptionsAction)
    {
        Starting += () =>
        {
            apiOptionsAction?.Invoke(apiOptions);
            apiOptions.Config = Config;
        };
    }

    public void ConfigureServices(IServiceCollection services)
    {
        ServiceProvider provider = null;
        var assemblies = Assemblies.ToArray();

        XmlPrefix xmlPrefix = null;

        if (apiOptions.EnableSoapXml)
        {
            xmlPrefix = new XmlPrefix
            {
                Local = apiOptions.XmlPrefix,
                LocalDomain = apiOptions.WsdlDomain
            };
        }

        // Its a MVC
        var mvcBuilder = services.AddMvcCore(options =>
        {
            options.Filters.Clear();

            if (apiOptions.EnableAuth)
                options.Filters.Add<AuthorizationFilter>(-100002);

            options.Filters.Add<ResourceFilter>(-100002);
            options.Filters.Add<ActionFilter>(-100000);
            options.Filters.Add<ResultFilter>(-99999);

            // Input Formatters
            var via = (SystemTextJsonInputFormatter)options.InputFormatters.Find(typeof(SystemTextJsonInputFormatter));
            via.SupportedMediaTypes.Clear();
            via.SupportedMediaTypes.Add("application/json");

            options.InputFormatters.Add(new FormInputFormatter());

            // Output Formatters
            options.OutputFormatters.RemoveType<StringOutputFormatter>();

            var voa = (SystemTextJsonOutputFormatter)options.OutputFormatters.Find(typeof(SystemTextJsonOutputFormatter));
            voa.SupportedMediaTypes.Clear();
            voa.SupportedMediaTypes.Add("application/json");

            if (apiOptions.EnableSoapXml)
                options.AddSoapXml(xmlPrefix, assemblies);

        });



        mvcBuilder.ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        // DataAnnotations for API
        mvcBuilder.AddDataAnnotations();

        // system.text.json options
        mvcBuilder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new BitArrayJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new PhysicalAddressJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new IPAddressCidrJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new IPAddressJsonConverter());

            var namingPolicy = new NamingPolicy();

            options.JsonSerializerOptions.PropertyNamingPolicy = namingPolicy;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        foreach (var assembly in assemblies)
        {
            mvcBuilder.AddApplicationPart(assembly);
        }

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.Conventions.Add(new VersionNamespaceConvention(apiOptions.ControllerGroupNamespacePrefix));
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "S'-v'V";
            options.SubstituteApiVersionInUrl = true;
            options.SubstitutionFormat = "S'/v'V";
        });

        if (apiOptions.EnableDocumentation)
        {
            services.AddSwaggerGen(options =>
            {
                options.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string" });
                options.MapType<BitArray>(() => new OpenApiSchema { Type = "string" });
                options.MapType<PhysicalAddress>(() => new OpenApiSchema { Type = "string" });
                options.MapType<IPAddressCidr>(() => new OpenApiSchema { Type = "string" });
                options.MapType<IPAddress>(() => new OpenApiSchema { Type = "string" });

                // Get Xml Comments
                foreach (var ic in (new DirectoryInfo(AppContext.BaseDirectory)).GetFiles("*.xml"))
                {
                    var str = File.ReadAllText(ic.FullName);
                    var valid = false;
                    if (!string.IsNullOrEmpty(str) && str.TrimStart().StartsWith("<"))
                    {
                        try
                        {
                            XDocument.Parse(str);
                            valid = true;
                        }
                        catch { }
                    }

                    if (valid)
                        options.IncludeXmlComments(ic.FullName, true);
                }


                if (apiOptions.EnableSoapXml)
                {
                    options.AddSoapXml(xmlPrefix);
                }

                options.DocumentFilter<DocumentationFilter>();
                options.OperationFilter<DocumentationFilter>();
                options.SchemaFilter<DocumentationFilter>();

                // Get version description                
                var versionService = provider.GetRequiredService<IApiVersionDescriptionProvider>();

                // get groups
                var groups = GetApiVersionGroups(versionService.ApiVersionDescriptions);

                foreach (var group in groups)
                {
                    foreach (var description in group.Value)
                    {
                        var documentOptions = new OpenApiInfo()
                        {
                            Title = apiOptions.DocumentationName,
                            Version = description.ApiVersion.ToString(),

                            //Description = "A sample application with Swagger, Swashbuckle, and API versioning.",
                            //Contact = new OpenApiContact() { Name = "Bill Mei", Email = "bill.mei@somewhere.com" },
                            //License = new OpenApiLicense() { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
                        };

                        if (description.IsDeprecated)
                        {
                            documentOptions.Description += " This API version has been deprecated.";
                        }

                        var documentName = $"{group.Key}-v{description.ApiVersion.MajorVersion}";

                        options.SwaggerDoc(documentName, documentOptions);
                    }
                }
            });
        }

        if (apiOptions.EnableAuth)
        {
            services.AddScoped<IAuthService, AuthService>();
        }

        services.Configure<ApiOptions>(options => options.CopyFrom(apiOptions));

        services.AddSingleton<IDatabaseService, DatabaseService>();

        services.AddSingleton<ITypeRepository, TypeRepository>(factory =>
        {
            var types = new List<Type>();
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) types.Add(type);
            foreach (var assembly in assemblies) types.AddRange(assembly.GetTypes());
            var typeCollections = new TypeRepository(types.ToArray());

            return typeCollections;
        });

        provider = services.BuildServiceProvider();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider provider)
    {
        var versionService = provider.GetService<IApiVersionDescriptionProvider>();

        // HSTS
        app.UseHsts();

        // Redirect HTTP to HTTPS
        app.UseHttpsRedirection();

        var groups = GetApiVersionGroups(versionService.ApiVersionDescriptions);

        // Use Documentation
        if (apiOptions.EnableDocumentation)
        {
            app.Use(async (context, next) =>
            {
                // reroute to openapi schema
                // only in documentation

                if (context.Request.Path != null)
                {
                    var paths = context.Request.Path.ToString().ToLower();

                    if (paths.StartsWith("/openapi", out string remaining))
                    {
                        string newurl = null;
                        var ox = remaining.Split(Collections.Slash, StringSplitOptions.RemoveEmptyEntries);

                        if (ox.Length == 2) newurl = $"/openapi/{ox[0].Replace("v", "ooo")}-{ox[1]}";
                        else if (ox.Length == 1) newurl = $"/openapi/main-{ox[0]}";

                        context.Request.Path = newurl;
                    }
                }

                await next.Invoke();
            });

            app.UseSwagger(options =>
            {
                options.RouteTemplate = "openapi/{documentname}";
            });

            foreach (var group in groups)
            {
                var spec = $"{apiOptions.RoutePrefix.TrimStart('/')}{(group.Key != "main" ? $"/{group.Key.ToLower().Replace("ooo", "v")}" : "")}";

                var endpoint = new List<(string, string)>();

                foreach (var description in group.Value)
                {
                    var gname = $"V{description.ApiVersion.MajorVersion}";
                    var schemaUrl = $"/openapi/{(group.Key != "main" ? $"{group.Key.Replace("ooo", "v")}/" : "")}v{description.ApiVersion.MajorVersion}";
                    endpoint.Add((schemaUrl, gname));
                }


                app.UseSwaggerUI(options =>
                {
                    options.RoutePrefix = spec;

                    foreach (var (schemaUrl, gname) in endpoint)
                    {
                        options.SwaggerEndpoint(schemaUrl, gname);
                    }

                    options.DocumentTitle = apiOptions.DocumentationName;
                    options.SupportedSubmitMethods();
                    options.DefaultModelsExpandDepth(-1);
                });

            }
        }

        // Check Content-Type & Accept
        app.Use(async (context, next) =>
        {
            if (context.Request.Path != null)
            {
                // reroute controller with version-group
                string newpath = null;
                string controllerpath = null;

                var originalPath = context.Request.Path.ToString();
                var lowerPath = context.Request.Path.ToString().ToLower();

                foreach (var (group, _) in groups)
                {
                    // /group/v1/path/to/controller
                    if (lowerPath.StartsWith($"/{group.Replace("ooo", "v")}", originalPath, out string remaining1))
                    {
                        ParseRoutingPath(remaining1, out string version, out string remaining2);
                        controllerpath = remaining2;

                        if (version != null && version.Length > 1)
                        {
                            newpath = $"/{version}-{group}{remaining2}";
                            break;
                        }
                    }
                }
                if (newpath == null)
                {
                    ParseRoutingPath(originalPath, out string version, out string remaining);
                    controllerpath = remaining;

                    if (version != null && version.Length > 1 && !version.Contains("-"))
                        newpath = $"/{version}-main{remaining}";
                }

                if (newpath != null)
                {
                    context.Items.Add("originalPath", context.Request.Path.Value);
                    context.Items.Add("controllerPath", controllerpath);
                    context.Request.Path = newpath;
                }
            }

            var qsContentType = context.Request.Query["contentType"].ToString();

            if (qsContentType == "application/soap+xml")
                context.Request.ContentType = "application/soap+xml";
            else if (qsContentType == "application/json")
                context.Request.ContentType = "application/json";

            var contentType = context.Request.ContentType;

            var accept = context.Request.Headers["Accept"].ToString();

            if (contentType != null)
            {
                if (accept == "*/*")
                {
                    context.Request.Headers["Accept"] = contentType;
                }
            }

            await next.Invoke();
        });

        // Route soap-xml
        if (apiOptions.EnableSoapXml)
        {
            app.UseSoapXml();
        }

        // Route to controllers
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

    }

    private void ParseRoutingPath(string path, out string version, out string remaining)
    {
        version = null;
        remaining = null;

        // /v1/path/to/controller
        if (path.StartsWith("/v") && path.Length > 2)
        {
            var nextslash = path.IndexOf('/', 1);

            if (nextslash > -1)
            {
                version = path.Substring(1, nextslash - 1);
                remaining = path.Substring(nextslash);
            }
            else
                version = path.Substring(1);
        }
    }

    private Dictionary<string, List<ApiVersionDescription>> GetApiVersionGroups(IReadOnlyList<ApiVersionDescription> apiVersionDescriptions)
    {
        var dict = new Dictionary<string, List<ApiVersionDescription>>();

        foreach (var description in apiVersionDescriptions)
        {
            var apiVersion = description.ApiVersion;

            var status = apiVersion.Status;
            //status = status?.Replace("ooo", "v");

            if (status != null)
            {
                List<ApiVersionDescription> list;
                if (dict.ContainsKey(status)) list = dict[status];
                else
                {
                    list = new List<ApiVersionDescription>();
                    dict.Add(status, list);
                }

                list.Add(description);
            }
        }

        return dict;
    }

    #endregion
}
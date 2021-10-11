using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Aveezo
{
    public delegate void AppStartCallback(string directory);

    public delegate void AppStopCallback();

    public abstract partial class App
    {
        #region Fields

        #endregion

        #region Methods

        private static void Start(App app, AppStartCallback start, AppStopCallback stop)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-us");

            string directory = AppDomain.CurrentDomain.BaseDirectory;

            if (app is Rest rest)
            {
                var hostBuilder = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs());
                hostBuilder.ConfigureWebHostDefaults(builder =>
                {
                    builder.UseStartup(delegate (WebHostBuilderContext c)
                    {                        
                        return rest;
                    });
                });

                // start app
                start(directory);

                rest.Started += () =>
                {
                    // start ho
                    var cancel = new CancellationTokenSource();
                    var hostTask = hostBuilder.Build().RunAsync(cancel.Token);

                    // bind host to app
                    app.BindProcess(hostTask);
                    app.BindProcess(cancel);
                };
                
                app.Wait();
            }
            else
            {
                if (Environment.UserInteractive)
                {
                    start(directory);

                    if (app != null)
                        app.Wait();
                }
                else
                {
                }
            }
        }        

        public static void Start(App app) => Start(app, (directory) => app.Start(directory), () => app.Stop());

        public static void Start(AppStartCallback start) => Start(null, start, null);

        public static void Start(AppStartCallback start, AppStopCallback stop) => Start(null, start, stop);

        #endregion
    }
}

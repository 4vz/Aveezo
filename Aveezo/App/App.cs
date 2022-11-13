using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aveezo;

public interface IApp
{
    public void Event(string[] messages);

    public void Event(string message);

    public void Event(string[] messages, string label);

    public void Event(string message, string label);

    public void Event(string[] messages, string label, string subLabel);

    public void Event(string message, string label, string subLabel);

    public void Error(string[] messages, string label);

    public void Error(string message, string label);

    public void Error(Exception exception, string label);
}

public abstract partial class App : IApp
{
    #region Fields

    private Task main = null;

    private CancellationTokenSource cancel = null;

    public string Directory { get; set; }

    public string ConfigFile { get; init; }

    public bool ConfigRequired { get; init; } = false;

    public Config Config { get; private set; }

    public string ErrorFile { get; set; } = "error";

    public bool IsRunning { get; private set; } = false;

    private AutoResetEvent mainLoopWait = new AutoResetEvent(false);

    protected event Action Starting;

    protected event Action Started;

    private List<Task> bindTasks = new List<Task>();

    private List<CancellationTokenSource> bindCancels = new List<CancellationTokenSource>();

    #endregion

    #region Constructors

    public App()
    {
    }

    #endregion

    #region Handlers

    protected void SqlLoadStatusEventHandler(object sender, SqlLoadEventArgs args)
    {
        var sql = sender as Sql;

        if (sql != null)
        {
            var name = sql.Name;
            var success = args.Success;
            var exception = args.Exception;

            Event($"INFO: {sql.DatabaseType} ", name);

            if (success)
            {
                Event($"OK: {(!string.IsNullOrEmpty(sql.Database) ? $"{sql.Database}:" : "")}{sql.User}", name);
            }
            else
            {
                if (exception != null)
                    Event($"FAILED: {exception.Message}", name);
                else
                    Event($"FAILED", name);

#if DEBUG
                Event($"STRING: {sql.Connection.ConnectionString}", name);
#endif
            }
        }
    }

    #endregion

    #region Methods

    private async Task Main()
    {
        Event("App.Start");

        var configRequiredPassed = true;

        Event("Checking for main config file...", "CONFIG");

        if (ConfigFile != null)
        {
            if (ConfigRequired)
            {
                Event("Config is required", "CONFIG");
                var configFileInfo = new FileInfo(Path.Combine(Directory, ConfigFile));

                if (!configFileInfo.Exists)
                {
                    configRequiredPassed = false;
                }
                else
                {
                    Config = Config.Load(Directory, ConfigFile);

                    if (Config)
                        Event($"Config OK {Config.Path}", "CONFIG");
                    else
                    {
                        Event($"Config {ConfigFile} cannot be loaded", "CONFIG");
                        configRequiredPassed = false;
                    }
                }
            }
            else
            {
                Config = Config.Load(Directory, ConfigFile);

                if (Config)
                    Event($"Config OK {Config.Path}", "CONFIG");
                else
                    Event($"Config {ConfigFile} cannot be loaded", "CONFIG");
            }
        }

        if (!configRequiredPassed)
        {
            FatalError($"Config cannot be loaded: ConfigFile = {Path.Combine(Directory, ConfigFile)}, ConfigRequired = {ConfigRequired}");
            return;
        }

        Starting?.Invoke();

        var start = await OnStart();

        if (start)
        {
            Event("App.Started");

            Started?.Invoke();

            IsRunning = true;

            while (true)
            {
                mainLoopWait.WaitOne(100);

                if (cancel.IsCancellationRequested)
                {
                    Terminal.Cancel();
                    break;
                }
                else
                {
                    var oneBindingTasksBeingCancelled = false;

                    foreach (var task in bindTasks)
                    {
                        if (task.IsCompleted || task.IsCanceled)
                        {
                            oneBindingTasksBeingCancelled = true;
                            break;
                        }
                    }

                    if (oneBindingTasksBeingCancelled)
                    {
                        cancel.Cancel();
                        Terminal.Cancel();
                        break;
                    }
                }

                await OnLoop();
            }

            IsRunning = false;

            cancel.Dispose();
            cancel = null;

            await OnStop();
        }

        Event("App.End");
    }

    public void Set()
    {
        mainLoopWait.Set();
    }

    private string WriteErrorLog(string message)
    {
        var line = $"{DateTime.UtcNow:yyyy/MM/dd:HH:mm:ss.fff}|ERROR|{message}";

        if (ErrorFile != null)
        {
            var path = Path.Combine(Directory, ErrorFile);
            File.AppendAllText(path, $"{line}{Environment.NewLine}");
        }

        return line;
    }

    internal void Join()
    {
        try
        {
            main.Wait();
        }
        catch (Exception ex)
        {
            OnEvent(WriteErrorLog($"{(ex.InnerException != null ? ex.InnerException.Message : ex.Message)}"), 0);
            Thread.Sleep(5000);
        }
    }

    public void Start()
    {
        Start(AppDomain.CurrentDomain.BaseDirectory);
    }

    public void Start(string directory)
    {
        if (main == null)
        {
            Directory = directory;

            cancel = new CancellationTokenSource();

            main = Task.Run(Main);
        }
    }

    public void Stop()
    {
        if (main != null && cancel != null)
        {
            mainLoopWait.Set();
            cancel.Cancel();
        }

        // cancel all bindings
        foreach (var c in bindCancels)
        {
            c.Cancel();
        }
    }

    /// <summary>
    /// Stop the program, and write log in the error file.
    /// </summary>
    public void FatalError(string message)
    {
        OnEvent(WriteErrorLog($"{message}"), 0);
        Stop();

        Thread.Sleep(3000);
    }

    public void BindProcess(Task task)
    {
        if (task != null && !bindTasks.Contains(task))
            bindTasks.Add(task);
    }

    public void BindProcess(CancellationTokenSource cancel)
    {
        if (cancel != null && !bindCancels.Contains(cancel))
            bindCancels.Add(cancel);
    }

    public void Wait()
    {
        Join();
        Stop();

        foreach (var task in bindTasks)
        {
            task.Wait();
        }
    }

    #endregion

    #region Virtuals

    protected virtual async Task OnLoop() { }

    protected virtual async Task<bool> OnStart() => true;

    protected virtual async Task OnStop() { }

    protected virtual void OnEvent(string message, int repeat)
    {
        if (Terminal.IsAvailable)
        {
            if (repeat > 0)
            {
                //Terminal.Up();
                //Terminal.ClearCurrentLine();
                Terminal.WriteLine($"{message} ({repeat + 1})");
            }
            else
            {
                Terminal.WriteLine(message);
            }
        }
    }

    #endregion
}

﻿using System;
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
    public Task Event(string message);

    public Task Event(string[] messages);

    public Task Event(string message, string label);

    public Task Event(string[] messages, string label);

    public Task Error(string message);

    public Task Error(string[] messages);

    public Task Error(string message, string label);

    public Task Error(string[] messages, string label);

    public Task Error(Exception exception);

    public Task Error(Exception exception, string label);
}

public abstract partial class App : IApp
{
    #region Fields

    private Task main = null;

    private CancellationTokenSource cancel = null;

    private AutoResetEvent mainLoopWait = new AutoResetEvent(false);

    private List<Task> bindTasks = new List<Task>();

    private List<CancellationTokenSource> bindCancels = new List<CancellationTokenSource>();

    public string Directory { get; set; }

    public string ConfigFile { get; init; }

    public bool ConfigRequired { get; init; } = false;

    public Config Config { get; private set; }

    public string ErrorFile { get; set; } = "error";

    public bool IsRunning { get; private set; } = false;

    public bool IsFatalError { get; private set; }

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

            Event($"INFO:Type={sql.DatabaseType} Database={(!string.IsNullOrEmpty(sql.Database) ? sql.Database : "<none>")} User={sql.User}", name);

            if (success)
            {
                Event($"OK", name);
            }
            else
            {
                if (exception != null)
                    Event($"FAILED:{exception.Message}", name);
                else
                    Event($"FAILED", name);

#if DEBUG
                Event($"STRING:{sql.Connection.ConnectionString}", name);
#endif
            }
        }
    }

    #endregion

    #region Methods

    private async Task Main()
    {
        Event("App.Starting");

        var configRequiredPassed = true;

        if (ConfigFile != null)
        {
            Event("Checking config file...", "CONFIG");

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

        var start = await OnStart();

        if (start)
        {
            Event("App.Started");

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
        IsFatalError = true;

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

    protected virtual Task OnLoop() => Task.CompletedTask;

    protected virtual Task<bool> OnStart() => Task.FromResult(true);

    protected virtual Task OnStop() => Task.CompletedTask;

    protected virtual async Task OnEvent(string message, int repeat)
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

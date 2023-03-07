using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aveezo;

public enum PromptStage
{
    None,
    WritingMarker,
    PromptReadyEvent,
    PromptUnchangedEvent,
    Ready
}

public class Shell
{
    #region Fields

    /// <summary>
    /// Internal shell for DataWrite.
    /// </summary>
    internal event EventHandler<SshSendDataEventArgs> DataWrite;

    /// <summary>
    /// Shell prompt.
    /// </summary>
    public string Prompt { get; private set; } = null;

    private string marker;

    public PromptStage PromptStage = PromptStage.None;

    public bool IsExecuting => ExecutingShell != null;

    public Shell ExecutingShell { get; private set; } = null;

    public Shell ParentShell { get; private set; } = null;

    public string RequestString { get; init; } = null;

    public event EventHandler Idle;

    public event EventHandler Ready;

    public event EventHandler PromptReady;

    public event EventHandler<SshReceivedDataEventArgs> DataReceived;

    public event EventHandler<ShellRequestingEventArgs> Requesting;

    private int promptReadyIdleCounter = 0;

    private int promptCounter = 0;

    public event EventHandler<ShellEventHandlingEventArgs> EventHandling;

    public object Tag { get; set; }

    #endregion

    #region Constructors

    private Shell(EventHandler<SshSendDataEventArgs> write, string requestString)
    {
        marker = Rnd.String(5, Collections.WordDigit);
        DataWrite = write;
        RequestString = requestString;
    }

    internal Shell(EventHandler<SshSendDataEventArgs> write) : this(write, null)
    {
    }

    public Shell(string requestString) : this(null, requestString)
    {
    }

    public Shell() : this(null, null)
    {
    }

    #endregion

    #region Methods

    private void HandleEvent(string[] messages, string label, bool error, Exception exception, Shell sender)
    {
        if (EventHandling != null)
            EventHandling.Invoke(sender, new ShellEventHandlingEventArgs(messages, label, error, exception));
        else if (ParentShell != null)
            ParentShell.HandleEvent(messages, label, error, exception, sender);
    }

    public void Event(string message) => HandleEvent(message.Array(), null, false, null, this);

    public void Event(string[] messages) => HandleEvent(messages, null, false, null, this);

    public void Event(string message, string label) => HandleEvent(message.Array(), label, false, null, this);

    public void Event(string[] messages, string label) => HandleEvent(messages, label, false, null, this);

    public void Error(string message) => HandleEvent(message.Array(), null, true, null, this);

    public void Error(string[] messages) => HandleEvent(messages, null, true, null, this);

    public void Error(string message, string label) => HandleEvent(message.Array(), label, true, null, this);

    public void Error(string[] messages, string label) => HandleEvent(messages, label, true, null, this);

    public void Error(Exception exception) => HandleEvent(null, null, true, exception, this);

    public void Error(Exception exception, string label) => HandleEvent(null, label, true, exception, this);

    private void Dispose(Shell shell)
    {
        if (shell != null)
        {
            shell.ParentShell = null;
            Dispose(shell.ExecutingShell);
            shell.ExecutingShell = null;
        }
    }

    internal void OnDataReceived(object sender, SshReceivedDataEventArgs e)
    {
        promptReadyIdleCounter = 0;

        if (IsExecuting)
        {
            ExecutingShell.OnDataReceived(this, e);

            if (e.CurrentLine == Prompt)
            {
                Dispose(ExecutingShell);
                ExecutingShell = null;
                OnPromptReady();                
            }
        }
        else
        {
            if (PromptStage == PromptStage.WritingMarker)
            {
                if (e.CurrentLine.EndsWith(marker))
                {
                    var prompt = e.CurrentLine[..^marker.Length];

                    if (Prompt == null || prompt != Prompt)
                    {
                        PromptStage = PromptStage.PromptReadyEvent;
                        Prompt = prompt;
                    }
                    else
                    {
                        PromptStage = PromptStage.PromptUnchangedEvent;
                    }
                }

                // erase marker
                SendLine($"{(char)8}{(char)8}{(char)8}{(char)8}{(char)8}");
            }            
            else if (PromptStage == PromptStage.PromptReadyEvent)
            {
                PromptStage = PromptStage.Ready;
                OnPromptReady();
            }
            else if (PromptStage == PromptStage.PromptUnchangedEvent)
            {
                PromptStage = PromptStage.Ready;
            }
            else
            {
                DataReceived?.Invoke(this, e);
            }
        }
    }

    internal void OnIdle(object sender, EventArgs e)
    {
        if (IsExecuting)
        {
            ExecutingShell.OnIdle(this, e);
        }
        else
        {
            if (PromptStage == PromptStage.None)
            {
                PromptStage = PromptStage.WritingMarker;
                Send(marker);
            }
            else if (PromptStage == PromptStage.Ready)
            {
                promptReadyIdleCounter++;

                if (promptReadyIdleCounter == 21)
                {
                    PromptStage = PromptStage.WritingMarker;
                    Send(marker);
                }
                else
                    Idle?.Invoke(this);
            }
        }
    }

    internal void OnDisconnected(object sender, EventArgs e)
    {
        Dispose(ExecutingShell);
        ExecutingShell = null;
        PromptStage = PromptStage.None;
        promptCounter = 0;
        Prompt = null;

    }

    private void OnPromptReady()
    {
        promptCounter++;
        PromptReady?.Invoke(this);

        if (promptCounter == 1)
        {
            Ready?.Invoke(this);
        }
    }

    private void Start()
    {
        if (RequestString != null)
        {
            SendLine(RequestString);
        }
    }

    public void Send(string data)
    {
        if (ParentShell != null)
            ParentShell.Send(data);
        else
            DataWrite?.Invoke(this, new SshSendDataEventArgs(data));
    }

    public void SendLine(string data)
    {
        if (ParentShell != null)
            ParentShell.SendLine(data);
        else
            DataWrite?.Invoke(this, new SshSendDataEventArgs(data, true));
    }

    public async Task<string[]> Request(string requestString)
    {
        var shell = new Shell(requestString);
        var lines = new List<string>();
        shell.DataReceived += (s, e) =>
        {
            lines.AddRange(e.Lines);
        };

        await Request(shell);

        return lines.ToArray();
    }

    public async Task Request(Shell shell)
    {
        if (PromptStage == PromptStage.Ready)
        {
            ExecutingShell = shell;
            shell.ParentShell = this;
            shell.Start();

            Requesting?.Invoke(this, new ShellRequestingEventArgs(shell.RequestString));

            while (IsExecuting) await Task.Delay(10);
        }
    }

    #endregion

    #region Statics

    #endregion
}

public class SshShell : Shell
{
    #region Fields

    private string password;

    #endregion

    #region Constructors

    public SshShell(string hostName, string user, string password)
    {
        this.password = password;

        RequestString = $"ssh -oUserKnownHostsFile=/dev/null -oStrictHostKeyChecking=no {user}@{hostName}";

        DataReceived += SshShell_DataReceived;
    }

    #endregion

    #region Methods

    private void SshShell_DataReceived(object sender, SshReceivedDataEventArgs e)
    {
        if (e.CurrentLine.TrimEnd().EndsWith("password:"))
        {
            SendLine(password);
        }
    }


    #endregion

    #region Statics

    #endregion
}


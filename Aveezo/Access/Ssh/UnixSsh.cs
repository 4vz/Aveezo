using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Messages.Transport;

namespace Aveezo;

public class UnixSsh : Ssh
{
    #region Consts

    private const string dateCommand = "date --iso-8601=seconds";

    #endregion

    #region Fields

    private readonly StringBuilder temporaryLine = new StringBuilder();

    public string Prompt { get; private set; } = null;

    public TimeSpan TimeSpanOffset { get; private set; } = TimeSpan.MaxValue;

    public DateTime DateTime => sessionCheckStage > 2 ? DateTime.Now - TimeSpanOffset : DateTime.Now;

    //public bool IsPromptReady { get; private set; } = false;

    public bool IsQueueStop { get; private set; } = false;

    private bool requesting = false;

    private bool requestResultCommand = false;

    private List<string> requestResult = new List<string>();

    private string lastLine = null;

    public event EventHandler<UnixSshPromptChangedEventArgs> PromptChanged;

    public event EventHandler<UnixSshPromptReadyEventArgs> PromptReady;

    public event EventHandler<SshDataEventArgs> DataReceived;

    private int sessionCheckStage = -1;

    private bool firstPromptReady = false;

    public Request Request { get; private set; } = null;

    private List<WatchedPath> watchedPaths = new List<WatchedPath>();

    #endregion

    #region Constructors

    public UnixSsh() : base()
    {
        Construct();
    }

    public UnixSsh(uint shellColumns, uint shellRows, uint shellWidth, uint shellHeight) : base(shellColumns, shellRows, shellWidth, shellHeight)
    {
        Construct();
    }

    private void Construct()
    {
        Connected += UnixSsh_Connected;
        DataAvailable += UnixSsh_DataAvailable;
        Idle += UnixSsh_Idle;
        Disconnected += UnixSsh_Disconnected;
        ConnectionFail += UnixSsh_ConnectionFail;

        Request = new Request(this, "date --iso-8601=seconds");
    }

    #endregion

    #region Methods

    public async Task<string[]> Request2(string request)
    {
        //while (!IsPromptReady || requesting) await Task.Delay(10);

        //if (IsPromptReady && requesting == false)
        //{
        //    requestResult.Clear();
        //    requesting = true;
        //    requestResultCommand = false;

        //    WriteLine(request);
        //}

        //while (requesting) await Task.Delay(50);

        //return requestResult.ToArray();
        throw new NotImplementedException();
    }

    public void Check()
    {
        //if (IsPromptReady || (sessionCheckStage == -1 && lineBuilder.Length > 0))
        //{
        //    sessionCheckStage = 1;
        //    Write(dateCommand);
        //}
        throw new NotImplementedException();
    }

    public async Task QueueStop()
    {
        if (!IsQueueStop)
        {
            IsQueueStop = true;

            //if (IsPromptReady)
            //    await Stop();
        }
    }

    private void UnixSsh_ConnectionFail(object sender, SshConnectionFailEventArgs e)
    {
        if (IsQueueStop)
        {
            IsReconnect = false;
        }
    }

    private void UnixSsh_Connected(object sender, EventArgs e)
    {            
        temporaryLine.Clear();
       
        lastLine = null;

        //IsPromptReady = false;
        //sessionCheckStage = -1;
    }

    private void UnixSsh_DataAvailable(object sender, EventArgs e)
    {
        if (IsQueueStop && Prompt == null)
        {
            Stop();
            return;
        }

        //IsPromptReady = false;

        // Read data from stream
        string data = Stream.Read();

        // Split data to lines
        var lines = new List<string>();
        string[] splittedLines = data.Split(Collections.NewLine);        

        temporaryLine.Append(splittedLines[0]);

        if (splittedLines.Length >= 2)
        {
            lines.Add(temporaryLine.ToString());
            for (var valuesIndex = 1; valuesIndex < (splittedLines.Length - 1); valuesIndex++)
                lines.Add(splittedLines[valuesIndex]);

            temporaryLine.Clear();
            temporaryLine.Append(splittedLines[^1]);
        }

        if (lines.Count > 0) lastLine = lines[^1];

        // Data received event
        DataReceived?.Invoke(this, new SshDataEventArgs(data, lines.ToArray()));

        // Prompt

        string currentLineBuilder = temporaryLine.ToString();

        var endWithPrompt = (Prompt != null) && currentLineBuilder.EndsWith(Prompt);

       

        if (sessionCheckStage == 1)
        {
            if (currentLineBuilder.EndsWith(dateCommand))
            {
                sessionCheckStage = 2;

                var newPrompt = currentLineBuilder.Substring(0, currentLineBuilder.Length - dateCommand.Length);

                if (newPrompt != Prompt) 
                {
                    Prompt = newPrompt;

                    var promptChangedEventArgs = new UnixSshPromptChangedEventArgs(Prompt);

                    PromptChanged?.Invoke(this, promptChangedEventArgs);
                }

                WriteLine("");
            }
        }
        else if (sessionCheckStage == 2)
        {
            if (endWithPrompt)
            {
                sessionCheckStage = 0;
                TimeSpanOffset = DateTime.Now - DateTime.Parse(lastLine);
            }
        }

        if (sessionCheckStage == 0)
        {
            if (requesting) // collect response for request command
            {
                if (!requestResultCommand && requestResult.Count == 0)
                {
                    
                    for (int lineIndex = 1; lineIndex < lines.Count; lineIndex++)
                        requestResult.Add(lines[lineIndex]);

                    if (lines.Count > 0)
                        requestResultCommand = true;
                }
                else
                {
                    requestResult.AddRange(lines);
                }
            }
            if (endWithPrompt)
            {
                if (requesting)
                    requesting = false;

                IsPromptReady = true;

                if (IsQueueStop)
                    Stop();
                else
                {
                    var firstTime = !firstPromptReady;

                    if (!firstPromptReady)
                    {
                        firstPromptReady = true;

                        // executed during first promptready

                    }

                    var promptReadyEventArgs = new UnixSshPromptReadyEventArgs(firstTime);

                    // reserved for internal routine

                    //if (watchedPaths.Count > 0)
                    //{
                    //    var lspaths = new StringBuilder();
                    //    foreach (var watchedPath in watchedPaths)
                    //    {
                    //        if (lspaths.Length > 0) lspaths.Append(' ');
                    //        lspaths.Append(watchedPath.Path);
                    //    }

                    //    WriteLine($"ls --full-time {lspaths.ToString()}");


                    //}

                    // user routine

                    PromptReady?.Invoke(this, promptReadyEventArgs);
                }
            }
        }
    }

    private void UnixSsh_Idle(object sender, EventArgs e)
    {
        if (sessionCheckStage == -1)
        {
            Check();
        }
    }

    private async void UnixSsh_Disconnected(object sender, EventArgs e)
    {
        // reset fields to original values
        sessionCheckStage = -1;



        Prompt = null;
        //IsPromptReady = false;
        firstPromptReady = false;
        TimeSpanOffset = TimeSpan.MaxValue;

        requesting = false;
        requestResultCommand = false;
        requestResult.Clear();
    }

    protected WatchedPath Watch(string path, Action<WatchedPathChangedEventArgs> changed)
    {
        WatchedPath watchedPath = null;

        foreach (var wp in watchedPaths)
        {
            if (wp.Path == path)
            {
                watchedPath = wp;
                break;
            }
        }

        if (watchedPath == null)
        {
            watchedPath = new WatchedPath(path);
            watchedPath.Changed += changed;

            watchedPaths.Add(watchedPath);
        }
        else
        {

        }

        return watchedPath;
    }

    protected void Unwatch(WatchedPath watchedPath)
    {

    }

    #endregion
}



public class WatchedPath
{
    #region Fields

    public string Path { get; }

    public event Action<WatchedPathChangedEventArgs> Changed;

    public List<WatchedPathFile> Files { get; } = new List<WatchedPathFile>();

    #endregion

    #region Constructors

    public WatchedPath(string path)
    {
        Path = path;
    }

    #endregion
}

public class WatchedPathFile
{
    #region Fields

    public string Path { get; }

    public long Size { get; set; } = 0;

    #endregion

    #region Constructors

    public WatchedPathFile(string path)
    {
        Path = path;
    }

    #endregion
}

public class WatchedPathChangedEventArgs : EventArgs
{

}


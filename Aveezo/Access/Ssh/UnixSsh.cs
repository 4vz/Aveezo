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

    public Shell Shell { get; }

    private readonly StringBuilder temporaryLine = new StringBuilder();

    public bool IsQueueStop { get; private set; } = false;

    #endregion

    #region Constructors

    public UnixSsh(uint shellColumns, uint shellRows, uint shellWidth, uint shellHeight) : base(shellColumns, shellRows, shellWidth, shellHeight)
    {
        // Ssh events
        Connected += UnixSsh_Connected;
        DataAvailable += UnixSsh_DataAvailable;
        Disconnected += UnixSsh_Disconnected;
        ConnectionFail += UnixSsh_ConnectionFail;

        // Create new internal request as root request for SSH.
        Shell = new Shell(Shell_DataWrite);

        // In events
        Idle += Shell.OnIdle;        
    }


    #endregion

    #region Methods

    private void Shell_DataWrite(object sender, SshSendDataEventArgs e)
    {
        if (e.NewLine)
            WriteLine(e.Data);
        else
            Write(e.Data);
    }

    public async Task<string[]> Request2(string request)
    {
        throw new NotImplementedException();
    }

    public async Task QueueStop()
    {
        if (!IsQueueStop)
        {
            IsQueueStop = true;
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
    }

    private void UnixSsh_DataAvailable(object sender, EventArgs e)
    {
        if (IsQueueStop)
        {
            Stop();
            return;
        }

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

        // Data received event
        Shell.OnDataReceived(this, new SshReceivedDataEventArgs(data, lines.ToArray(), temporaryLine.ToString()));
    }

    private void UnixSsh_Idle(object sender, EventArgs e)
    {
    }

    private void UnixSsh_Disconnected(object sender, EventArgs e)
    {
        Shell.OnDisconnected(this, EventArgs.Empty);
    }

    #endregion
}


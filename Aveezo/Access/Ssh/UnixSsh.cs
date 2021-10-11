using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Messages.Transport;

namespace Aveezo
{
    public class UnixSsh : Ssh
    {
        #region Consts

        private const string dateCommand = "date --iso-8601=seconds";

        #endregion

        #region Fields

        private StringBuilder lineBuilder = new StringBuilder();

        public string Prompt { get; private set; } = null;

        public TimeSpan TimeSpanOffset { get; private set; } = TimeSpan.MaxValue;

        public DateTime DateTime => sessionCheckStage > 2 ? DateTime.Now - TimeSpanOffset : DateTime.Now;

        public bool IsPromptReady { get; private set; } = false;

        public bool IsQueueStop { get; private set; } = false;

        private bool requesting = false;

        private bool requestResultCommand = false;

        public List<string> requestResult = new List<string>();

        private string lastLine = null;

        public event EventHandler<UnixSshPromptChangedEventArgs> PromptChanged;

        public event EventHandler<UnixSshPromptReadyEventArgs> PromptReady;

        public event EventHandler<SshDataEventArgs> DataReceived;

        private int sessionCheckStage = -1;

        private bool firstPromptReady = false;

        #endregion

        #region Constructors

        public UnixSsh() : base()
        {
            Connected += UnixSsh_Connected;
            DataAvailable += UnixSsh_DataAvailable;
            Idle += UnixSsh_Idle;
            Disconnected += UnixSsh_Disconnected;
            ConnectionFail += UnixSsh_ConnectionFail;
        }

        #endregion

        #region Methods

        public async Task<string[]> Request(string request)
        {
            while (!IsPromptReady || requesting) await Task.Delay(10);

            if (IsPromptReady && requesting == false)
            {
                requestResult.Clear();
                requesting = true;
                requestResultCommand = false;

                WriteLine(request);
            }

            while (requesting) await Task.Delay(50);

            return requestResult.ToArray();
        }

        public void Check()
        {
            if (IsPromptReady || (sessionCheckStage == -1 && lineBuilder.Length > 0))
            {
                sessionCheckStage = 1;
                Write(dateCommand);
            }
        }

        public async Task QueueStop()
        {
            if (!IsQueueStop)
            {
                IsQueueStop = true;

                if (IsPromptReady)
                    await Stop();
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
            lineBuilder.Clear();
           
            lastLine = null;

            IsPromptReady = false;
            sessionCheckStage = -1;
        }

        private async void UnixSsh_DataAvailable(object sender, EventArgs e)
        {
            if (IsQueueStop && Prompt == null)
            {
                Stop();
                return;
            }

            IsPromptReady = false;

            string data = Stream.Read();

            string[] values = data.Split(Collections.NewLine);
            var lines = new List<string>();

            lineBuilder.Append(values[0]);

            if (values.Length >= 2)
            {
                lines.Add(lineBuilder.ToString());
                for (var valuesIndex = 1; valuesIndex < (values.Length - 1); valuesIndex++)
                    lines.Add(values[valuesIndex]);

                lineBuilder.Clear();
                lineBuilder.Append(values[^1]);
            }

            if (lines.Count > 0) lastLine = lines[^1];
            string currentLineBuilder = lineBuilder.ToString();

            var endWithPrompt = (Prompt != null) && currentLineBuilder.EndsWith(Prompt);

            var dataEventArgs = new SshDataEventArgs(data, lines.ToArray());
            await OnDataReceived(dataEventArgs);
            DataReceived?.Invoke(this, dataEventArgs);

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
                        await OnPromptChanged(promptChangedEventArgs);
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
                        await OnPromptReady(promptReadyEventArgs);
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
            IsPromptReady = false;
            firstPromptReady = false;
            TimeSpanOffset = TimeSpan.MaxValue;

            requesting = false;
            requestResultCommand = false;
            requestResult.Clear();
        }

        #endregion

        #region Virtuals

        protected virtual async Task OnPromptChanged(UnixSshPromptChangedEventArgs e) { }

        protected virtual async Task OnPromptReady(UnixSshPromptReadyEventArgs e) { }

        protected virtual async Task OnDataReceived(SshDataEventArgs e) { }

        #endregion
    }

    
}

using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aveezo
{
    public class Ssh
    {
        #region Fields

        private Task main = null;

        public string Host { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public bool IsStarted { get; private set; } = false;

        private SshClient client = null;

        public bool IsConnected => client != null && client.IsConnected;

        private bool beingDisconnected = false;

        protected bool IsReconnect { get; set; } = false;

        public int ReconnectDelay { get; set; } = 5000;

        public DateTime LastDataTimeStamp { get; private set; } = DateTime.Now;

        public ShellStream Stream { get; private set; }

        public bool IsStreamAvailable => Stream != null;

        public event EventHandler BeforeConnect;

        public event EventHandler Connecting;

        public event EventHandler<SshConnectionFailEventArgs> ConnectionFail;

        public event EventHandler Connected;

        public event EventHandler DataAvailable;

        public event EventHandler Idle;

        public event EventHandler Disconnecting;

        public event EventHandler Disconnected;

        public event EventHandler<SshReconnectingEventArgs> Reconnecting;

        private AutoResetEvent mainLoopWait = new AutoResetEvent(false);

        #endregion

        #region Constructors

        public Ssh()
        {

        }

        #endregion

        #region Methods

        private async Task Main()
        {
            IsReconnect = true;

            while (true)
            {
                try
                {
                    if (client != null)
                    {
                        client.Dispose();
                    }

                    await OnBeforeConnect();
                    BeforeConnect?.Invoke(this, new EventArgs());

                    client = new SshClient(Host, User, Password);

                    await OnConnecting();
                    Connecting?.Invoke(this, new EventArgs());

                    client.Connect();
                }
                catch (Exception ex)
                {
                    var connectionFailArgs = new SshConnectionFailEventArgs(ex.Message switch
                    {
                        "No such host is known" => SshConnectionFailReason.HostUnknown,
                        "A socket operation was attempted to an unreachable host." => SshConnectionFailReason.HostUnreachable,
                        string b when b.IndexOf("connected party did not properly") > -1 => SshConnectionFailReason.TimeOut,
                        "Permission denied (password)." => SshConnectionFailReason.AuthenticationFailed,
                        _ => SshConnectionFailReason.Unknown
                    }, ex.Message);
                    await OnConnectionFail(connectionFailArgs);
                    ConnectionFail?.Invoke(this, connectionFailArgs);
                }

                if (IsConnected)
                {
                    Stream = client.CreateShellStream("", 80, 40, 80, 40, 1024);
                   
                    //client.Session.ChannelCloseReceived += SessionCloseReceived;
                    //client.Session.ChannelDataReceived += SessionDataReceived;

                    await OnConnected();
                    Connected?.Invoke(this, new EventArgs());

                    LastDataTimeStamp = DateTime.Now;
                    beingDisconnected = false;

                    var alreadyReceivingData = false;

                    while (IsConnected)
                    {
                        mainLoopWait.WaitOne(1000);
                        
                        if (beingDisconnected)
                        {
                            break;
                        }
                        else
                        {
                            if (Stream.DataAvailable)
                            {
                                LastDataTimeStamp = DateTime.Now;
                                alreadyReceivingData = true;

                                await OnDataAvailable();
                                DataAvailable?.Invoke(this, new EventArgs());
                            }
                            else if (alreadyReceivingData)
                            {
                                await OnIdle();
                                Idle?.Invoke(this, new EventArgs());
                            }
                        }
                    }

                    await OnDisconnecting();
                    Disconnecting?.Invoke(this, new EventArgs());
                    

                    //if (client.Session != null)
                    //{
                    //    client.Session.ChannelCloseReceived -= SessionCloseReceived;
                    //    client.Session.ChannelDataReceived -= SessionDataReceived;
                    //}

                    Stream.Dispose();
                    Stream = null;

                    await OnDisconnected();
                    Disconnected?.Invoke(this, new EventArgs());
                }

                if (IsReconnect)
                {
                    var reconnectingArgs = new SshReconnectingEventArgs { Reconnect = true };

                    await OnReconnecting(reconnectingArgs);
                    if (reconnectingArgs.Reconnect)
                    {
                        Reconnecting?.Invoke(this, reconnectingArgs);

                        if (reconnectingArgs.Reconnect)
                            await Task.Delay(ReconnectDelay);
                        else
                            break;
                    }
                    else
                        break;
                }
                else
                    break;
            }

            IsStarted = false;
        }

        private void SessionDataReceived(object sender, MessageEventArgs<Renci.SshNet.Messages.Connection.ChannelDataMessage> e)
        {
            mainLoopWait.Set();
        }

        private void SessionCloseReceived(object sender, MessageEventArgs<Renci.SshNet.Messages.Connection.ChannelCloseMessage> e)
        {
            beingDisconnected = true;
            mainLoopWait.Set();
        }

        public async Task Start()
        {
            if (!IsStarted)
            {
                IsStarted = true;

                main = Task.Run(Main);
            }
        }

        public async Task Start(string host, string user, string password)
        {
            Host = host;
            User = user;
            Password = password;

            Start();
        }

        public async Task Stop()
        {
            if (IsStarted)
            {
                if (IsConnected)
                {
                    IsReconnect = false;
                    client.Disconnect();
                }
                else
                {
                    IsStarted = false;
                }
            }
        }

        public void Write(char data) => Write($"{data}");

        public void Write(string data)
        {
            if (IsStreamAvailable)
            {
                Stream.Write(data);
            }
        }

        public void WriteLine(string data)
        {
            if (IsStreamAvailable)
            {
                Stream.WriteLine(data);
            }
        }

        #endregion

        #region Virtuals

        protected virtual async Task OnBeforeConnect() { }

        protected virtual async Task OnConnecting() { }

        protected virtual async Task OnConnectionFail(SshConnectionFailEventArgs e) { }

        protected virtual async Task OnConnected() { }

        protected virtual async Task OnDataAvailable() { }

        protected virtual async Task OnIdle() { }

        protected virtual async Task OnDisconnecting() { }

        protected virtual async Task OnDisconnected() { }

        protected virtual async Task OnReconnecting(SshReconnectingEventArgs e) { }

        #endregion

    }
}

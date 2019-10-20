using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Messages.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Aveezo
{
    public class Ssh
    {
        #region Fields

        private SshClient client = null;

        private Thread mainThread = null;

        private ShellStream stream = null;

        public bool IsStarted { get; private set; } = false;
        public bool IsConnected => client != null ? client.IsConnected : false;

        public bool IsReconnect { get; set; } = true;
        public int ReconnectDelay { get; set; } = 5000;

        public event ConnectionFailedEventHandler ConnectionFailed;

        public string TerminalPrompt { get; private set; } = null;
        
        #endregion

        #region Constructors

        public Ssh()
        {

        }

        #endregion

        #region Methods
        public void Start(string host, string user, string password)
        {
            if (!IsStarted)
            {
                IsStarted = true;

                mainThread = new Thread(new ThreadStart(delegate ()
                {
                    client = new SshClient(host, user, password);

                    while (true)
                    {
                        ConnectionFailReason reason = ConnectionFailReason.None;

                        try
                        {
                            client.Connect();
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == "No such host is known")
                            {
                                reason = ConnectionFailReason.HostUnknown;
                            }
                            else if (ex.Message.IndexOf("connected party did not properly") > -1)
                            {
                                reason = ConnectionFailReason.TimeOut;
                            }
                            else if (ex.Message == "Permission denied (password).")
                            {
                                reason = ConnectionFailReason.AuthenticationFailed;
                            }
                            else
                            {
                                reason = ConnectionFailReason.Unknown;
                            }
                        }

                        if (IsConnected)
                        {
                            stream = client.CreateShellStream("", 80, 40, 80, 40, 1024);

                            while (IsConnected)
                            {
                                if (stream.DataAvailable)
                                {
                                    string data = stream.Read();
                                    Console.Write(string.Join("-", data.ToCharArray().ToInt()));
                                }
                            }

                            Console.WriteLine("Disconnected");

                            break;
                        }
                        else
                        {
                            ConnectionFailed?.Invoke(reason);

                            if (IsReconnect)
                            {
                                Thread.Sleep(ReconnectDelay);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    client = null;
                    stream = null;
                }));

                mainThread.Start();
            }
        }

        public void Send(string command)
        {
            if (IsConnected)
            {
                stream.Write(command + "\n");
            }
        }


        #endregion
    }
    
    public delegate void ConnectionFailedEventHandler(ConnectionFailReason reason);

    //public delegate void Connection
}

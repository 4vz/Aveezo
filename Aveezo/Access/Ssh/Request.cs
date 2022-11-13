using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

public class Request
{
    #region Fields

    public string Prompt { get; private set; } = null;

    private string request = null;

    private bool initted = false;

    private UnixSsh Ssh;

    public event EventHandler<string> Closed;

    #endregion

    #region Constructors

    public Request(UnixSsh ssh, string request)
    {
        Ssh = ssh;
        Ssh.DataReceived += DataReceived;
        this.request = request;
    }

    #endregion

    #region Methods

    public void Init()
    {
        if (!initted)
        {
            initted = true;
            Ssh.WriteLine(request);
        }
    }

    private void DataReceived(object sender, SshDataEventArgs e)
    {
         
    }

    #endregion

    #region Statics

    #endregion
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

public static class EventHandlerExtensions
{
    /// <summary>
    /// IfNotNull the EventHandler with no event data.
    /// </summary>
    public static void Invoke(this EventHandler handler, object sender)
    {
        handler?.Invoke(sender, EventArgs.Empty);
    }
}

using Renci.SshNet.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public abstract partial class App
    {
        #region Fields

        private string lastEventMessage = null;

        private string lastEventLabel = null;

        private int lastEventRepeat = 0;

        private object eventSync = new object();

        private int errorCounter = 0;

        public int ErrorCounter => errorCounter;

        #endregion

        #region Methods

        // events

        public Task Event(string message) => Event(message.Array(), null);

        public Task Event(string[] messages) => Event(messages, null);

        public Task Event(string message, string label) => Event(message.Array(), label);

        public async Task Event(string[] messages, string label)
        {
            if (messages == null) return;

            foreach (var message in messages)
            {
                if (message != null)
                {
                    lock (eventSync)
                    {
                        lastEventRepeat = (message == lastEventMessage && label == lastEventLabel) ? (lastEventRepeat + 1) : 0;
                        lastEventMessage = message;
                        lastEventLabel = label;

                        string eventMessage = $"{DateTime.UtcNow:yyyy/MM/dd:HH:mm:ss.fff}|{label ?? ""}|{message}";

                        OnEvent(eventMessage, lastEventRepeat);
                    }
                }
            }
        }

        // errors

        public Task Error(string message) => Error(message.Array(), null);

        public Task Error(string[] messages) => Error(messages, null);

        public Task Error(string message, string label) => Error(message.Array(), label);

        public async Task Error(string[] messages, string label)
        {
            if (messages != null) errorCounter += messages.Length;

            Event(messages, $"ERROR{(label != null ? $"#{label}" : "")}");
        }

        // exception errors

        public Task Error(Exception exception) => Error(exception?.Message);

        public Task Error(Exception exception, string label) => Error(exception?.Message, label);

        #endregion
    }
}

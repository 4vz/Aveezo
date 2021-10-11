using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public abstract partial class App
    {
        #region Fields

        private string lastEventMessage = null;
        private string lastEventLabel = null;
        private string lastEventSubLabel = null;

        private int lastEventRepeat = 0;

        private object eventSync = new object();

        #endregion

        #region Methods

        public void Event(string[] messages)
        {
            if (messages == null) return;

            foreach (var message in messages)
            {
                Event(message, null, null);
            }
        }

        public void Event(string message) => Event(message, null, null);

        public void Event(string[] messages, string label)
        {
            if (messages == null) return;

            foreach (var message in messages)
            {
                Event(message, label, null);
            }
        }

        public void Event(string message, string label) => Event(message, label, null);

        public void Event(string[] messages, string label, string subLabel)
        {
            if (messages == null) return;

            foreach (var message in messages)
            {
                Event(message, label, subLabel);
            }
        }

        public void Event(string message, string label, string subLabel)
        {
            if (message != null)
            {
                lock (eventSync)
                {
                    lastEventRepeat = (message == lastEventMessage && label == lastEventLabel && subLabel == lastEventSubLabel) ? (lastEventRepeat + 1) : 0;
                    lastEventMessage = message;
                    lastEventLabel = label;
                    lastEventSubLabel = subLabel;

                    string eventMessage = $"{DateTime.UtcNow:yyyy/MM/dd:HH:mm:ss.fff}|{(label != null ? $"{label}{(subLabel != null ? $">{subLabel}" : "")}" : "")}|{message}";

                    OnEvent(eventMessage, lastEventRepeat);
                }
            }
        }

        public void Error(string[] messages, string label)
        {
            if (messages == null) return;

            foreach (var message in messages)
            {
                Event(message, label, "ERROR");
            }
        }

        public void Error(string message, string label) => Event(message, label, "ERROR");

        public void Error(Exception exception, string label)
        {
            if (exception == null) return;

            Error(exception.Message, label);
        }

        #endregion
    }
}

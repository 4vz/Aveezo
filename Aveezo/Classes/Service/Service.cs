
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class ServiceOptions
    {
        #region Fields

        public int Delay { get; set; } = 1000;

        public bool DelayFirst { get; set; } = false;

        #endregion
    }

    public class Service
    {
        #region Fields

        private Task loopTask;

        private Func<Task> onLoop;

        private Predicate onAlive = null;

        private Func<Task> onInit;

        private Func<string, Task> onEvent;

        public bool IsStarted { get; private set; }
         
        #endregion

        #region Constructors

        public Service()
        {
        }

        #endregion

        #region Methods

        public void OnLoop(Func<Task> c) => onLoop = c;

        public void OnAlive(Predicate c) => onAlive = c;

        public void OnInit(Func<Task> c) => onInit = c;

        public void OnEvent(Func<string, Task> c) => onEvent = c;

        public async void Start(Action<ServiceOptions> options = null)
        {
            if (onLoop == null) throw new InvalidOperationException("OnLoop is required");
            if (onAlive == null) throw new InvalidOperationException("OnAlive is required");

            var serviceOptions = new ServiceOptions();

            options?.Invoke(serviceOptions);

            if (!IsStarted)
            {
                IsStarted = true;

                await onInit?.Invoke();

                if (!serviceOptions.DelayFirst)
                {
                    await onLoop();
                }

                loopTask = Task.Run(async () =>
                {
                    await Task.Delay(serviceOptions.Delay);

                    while (onAlive())
                    {
                        await onLoop();
                        await Task.Delay(serviceOptions.Delay);
                    }
                });
            }
        }

        public void Stop()
        {
            if (IsStarted)
            {
                loopTask.Wait();

                IsStarted = false;
            }
        }

        public void Event(string message)
        {
            onEvent?.Invoke(message);
        }

        #endregion
    }
}

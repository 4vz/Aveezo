using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class Service
    {
        #region Fields

        public bool IsStarted { get; private set; }

        private Task loopTask;

        public int Delay { get; set; }

        public event EventHandler Loop;

        public event EventHandler<ServiceAliveEventArgs> Alive;

        public event EventHandler<ServiceEventEventArgs> EventReceived;

        #endregion

        #region Constructors

        public Service(int delay)
        {
            Delay = delay;
        }

        #endregion

        #region Statics

        public static implicit operator Service(int delay)
        {
            return new Service(delay);
        }

        #endregion

        #region Methods

        public void Start() => Start(false);

        public void Start(bool delayFirst)
        {
            if (!IsStarted)
            {
                IsStarted = true;

                OnStarted();

                if (!delayFirst)
                    LoopProcess();
                loopTask = Task.Run(LoopTask);
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

        private async Task LoopTask()
        {
            await Task.Delay(Delay);

            while (AliveProcess())
            {
                await LoopProcess();
                await Task.Delay(Delay);
            }

            Console.WriteLine("ENDED");
        }

        private async Task LoopProcess()
        {
            await OnLoop();
            Loop?.Invoke(this, new EventArgs());
        }

        private bool AliveProcess()
        {
            var overrideReturn = OnAlive();

            var serviceAliveEventArgs = new ServiceAliveEventArgs();
            Alive?.Invoke(this, serviceAliveEventArgs);       
            
            var returnValue = overrideReturn || serviceAliveEventArgs.Alive;

            return returnValue;
        }

        public void Event(string message)
        {
            var arg = new ServiceEventEventArgs(message);

            EventReceived?.Invoke(this, arg);
            OnEvent(arg);
        }

        #endregion

        #region Virtuals

        protected virtual bool OnAlive() => false;

        protected virtual async Task OnLoop() { }

        protected virtual void OnStarted() { }

        protected virtual void OnEvent(ServiceEventEventArgs e) { }

        #endregion
    }
}

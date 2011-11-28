using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace KinectBrowser.Interaction
{
    public class InteractionsCore
    {
        private Thread coreLoopThread;
        private TimeSpan updateDuration;

        private static object sync = new object();

        public static object Sync
        {
            get { return sync; }
        }

        static InteractionsCore core = null;

        public static InteractionsCore Core
        {
            get
            {
                if (core == null)
                    throw new InvalidOperationException("Initialize doit être appelé avant tout accès la InteractionsCore.Core");

                return core;
            }
        }

        public static void Initialize()
        {
            Initialize(TimeSpan.FromSeconds(1 / 30.0));
        }

        public static void Initialize(TimeSpan updateDuration)
        {
            lock (sync)
            {
                if (core != null)
                    throw new InvalidOperationException("Core a déjà été initialisé");
                else
                    core = new InteractionsCore(updateDuration);
            }
        }

        private InteractionsCore(TimeSpan updateDuration)
        {
            this.updateDuration = updateDuration;

            coreLoopThread = new Thread(CoreLoopThreadFunction);
            coreLoopThread.IsBackground = true;
            coreLoopThread.Name = "Interactions Core Thread";
            coreLoopThread.SetApartmentState(ApartmentState.STA);
            coreLoopThread.Start();
        }

        public event EventHandler Loop;

        private bool stopped = false;

        Stopwatch coreLoopWatch = new Stopwatch();
        private void CoreLoopThreadFunction()
        {
            coreLoopWatch.Restart();

            while (!stopped)
            {
                if (coreLoopWatch.Elapsed > updateDuration)
                {
                    LoopExec();

                    coreLoopWatch.Restart();
                }
                /* CPU Saving */
                Thread.Sleep(1);
            }
        }

        private void LoopExec()
        {
            if (Loop != null)
                Loop(this, EventArgs.Empty);
        }

        public void Stop()
        {
            stopped = true;
            Thread.Sleep(10);
        }
    }
}

namespace Bloxstrap
{
    internal static class MultiInstanceWatcher
    {
        private static int GetOpenProcessesCount()
        {
            const string LOG_IDENT = "MultiInstanceWatcher::GetOpenProcessesCount";

            try
            {
                // prevent any possible race conditions by checking for Froststrap processes too
                int count = Process.GetProcesses().Count(x => x.ProcessName is "RobloxPlayerBeta" or "Froststrap");
                count -= 1; // ignore the current process
                return count;
            }
            catch (Exception ex)
            {
                // everything process related can error at any time
                App.Logger.WriteException(LOG_IDENT, ex);
                return -1;
            }
        }

        private static void FireInitialisedEvent()
        {
            using EventWaitHandle initEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "Bloxstrap-MultiInstanceWatcherInitialisationFinished");
            initEventHandle.Set();
        }

        public static void Run()
        {
            const string LOG_IDENT = "MultiInstanceWatcher::Run";

            // Try to get both mutexes for better compatibility
            bool acquiredMutex1;
            bool acquiredMutex2;

            using Mutex mutex1 = new Mutex(false, "ROBLOX_singletonMutex");
            using Mutex mutex2 = new Mutex(false, "ROBLOX_singletonEvent");

            try
            {
                acquiredMutex1 = mutex1.WaitOne(0);
                acquiredMutex2 = mutex2.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                acquiredMutex1 = true;
                acquiredMutex2 = false;
            }

            if (!acquiredMutex1 && !acquiredMutex2)
            {
                App.Logger.WriteLine(LOG_IDENT, "Client singleton mutexes are already acquired");
                FireInitialisedEvent();
                return;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Acquired singleton mutexes! Mutex1: {acquiredMutex1}, Mutex2: {acquiredMutex2}");
            FireInitialisedEvent();

            // watch for alive processes
            int count;
            do
            {
                Thread.Sleep(5000);
                count = GetOpenProcessesCount();
            }
            while (count == -1 || count > 0); // redo if -1 (one of the Process apis failed)

            App.Logger.WriteLine(LOG_IDENT, "All Roblox related processes have closed, exiting!");
        }
    }
}
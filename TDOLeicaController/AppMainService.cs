using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TDOLeicaController
{
    public class AppMainService
    {
        //Fields and Properties------------------------------------------------------------------------------------------------//
        
        public bool BackgroundTaskRunning { get; private set; }
        
        public delegate void ProgressEventHandler(object sender, AppProgressEventArgs args);
        public event ProgressEventHandler BackgroundProgress;
        public event ProgressEventHandler BackgroundCancelled;
        
        //-------------------------------------------------------

        private AppSettings appSettings;
        private AppUtilities appUtilities;
        private AppBackgroundTask appBackgroundTask;
        
        private CancellationTokenSource cTokenSource;
        private int bckTaskErrorCount;

        //Constructors---------------------------------------------------------------------------------------------------------//
        public AppMainService(AppSettings appSettings, AppUtilities appUtilities, AppBackgroundTask appBackgroundTask)
        {
            this.appSettings = appSettings;
            this.appUtilities = appUtilities;
            this.appBackgroundTask = appBackgroundTask;
            
            this.BackgroundTaskRunning = false;
        }

        //Methods--------------------------------------------------------------------------------------------------------------//

        //public method to start service
        public void StartBackgroundService()
        {
            if (!BackgroundTaskRunning)
            {
                cTokenSource = new CancellationTokenSource();
                backgroundServiceAsync();
            }
        }

        //public method to stop FilesScanningAsync()
        public void StopBackgroundService()
        {
            if (cTokenSource != null && !cTokenSource.IsCancellationRequested) { cTokenSource.Cancel(); }
        }
        

        //Helpers--------------------------------------------------------------------------------------------------------------//
        #region Helpers
      
        //main async method running infinite loop to scan files
        protected virtual Task backgroundServiceAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    appBackgroundTask.InitializeTask();
                }
                catch (Exception exception)
                {
                    BackgroundTaskRunning = false;
                    onBackgroundCancelled("Error initializing task: " + exception.GetBaseException().Message, 2);
                    return;
                }

                onBackgroundProgress("Background service started", 2);
                BackgroundTaskRunning = true;
                bckTaskErrorCount = 0;

                while (!cTokenSource.Token.IsCancellationRequested)
                {
                    var timeStamp = DateTime.Now;

                    bool isTimeToKeepAlive = (appSettings.LogKeepAliveIntervalSeconds == 0) ? false :
                        (timeStamp.Hour * 3600000 + timeStamp.Minute * 60000 + timeStamp.Second * 1000 + timeStamp.Millisecond) 
                            % appSettings.LogKeepAliveIntervalSeconds * 1000 == 5;
                    if (isTimeToKeepAlive) { onBackgroundProgress("This is Keep Alive log entry", 10);}

                    try
                    {
                        var returnMessage = appBackgroundTask.RunTask(timeStamp);
                        if (!String.IsNullOrEmpty(returnMessage))
                        {
                            onBackgroundProgress("Task Info: " + returnMessage, 1);
                        } 
                    }
                    catch (Exception exception)
                    {
                        bckTaskErrorCount += 1;
                        onBackgroundProgress(String.Format("Task error #{0}: {1}", bckTaskErrorCount,
                            exception.GetBaseException().Message), 2);
                        if (appSettings.BckTaskMaxAllowedErrors != 0 &&
                            bckTaskErrorCount > appSettings.BckTaskMaxAllowedErrors &&
                            !cTokenSource.IsCancellationRequested)
                        {
                            onBackgroundProgress(bckTaskErrorCount + " task error(s) encountered. Stopping....", 2);
                            cTokenSource.Cancel();
                        } 
                    }
                    
                    Thread.Sleep(appSettings.MainLoopIntervalmSec);
                }

                try
                {
                    appBackgroundTask.FinalizeTask();
                }
                catch (Exception exception)
                {
                    onBackgroundProgress("Error finalizing task: " + exception.GetBaseException().Message, 2);
                }
                BackgroundTaskRunning = false;
                onBackgroundCancelled("");
            });
        }

        // trigger ScanProgress event and write to log
        protected virtual void onBackgroundProgress(string progressMessage, int messageCode = 0)
        {
            if (messageCode >= appSettings.LoggingLevel) { appUtilities.WriteToLog(progressMessage); }
            if (BackgroundProgress != null) { BackgroundProgress(this, new AppProgressEventArgs(progressMessage, messageCode));}
        }

        // trigger ScanProgress event, trigger ScanCancelled event and write to log
        protected virtual void onBackgroundCancelled(string cancelMessage, int messageCode = 2)
        {
            cancelMessage = (!String.IsNullOrEmpty(cancelMessage)) ? cancelMessage : "Background service stopped";
            if (messageCode >= appSettings.LoggingLevel){ appUtilities.WriteToLog(cancelMessage); } 
            if (BackgroundCancelled != null) { BackgroundCancelled(this, new AppProgressEventArgs(cancelMessage, messageCode)); }
        }
        
        #endregion
                
    }
}


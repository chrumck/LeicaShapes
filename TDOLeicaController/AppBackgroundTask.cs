using System;
using System.IO.Ports;

namespace TDOLeicaController
{
    public class AppBackgroundTask
    {
        //Fields and Properties------------------------------------------------------------------------------------------------//
        
        
        //-------------------------------------------------------

        private AppSettings appSettings;
        private AppUtilities appUtilities;
        private SerialPort comPort; 

        private int jobRow;
        private DateTime nextCommandDT;
        private int pendingCommandsNo;

        //Constructors---------------------------------------------------------------------------------------------------------//
        public AppBackgroundTask(AppSettings appSettings, AppUtilities appUtilities)
        {
            this.appSettings = appSettings;
            this.appUtilities = appUtilities;

            jobRow = 0;
            nextCommandDT = DateTime.Now;
            pendingCommandsNo = 0;
        }

        //Methods--------------------------------------------------------------------------------------------------------------//

        //RunTask
        public string RunTask(DateTime timeStamp)
        {
            if (nextCommandDT.CompareTo(timeStamp) > 0) { return "next command not due. Waiting...."; }
            
            if (comPort == null || !comPort.IsOpen) { comOpen(); }

            var commandParams = appSettings.LeicaJob[jobRow].Split(new[] {','});

            if (commandParams[0] == "goto".ToLower() ) 
            {
                processGoTo(timeStamp, commandParams);
                return "going to line " + commandParams[1];
            }

            if (pendingCommandsNo > appSettings.MaxPendingCommands)
            {

            }

            comPort.Write(commandParams[0]);

                       
            
            
            return String.Empty;
        }

        //Finalize
        public void FinalizeTask()
        {
            if (comPort != null)
            {
                comPort.Dispose();
                comPort = null;
            }
        }

        
       

        //Helpers--------------------------------------------------------------------------------------------------------------//
        #region Helpers

        //comOpen
        protected virtual void comOpen()
        {
            if (comPort == null)
            {
                this.comPort = new SerialPort(appSettings.ComPort, appSettings.ComBaudRate, appSettings.ComParity,
                    appSettings.ComDataBits, appSettings.ComStopBits);
            }
            if (comPort != null && !comPort.IsOpen)
            {
                comPort.Open();
            }
        }

        //processGoTo
        protected virtual void processGoTo(DateTime timeStamp, string[] commandParams)
        {
            nextCommandDT = timeStamp;
            int addMiliseconds;
            if (commandParams.Length > 2 && int.TryParse(commandParams[2], out addMiliseconds))
            {
                nextCommandDT.AddMilliseconds(int.Parse(commandParams[2]));
            }
            jobRow = int.Parse(commandParams[1]);
        }
        

        #endregion
                
    }
}


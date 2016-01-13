using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace TDOLeicaController
{
    public class AppBackgroundTask
    {
        //Fields and Properties------------------------------------------------------------------------------------------------//
        
        
        //-------------------------------------------------------

        private AppSettings appSettings;
        private AppUtilities appUtilities;
        private IAppPort appPort; 

        public int jobRow {get; private set;}
        public DateTime nextCommandDT { get; private set; }
        public int pendingCommandsNo { get; private set; }
        
        private List<string> commandResponses;

        //Constructors---------------------------------------------------------------------------------------------------------//
        public AppBackgroundTask(AppSettings appSettings, AppUtilities appUtilities, IAppPort appPort)
        {
            this.appSettings = appSettings;
            this.appUtilities = appUtilities;
            this.appPort = appPort;

            jobRow = 0;
            nextCommandDT = DateTime.Now;
            pendingCommandsNo = 0;
            commandResponses = new List<string>();
        }

        //Methods--------------------------------------------------------------------------------------------------------------//

        //RunTask
        public string RunTask(DateTime timeStamp)
        {
            if (!appPort.IsOpen) { appPort.Open(); }
            
            checkResponse();

            if (pendingCommandsNo > appSettings.MaxPendingCommands)
            {
                throw new InvalidOperationException(String.Format("Pending commands count of {0} exceeds the limit of {1}",
                    pendingCommandsNo, appSettings.MaxPendingCommands));
            }

            if (jobRow >= appSettings.LeicaJob.Length) { return String.Empty; }

            if (nextCommandDT.CompareTo(timeStamp) > 0) { return String.Empty; }
            nextCommandDT = timeStamp;
            
            var commandParams = appSettings.LeicaJob[jobRow].Split(new[] {';'});
            if (commandParams.Length < 2 || commandParams[0] == String.Empty)
            {
                jobRow += 1;
                throw new ArgumentException(String.Format(
                    "The command line at row {0} seems to have malformed syntax or job empty.", jobRow - 1));
            }

            if (commandParams[0].ToLower() == "goto".ToLower() ) 
            {
                processGoTo(timeStamp, commandParams);
                return "Going to line " + jobRow;
            }

            processCommand(commandParams);
            return String.Format("Sent command {0}, next command due {1:HH:mm:ss.FFF}", commandParams[0], nextCommandDT);
        }


        //FinalizeTask
        public void FinalizeTask()
        {
            appPort.WriteLine(appSettings.ComFinalCommand);
            appPort.Close();
            jobRow = 0;
            nextCommandDT = DateTime.Now;
            pendingCommandsNo = 0;
            commandResponses = new List<string>();
        }
               

        //Helpers--------------------------------------------------------------------------------------------------------------//
        #region Helpers

        //processGoTo
        void processGoTo(DateTime timeStamp, string[] commandParams)
        {
            int addMiliseconds;
            if (commandParams.Length > 2 && int.TryParse(commandParams[2], out addMiliseconds))
            {
                nextCommandDT = nextCommandDT.AddMilliseconds(int.Parse(commandParams[2]));
            }
            var newJobRow = int.Parse(commandParams[1]);
            if (newJobRow >= appSettings.LeicaJob.Length)
            {
                throw new InvalidOperationException(String.Format("GoTo command jumps to line {0} which does not exist in the job.", newJobRow));
            }
            jobRow = newJobRow;
        }

        //processCommand
        void processCommand(string[] commandParams)
        {
            appPort.WriteLine(commandParams[0]);
            commandResponses.Add(commandParams[1]);
            jobRow += 1;
            pendingCommandsNo += 1;

            int addMiliseconds;
            if (commandParams.Length > 2 && int.TryParse(commandParams[2], out addMiliseconds))
            {
                nextCommandDT = nextCommandDT.AddMilliseconds(int.Parse(commandParams[2]));
            }
        }

        //checkForPortResponse
        void checkResponse()
        {
            if (commandResponses.Count == 0) { return; }

            string response;
            try
            {
                response = appPort.ReadLine();
            }
            catch (TimeoutException)
            {
                return;
            }
            
            var expectedResponse = commandResponses[0].Trim();
            commandResponses.RemoveAt(0);

            if (!response.ToLower().Contains(expectedResponse.ToLower()))
            {
                throw new InvalidOperationException(String.Format(
                    "The response {0} did not match the expected one: {1}", response, expectedResponse));
            }

            pendingCommandsNo -= 1;
            checkResponse();
        }
        

        #endregion
                
    }
}


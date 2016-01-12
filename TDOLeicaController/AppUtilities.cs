using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TDOLeicaController
{
    public class AppUtilities
    {
        //Fields and Properties------------------------------------------------------------------------------------------------//

        private AppSettings appSettings;

        //Constructors---------------------------------------------------------------------------------------------------------//
        public AppUtilities(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        //Methods--------------------------------------------------------------------------------------------------------------//

        //write log entry to the file GroutItToGwLog.txt
        public void WriteToLog(string logEntry)
        {
            try
            {
                File.AppendAllText("appLog.txt", String.Format("\n{0:yyyy-MM-dd HH:mm:ss} : {1}", DateTime.Now, logEntry));
            }
            catch { }
        }
    }
}

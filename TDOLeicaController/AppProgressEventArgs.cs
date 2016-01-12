using System;

namespace TDOLeicaController
{
    public class AppProgressEventArgs :EventArgs
    {
        public string ProgressMessage;
        public int MessageCode;

        public AppProgressEventArgs(string progressMessage, int messageCode )
        {
            this.ProgressMessage = progressMessage;
            this.MessageCode = messageCode;
        }
    }
}

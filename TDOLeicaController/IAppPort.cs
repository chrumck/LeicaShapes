using System.IO.Ports;

namespace TDOLeicaController
{
    public interface IAppPort
    {
        //Fields and Properties------------------------------------------------------------------------------------------------//

        bool IsOpen {get;}

        //Constructors---------------------------------------------------------------------------------------------------------//


        //Methods--------------------------------------------------------------------------------------------------------------//

        void Open(); 
        void Close();
        void WriteLine(string text);
        string ReadLine();
        string ReadExisting();
    }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO.Ports;

namespace TDOLeicaController.UnitTests
{
    [TestClass]
    public class Test_AppBackgroundTask
    {
        [TestMethod]
        public void AppBackgroundTask_opensComWithNewParameters()
        {
            var mockSerialPort = new Mock<SerialPort>();
            mockSerialPort.Setup(x => x.Open());
        }
    }
}

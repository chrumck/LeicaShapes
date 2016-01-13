using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO.Ports;

namespace TDOLeicaController.UnitTests
{
    [TestClass]
    public class Test_AppBackgroundTask
    {
        //Arrange - common for the test class -------------------------------------------------------------------------
        AppSettings appSettings;
        AppUtilities appUtilities;
        Mock<IAppPort> mockPort;
        AppBackgroundTask appBackgroundTask;
        DateTime timeStamp;

        public Test_AppBackgroundTask()
        {
            appSettings = new AppSettings();
            appSettings.LeicaJob = new[] { "dummyCommand; dummyResponse" };
            appUtilities = new AppUtilities(appSettings);
            mockPort = new Mock<IAppPort>();
            mockPort.Setup(x => x.IsOpen).Returns(true);
            mockPort.Setup(x => x.Open());
            mockPort.Setup(x => x.Close());
            mockPort.Setup(x => x.WriteLine(It.IsAny<string>()));
            mockPort.Setup(x => x.ReadLine()).Returns("dummyResponse");
            
            appBackgroundTask = new AppBackgroundTask(appSettings, appUtilities, mockPort.Object);

            timeStamp = DateTime.Now.AddDays(1);
        }
        
        //-------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void AppBackgroundTask_opensPortIfNotOpen()
        {
            //Arrange
            mockPort.Setup(x => x.IsOpen).Returns(false);

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            mockPort.Verify(x => x.IsOpen, Times.Once);
            mockPort.Verify(x => x.Open(), Times.Once);
        }

        [TestMethod]
        public void AppBackgroundTask_doesNotOpenIfOpen()
        {
            //Arrange

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            mockPort.Verify(x => x.IsOpen, Times.Once);
            mockPort.Verify(x => x.Open(), Times.Never);
        }

        [TestMethod]
        public void AppBackgroundTask_doesNotCheckResponseIfResponseCount0()
        {
            //Arrange

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            mockPort.Verify(x => x.ReadLine(), Times.Never);
        }
        
        [TestMethod]
        public void AppBackgroundTask_skipsCycleIfCommandNotDue()
        {
            //Arrange
            timeStamp = timeStamp.AddDays(-10);

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            Assert.IsTrue(taskResult.Contains(String.Empty));
        }

        [TestMethod]
        public void AppBackgroundTask_updatesNextCommandDTToTimseStamp()
        {
            //Arrange

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            Assert.IsTrue(appBackgroundTask.nextCommandDT.CompareTo(timeStamp) == 0);
        }

        [TestMethod]
        public void AppBackgroundTask_goesToLineOnGoTo()
        {
            //Arrange
            appSettings.LeicaJob = new[] { "goTO; 1", "dummyLine1" };
            
            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            Assert.IsTrue(taskResult.Contains("Going to line"));
            Assert.IsTrue(appBackgroundTask.jobRow == 1);
        }

        [TestMethod]
        public void AppBackgroundTask_throwsIfLineToHigh()
        {
            //Arrange
            appSettings.LeicaJob = new[] { "goTO; 2", "dummyLine1" };
            
            //Act
            string taskResult;
            try
            {
                taskResult = appBackgroundTask.RunTask(timeStamp);
            }
            catch (InvalidOperationException exception)
            {
                //Assert
                Assert.IsTrue(exception.Message.Contains("GoTo command jumps to line 2"));
                Assert.IsTrue(appBackgroundTask.jobRow == 0);
                return;
            }
            //Assert
            Assert.IsFalse(true);
        }

        [TestMethod]
        public void AppBackgroundTask_goesToLineAndAddsTimeToNextCommandDT()
        {
            //Arrange
            appSettings.LeicaJob = new[] { "goTO; 1; 100", "dummyLine1" };

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            Assert.IsTrue(taskResult.Contains("Going to line"));
            Assert.IsTrue(timeStamp.AddMilliseconds(100).Equals(appBackgroundTask.nextCommandDT));
        }

        [TestMethod]
        public void AppBackgroundTask_SendsCommandToWriteLine()
        {
            //Arrange

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            mockPort.Verify(x => x.WriteLine("dummyCommand"), Times.Once);
            Assert.IsTrue(taskResult.Contains("Sent command dummyCommand"));
        }

        [TestMethod]
        public void AppBackgroundTask_IncrementsJobRowBy1()
        {
            //Arrange

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            Assert.IsTrue(appBackgroundTask.jobRow == 1);
        }

        [TestMethod]
        public void AppBackgroundTask_IncrementsPendingCommandsBy1()
        {
            //Arrange

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            Assert.IsTrue(appBackgroundTask.pendingCommandsNo == 1);
        }

         [TestMethod]
        public void AppBackgroundTask_SendsCommandAndAndAddsTimeToNextCommandDT()
        {
            //Arrange
            appSettings.LeicaJob = new[] { "dummyCommand; dummyResponse; 100" };

            //Act
            var taskResult = appBackgroundTask.RunTask(timeStamp);

            //Assert
            Assert.IsTrue(timeStamp.AddMilliseconds(100).Equals(appBackgroundTask.nextCommandDT));
        }

         [TestMethod]
         public void AppBackgroundTask_CheckResponseSkipsIfReadLineTimesOut()
         {
             //Arrange
             mockPort.Setup(x => x.ReadLine()).Throws(new TimeoutException());

             //Act
             var taskResult = appBackgroundTask.RunTask(timeStamp);
             var taskResult2 = appBackgroundTask.RunTask(timeStamp);

             //Assert
             mockPort.Verify(x => x.WriteLine("dummyCommand"), Times.Once);
             Assert.IsTrue(taskResult.Contains("Sent command dummyCommand"));
             mockPort.Verify(x => x.ReadLine(), Times.Once);
         }

         [TestMethod]
         public void AppBackgroundTask_skipsCycleIfJobFinished()
         {
             //Arrange
             mockPort.Setup(x => x.ReadLine()).Throws(new TimeoutException());

             //Act
             var taskResult = appBackgroundTask.RunTask(timeStamp);
             var taskResult2 = appBackgroundTask.RunTask(timeStamp);

             //Assert
             Assert.IsTrue(taskResult2.Contains(String.Empty));
         }

         [TestMethod]
         public void AppBackgroundTask_CheckResponseDecrementsPendingCommandsBy1()
         {
             //Arrange

             //Act
             var taskResult = appBackgroundTask.RunTask(timeStamp);
             var taskResult2 = appBackgroundTask.RunTask(timeStamp);

             //Assert
             Assert.IsTrue(appBackgroundTask.pendingCommandsNo == 0);
         }

         [TestMethod]
         public void AppBackgroundTask_CheckResponseThrowsIfResponsesDoNotMatch()
         {
             //Arrange
             mockPort.Setup(x => x.ReadLine()).Returns("dummyRes....");

             //Act
             string taskResult, taskResult2;
             try
             {
                 taskResult = appBackgroundTask.RunTask(timeStamp);
                 taskResult2 = appBackgroundTask.RunTask(timeStamp);
             }
             catch (InvalidOperationException exception)
             {
                 //Assert
                 Assert.IsTrue(exception.Message.Contains("The response dummyRes.... did not match the expected one"));
                 Assert.IsTrue(appBackgroundTask.pendingCommandsNo == 1);
                 return;
             }
             //Assert
             Assert.IsFalse(true);
             
         }

         [TestMethod]
         public void AppBackgroundTask_CheckResponseInvokesRecursive()
         {
             //Arrange
             appSettings.LeicaJob = new[] { "dummyCommand; dummyResponse", "dummyCommand2; dummyResponse2" };
             var callCounter = 0;
             mockPort.Setup(x => x.ReadLine()).Returns(() => 
             {
                 callCounter += 1;
                 if (callCounter == 1) { throw new TimeoutException(); }
                 if (callCounter == 2) { return "dummyResponse"; }
                 if (callCounter == 3) { return "dummyResponse2"; }
                 throw new TimeoutException();
             });

             //Act
             var taskResult = appBackgroundTask.RunTask(timeStamp);
             var taskResult2 = appBackgroundTask.RunTask(timeStamp);
             var taskResult3 = appBackgroundTask.RunTask(timeStamp);

             //Assert
             Assert.IsTrue(appBackgroundTask.pendingCommandsNo == 0);
         }

         [TestMethod]
         public void AppBackgroundTask_CheckResponseInvokesRecursiveLeaveCheckForNextRun()
         {
             //Arrange
             appSettings.LeicaJob = new[] { "dummyCommand; dummyResponse", "dummyCommand2; dummyResponse2" };
             var callCounter = 0;
             mockPort.Setup(x => x.ReadLine()).Returns(() =>
             {
                 callCounter += 1;
                 if (callCounter == 1) { throw new TimeoutException(); }
                 if (callCounter == 2) { return "dummyResponse"; }
                 if (callCounter == 3) { throw new TimeoutException(); }
                 throw new TimeoutException();
             });

             //Act
             var taskResult = appBackgroundTask.RunTask(timeStamp);
             var taskResult2 = appBackgroundTask.RunTask(timeStamp);
             var taskResult3 = appBackgroundTask.RunTask(timeStamp);

             //Assert
             Assert.IsTrue(appBackgroundTask.pendingCommandsNo == 1);
         }
                
    }
}

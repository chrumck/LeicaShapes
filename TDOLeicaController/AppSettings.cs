using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace TDOLeicaController
{
    public class AppSettings
    {
        //Fields and Properties------------------------------------------------------------------------------------------------//

        public string ComPort { get; set; }
        public int ComBaudRate { get; set; }
        public int ComDataBits { get; set; }
        public StopBits ComStopBits { get; set; }
        public Parity ComParity { get; set; }
        public Handshake ComHandshake { get; set; }

        private int maxPendingCommands;
        public int MaxPendingCommands
        {
            get { return this.maxPendingCommands; }
            set { this.maxPendingCommands = (value >= 1 && value <= 30) ? value : this.maxPendingCommands; }
        }
        private int mainLoopIntervalmSec;
        public int MainLoopIntervalmSec
        {
            get { return this.mainLoopIntervalmSec; }
            set { this.mainLoopIntervalmSec = (value >= 1 && value <= 60000) ? value : this.mainLoopIntervalmSec; }
        }

        private int loggingLevel;
        public int LoggingLevel
        {
            get { return this.loggingLevel; }
            set { this.loggingLevel = (value >= 0 && value <= 10) ? value : this.loggingLevel; }
        }

        private int logKeepAliveIntervalSeconds;
        public int LogKeepAliveIntervalSeconds
        {
            get { return this.logKeepAliveIntervalSeconds; }
            set { this.logKeepAliveIntervalSeconds = (value >= 0 && value <= 14400) ? value : logKeepAliveIntervalSeconds; }
        }

        private int bckTaskMaxAllowedErrors;
        public int BckTaskMaxAllowedErrors
        {
            get { return this.bckTaskMaxAllowedErrors; }
            set { this.bckTaskMaxAllowedErrors = (value >= 0 && value <= 1000) ? value : bckTaskMaxAllowedErrors; }
        }

        public string[] LeicaJob { get; set; }
        
        //Constructors---------------------------------------------------------------------------------------------------------//

        public AppSettings()
        {
            ComPort = "COM1";
            ComBaudRate = 19200;
            ComDataBits = 8;
            ComStopBits = StopBits.One;
            ComParity = Parity.None;
            ComHandshake = Handshake.None;
            MaxPendingCommands = 5;
            MainLoopIntervalmSec = 10;
            LoggingLevel = 2;
            LogKeepAliveIntervalSeconds = 3600;
            BckTaskMaxAllowedErrors = 10;
            LeicaJob = new string[] {};
        }
        
        //Methods--------------------------------------------------------------------------------------------------------------//

        //ReadFromXML - overload for default settings file name
        public void ReadFromXML()
        {
            ReadFromXML("AppSettings.xml");
        }

        //ReadFromXML
        public void ReadFromXML(string fileName)
        {
            var settingsFile = new XmlDocument();
            if (!File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + fileName)) 
                { throw new ArgumentException("Settings file '" + fileName + "' does not exist."); }
            settingsFile.Load(fileName);
            var settingsMainNode = settingsFile.SelectSingleNode("AppSettings");
            if (settingsMainNode == null)
                { throw new ArgumentException(fileName + " is empty of file format not correct. Settings not loaded."); }

            var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var childNode in settingsMainNode.Cast<XmlNode>())
            {
                setPropertyFromXmlNodeHelper(childNode, properties);
            } 
        }

        //LoadLeicaJob
        public void LoadLeicaJob()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                LeicaJob = File.ReadAllLines(openFileDialog.FileName);
            }
        }

        //Helpers--------------------------------------------------------------------------------------------------------------//
        #region Helpers

        //savePropertyFromChildNode
        private void setPropertyFromXmlNodeHelper(XmlNode node, PropertyInfo[] properties)
        {
            var property = properties.FirstOrDefault(x => x.Name == node.Name);
            if (property != null && !String.IsNullOrEmpty(node.InnerText))
            {
                if (property.PropertyType == typeof(decimal))
                {
                    property.SetValue(this, decimal.Parse(node.InnerText), null);
                    return;
                }
                if (property.PropertyType == typeof(String))
                {
                    property.SetValue(this, node.InnerText, null);
                    return;
                }
                property.SetValue(this, int.Parse(node.InnerText), null);
            }
        } 

        #endregion


        
    }
}

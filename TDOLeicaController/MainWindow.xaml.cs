using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TDOLeicaController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Fields and Properties------------------------------------------------------------------------------------------------//

        public bool FormClosePending { get; private set; }

        private AppSettings appSettings;
        private AppUtilities appUtilities;
        private AppBackgroundTask appBackgroundTask;
        private AppMainService appMainService;
        private AppComPort appPort;


        //Constructors---------------------------------------------------------------------------------------------------------//

        public MainWindow()
        {
            this.FormClosePending = false;

            appSettings = new AppSettings();
            appUtilities = new AppUtilities(appSettings);
            appPort = new AppComPort();
            appPort.ReadTimeout = 5;
            appBackgroundTask = new AppBackgroundTask(appSettings, appUtilities, appPort);
            appMainService = new AppMainService(appSettings, appUtilities, appBackgroundTask);

            InitializeComponent();
            readAppSettingsFromXml();

            appMainService.BackgroundProgress += updateOnScanProgress;
            appMainService.BackgroundCancelled += updateOnScanCancelled;
        }

        //Methods--------------------------------------------------------------------------------------------------------------//

        //event delegate BtnSettings_Click
        private void BtnSettings_Click(object sender, EventArgs e)
        {
            if (readAppSettingsFromXml())
            {
                MessageBox.Show("Settings reloaded.\nSettings can be changed in 'AppSettings.xml' file in directory:\n " +
                    System.AppDomain.CurrentDomain.BaseDirectory, "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        //BtnJob_Click
        private void BtnJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                appSettings.LoadLeicaJob();
            }
            catch (Exception exception)
            {
                MessageBox.Show("Error loading job: " + exception.GetBaseException().Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        //BtnStartStop_Click
        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!appMainService.BackgroundTaskRunning)
            {
                BtnStartStop.Content = "STOP";
                readAppSettingsFromXml();
                BtnSettings.IsEnabled = false;
                BtnJob.IsEnabled = false;
                appMainService.StartBackgroundService();
                return;
            }
            BtnStartStop.Content = "Wait..";
            BtnStartStop.IsEnabled = false;
            appMainService.StopBackgroundService();
        }

        //event delegate to subscibe to appMainService.ScanProgress event
        private void updateOnScanProgress(object sender, AppProgressEventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + args.ProgressMessage;
            });
        }

        //event delegate to subscibe to appMainService.ScanCancelled event
        private void updateOnScanCancelled(object sender, AppProgressEventArgs args)
        {
            Dispatcher.Invoke(() => 
            {
                TxtStatus.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":" + args.ProgressMessage;
                BtnSettings.IsEnabled = true;
                BtnJob.IsEnabled = true;
                BtnStartStop.IsEnabled = true;
                BtnStartStop.Content = "START";
                if (FormClosePending) { this.Close(); }
            });
        }

        //MainWindow_Closing
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (appMainService.BackgroundTaskRunning)
            {
                this.IsEnabled = false;
                FormClosePending = true;
                e.Cancel = true;
                BtnStartStop.Content = "Wait..";
                appMainService.StopBackgroundService();
            }
        }

        //Helpers--------------------------------------------------------------------------------------------------------------//
        #region Helpers

        //readAppSettings from xmlFile, show message box is exception thrown
        private bool readAppSettingsFromXml()
        {
            var settingsLoaded = false;
            try
            {
                appSettings.ReadFromXML();

                appPort.PortName = appSettings.ComPortName;
                appPort.BaudRate = appSettings.ComBaudRate;
                appPort.Parity = appSettings.ComParity;
                appPort.DataBits = appSettings.ComDataBits;
                appPort.StopBits = appSettings.ComStopBits;
                appPort.NewLine = appSettings.ComNewLine;

                settingsLoaded = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Error loading settings: " + exception.GetBaseException().Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            return settingsLoaded;
        }

        #endregion
                
    }
}

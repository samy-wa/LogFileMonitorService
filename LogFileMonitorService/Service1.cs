using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Configuration.Install;

namespace LogFileMonitorService
{
    public partial class Service1 : ServiceBase
    {
        private FileSystemWatcher watcher;
        private StringBuilder logChanges;
        private Timer batchTimer = new Timer();

        private string targetFile = @"D:\TEMP\Logs\file.txt"; // Set your target file path here
        private string copyFile = @"D:\TEMP\Logs\copyFile.txt"; // Set your target file path here
        private string logFilePath = @"D:\TEMP\Logs\output\log.txt"; // Set your output log file path here
        private string lastHash;
        public Service1()
        {
            InitializeComponent();
            this.ServiceName = "LogFileMonitorService";
            logChanges = new StringBuilder();
            batchTimer = new Timer(15000); // 15 seconds
            batchTimer.Elapsed += BatchTimer_Elapsed;
        }

        protected override void OnStart(string[] args)
        {
            if (File.Exists(targetFile))
            {
                watcher = new FileSystemWatcher();
                watcher.Path = Path.GetDirectoryName(targetFile);
                watcher.Filter = Path.GetFileName(targetFile);
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += OnChanged;
                watcher.EnableRaisingEvents = true;

                logChanges.Clear();
                batchTimer.Start();
                WriteLog("Monitoring started...");
            }
            else
            {
                WriteLog("Target file does not exist. Please check the file path.");
                Stop();
            }
        }

        protected override void OnStop()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                batchTimer.Stop();
                WriteLog("Monitoring stopped.");
            }
        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            string currentHash = CalculateFileHash(targetFile);
            lastHash = CalculateFileHash(copyFile);
            if (lastHash != currentHash)
            {
                if (!File.Exists(copyFile))
                    File.Create(copyFile);

                File.WriteAllText(copyFile, targetFile);
                lastHash = currentHash;
                string fileContent = File.ReadAllText(logFilePath);
                WriteLog($"{DateTime.Now}: File changed.\n{logFilePath}");
            }
        }

        private void BatchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (logChanges.Length > 0)
            {
                WriteLog(logChanges.ToString());
                logChanges.Clear();
            }
        }
        private string CalculateFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        private void WriteLog(string message)
        {
            if (!File.Exists(logFilePath))
                File.Create(logFilePath);
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine(message);
            }
        }
    }

    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.ServiceName = "LogFileMonitorService";
            serviceInstaller.DisplayName = "File Hash Monitor Service";
            serviceInstaller.Description = "Monitors a file for changes and reports changes every 15 seconds.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }


}

using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Linq;

class GuardianWatchdog
{
    private const string PRIMARY_WATCHDOG_PATH = @"C:\Harbinger Watermark\WatermarkWatchdog\bin\Release\net8.0\WatermarkWatchdog.exe";
    private const string PRIMARY_WATCHDOG_NAME = "WatermarkWatchdog";
    private const int CHECK_INTERVAL = 1000; // Check every 1 seconds
    
    static void Main()
    {
        // Ensure only one guardian runs
        bool createdNew;
        using var mutex = new Mutex(true, "HarbingerGuardianWatchdogMutex", out createdNew);
        
        if (!createdNew)
        {
            // Another guardian is already running
            return;
        }
        
        string logPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "guardian.log"
        );
        
        WriteLog(logPath, "Guardian Watchdog started - protecting primary watchdog");
        
        if (!File.Exists(PRIMARY_WATCHDOG_PATH))
        {
            WriteLog(logPath, $"ERROR: Primary watchdog not found at {PRIMARY_WATCHDOG_PATH}");
            return;
        }
        
        // Start primary watchdog immediately
        StartPrimaryWatchdog(logPath);
        
        // Keep monitoring forever
        while (true)
        {
            Thread.Sleep(CHECK_INTERVAL);
            CheckAndRestartPrimaryWatchdog(logPath);
        }
    }
    
    static void StartPrimaryWatchdog(string logPath)
    {
        try
        {
            var existing = Process.GetProcessesByName(PRIMARY_WATCHDOG_NAME);
            if (existing.Length > 0)
            {
                WriteLog(logPath, "Primary watchdog already running");
                return;
            }
            
            var process = new Process();
            process.StartInfo.FileName = PRIMARY_WATCHDOG_PATH;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            
            WriteLog(logPath, "Primary watchdog started successfully");
        }
        catch (Exception ex)
        {
            WriteLog(logPath, $"ERROR starting primary watchdog: {ex.Message}");
        }
    }
    
    static void CheckAndRestartPrimaryWatchdog(string logPath)
    {
        var running = Process.GetProcessesByName(PRIMARY_WATCHDOG_NAME);
        
        if (running.Length == 0)
        {
            WriteLog(logPath, "PRIMARY WATCHDOG KILLED! Restarting in 2 seconds...");
            Thread.Sleep(2000); // Brief delay so you see it was killed
            StartPrimaryWatchdog(logPath);
        }
    }
    
    static void WriteLog(string logPath, string message)
    {
        try
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(logPath, logMessage + Environment.NewLine);
        }
        catch { }
    }
}
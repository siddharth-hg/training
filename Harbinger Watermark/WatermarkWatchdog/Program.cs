using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Linq;

class WatermarkWatchdog
{
    private const string WATERMARK_PATH = @"C:\Harbinger Watermark\bin\Debug\net8.0-windows\HarbingerWatermarkOverlay.exe";
    private const string WATERMARK_PROCESS_NAME = "HarbingerWatermarkOverlay";
    private const string GUARDIAN_PATH = @"C:\Harbinger Watermark\GuardianWatchdog\bin\Release\net8.0\GuardianWatchdog.exe";
    private const string GUARDIAN_PROCESS_NAME = "GuardianWatchdog";
    private const int CHECK_INTERVAL = 1000;
    
    static void Main()
    {
        // Ensure only one primary watchdog runs
        bool createdNew;
        using var mutex = new Mutex(true, "HarbingerWatermarkWatchdogMutex", out createdNew);
        
        if (!createdNew)
        {
            return;
        }
        
        string logPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "watchdog.log"
        );

        WriteLog(logPath, "Primary Watchdog started");

        if (!File.Exists(WATERMARK_PATH))
        {
            WriteLog(logPath, $"ERROR: Watermark not found at {WATERMARK_PATH}");
            return;
        }

        // Start watermark immediately
        StartWatermark(logPath);
        
        // Start guardian if not running
        EnsureGuardianRunning(logPath);

        // Monitor loop
        while (true)
        {
            Thread.Sleep(CHECK_INTERVAL);
            CheckAndRestartWatermark(logPath);
            EnsureGuardianRunning(logPath); // Also check guardian
        }
    }

    static void StartWatermark(string logPath)
    {
        try
        {
            var existing = Process.GetProcessesByName(WATERMARK_PROCESS_NAME);
            if (existing.Length > 0)
            {
                WriteLog(logPath, "Watermark already running");
                return;
            }
            
            var process = new Process();
            process.StartInfo.FileName = WATERMARK_PATH;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            
            WriteLog(logPath, "Watermark started successfully");
        }
        catch (Exception ex)
        {
            WriteLog(logPath, $"ERROR starting watermark: {ex.Message}");
        }
    }

    static void CheckAndRestartWatermark(string logPath)
    {
        var running = Process.GetProcessesByName(WATERMARK_PROCESS_NAME);
        
        if (running.Length == 0)
        {
            WriteLog(logPath, "Watermark stopped! Restarting...");
            StartWatermark(logPath);
        }
        else if (running.Length > 1)
        {
            WriteLog(logPath, $"{running.Length} watermarks detected, killing extras");
            var sorted = running.OrderBy(p => {
                try { return p.StartTime; }
                catch { return DateTime.MaxValue; }
            }).ToArray();
            
            for (int i = 1; i < sorted.Length; i++)
            {
                try
                {
                    sorted[i].Kill();
                }
                catch { }
            }
        }
    }
    
    static void EnsureGuardianRunning(string logPath)
    {
        try
        {
            if (!File.Exists(GUARDIAN_PATH))
                return;
                
            var running = Process.GetProcessesByName(GUARDIAN_PROCESS_NAME);
            if (running.Length == 0)
            {
                WriteLog(logPath, "Guardian not running, starting it...");
                var process = new Process();
                process.StartInfo.FileName = GUARDIAN_PATH;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
            }
        }
        catch (Exception ex)
        {
            WriteLog(logPath, $"Error with guardian: {ex.Message}");
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
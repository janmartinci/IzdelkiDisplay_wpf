using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DisplayApp
{
    public static class ErrorLogger
    {
        private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DisplayApp");
        private static readonly string LogDir = Path.Combine(AppDataDir, "logs");
        private static readonly string CrashMarkerFile = Path.Combine(AppDataDir, "crash.marker");
        private static readonly object Sync = new object();

        public static void Initialize()
        {
            try
            {
                Directory.CreateDirectory(LogDir);

                Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
                TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
                Application.Current.Exit += (s, e) => ClearCrashMarker();

                // If we find a crash marker from previous run, notify user (optional)
                if (File.Exists(CrashMarkerFile))
                {
                    string lastLog = Directory.GetFiles(LogDir, "*.log").OrderByDescending(f => f).FirstOrDefault();
                    string message = "The application detected an unexpected shutdown in the previous run.";
                    if (!string.IsNullOrEmpty(lastLog))
                        message += Environment.NewLine + $"Last log: {lastLog}";
                    MessageBox.Show(message, "Previous Crash Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch
            {
                // Avoid throwing from logger initialization
            }
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                LogException(e.Exception, "DispatcherUnhandledException");
                SafeCreateCrashMarker();
            }
            catch { }
            // Let runtime decide whether to terminate. Do not forcibly swallow exceptions here.
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = e.ExceptionObject as Exception ?? new Exception("Unhandled non-Exception object thrown");
                LogException(ex, "AppDomain.UnhandledException");
                SafeCreateCrashMarker();
            }
            catch { }
        }

        private static void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
                SafeCreateCrashMarker();
            }
            catch { }
        }

        public static void LogException(Exception ex, string source = null)
        {
            if (ex == null) return;

            lock (Sync)
            {
                try
                {
                    string file = Path.Combine(LogDir, $"crash_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.log");
                    var sb = new StringBuilder();
                    sb.AppendLine($"TimeUtc: {DateTime.UtcNow:o}");
                    sb.AppendLine($"Source: {source}");
                    sb.AppendLine($"Message: {ex.Message}");
                    sb.AppendLine($"Type: {ex.GetType().FullName}");
                    sb.AppendLine($"StackTrace: {ex.StackTrace}");
                    sb.AppendLine();
                    sb.AppendLine("ToString:");
                    sb.AppendLine(ex.ToString());
                    sb.AppendLine();
                    sb.AppendLine("Environment:");
                    sb.AppendLine($"OSVersion: {Environment.OSVersion}");
                    sb.AppendLine($"MachineName: {Environment.MachineName}");
                    sb.AppendLine($"CurrentDirectory: {Environment.CurrentDirectory}");
                    sb.AppendLine($"AppDomain: {AppDomain.CurrentDomain.FriendlyName}");
                    sb.AppendLine();
                    sb.AppendLine("Loaded assemblies:");
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.GetName().Name))
                    {
                        try { sb.AppendLine($" - {asm.GetName().Name} ({asm.GetName().Version})"); } catch { }
                    }

                    File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // swallow to avoid throwing from logger
                }
            }
        }

        private static void SafeCreateCrashMarker()
        {
            try
            {
                Directory.CreateDirectory(AppDataDir);
                File.WriteAllText(CrashMarkerFile, DateTime.UtcNow.ToString("o"));
            }
            catch { }
        }

        public static void ClearCrashMarker()
        {
            try
            {
                if (File.Exists(CrashMarkerFile))
                    File.Delete(CrashMarkerFile);
            }
            catch { }
        }
    }
}
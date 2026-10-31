using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;

namespace BannerlordPlusPlus
{
    internal static class CrashLogger
    {
        private static readonly object SyncRoot = new object();
        private static string _logDirectory;

        public static void Initialize()
        {
            try
            {
                string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(assemblyDirectory))
                    throw new InvalidOperationException("The mod assembly directory could not be determined.");

                _logDirectory = Path.Combine(assemblyDirectory, "CrashLogs");
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                Directory.CreateDirectory(_logDirectory);
            }
            catch (IOException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash logger initialization failed: {0}", exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash logger initialization failed: {0}", exception);
            }
            catch (SecurityException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash logger initialization failed: {0}", exception);
            }
            catch (ArgumentException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash logger initialization failed: {0}", exception);
            }
            catch (NotSupportedException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash logger initialization failed: {0}", exception);
            }
            catch (InvalidOperationException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash logger initialization failed: {0}", exception);
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            string report = CreateReport(args.ExceptionObject, args.IsTerminating);

            try
            {
                WriteReport(report);
            }
            catch (IOException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash report could not be written: {0}", exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash report could not be written: {0}", exception);
            }
            catch (SecurityException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash report could not be written: {0}", exception);
            }
            catch (ArgumentException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash report could not be written: {0}", exception);
            }
            catch (NotSupportedException exception)
            {
                Trace.TraceError("BannerlordPlusPlus crash report could not be written: {0}", exception);
            }
        }

        private static string CreateReport(object exceptionObject, bool isTerminating)
        {
            var report = new StringBuilder();
            Assembly assembly = Assembly.GetExecutingAssembly();
            report.AppendLine("BannerlordPlusPlus crash report");
            report.AppendLine($"Time (local): {DateTime.Now:O}");
            report.AppendLine($"Time (UTC): {DateTime.UtcNow:O}");
            using (Process process = Process.GetCurrentProcess())
            {
                report.AppendLine($"Process: {process.ProcessName} (PID {process.Id})");
            }

            report.AppendLine($"Managed thread ID: {Thread.CurrentThread.ManagedThreadId}");
            report.AppendLine($"Process terminating: {isTerminating}");
            report.AppendLine($"Mod version: {assembly.GetName().Version}");
            report.AppendLine($"Mod assembly: {assembly.Location}");
            report.AppendLine($"Game directory: {AppDomain.CurrentDomain.BaseDirectory}");
            report.AppendLine($"Working directory: {Environment.CurrentDirectory}");
            report.AppendLine($"OS: {Environment.OSVersion}");
            report.AppendLine($".NET runtime: {Environment.Version}");
            report.AppendLine();
            report.AppendLine("Unhandled exception:");
            report.AppendLine(exceptionObject == null
                ? "<null exception object>"
                : exceptionObject is Exception exception
                    ? exception.ToString()
                    : exceptionObject.ToString());

            return report.ToString();
        }

        private static void WriteReport(string report)
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(_logDirectory);
                string fileName = $"Crash_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.txt";
                string path = Path.Combine(_logDirectory, fileName);

                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(report);
                    writer.Flush();
                    stream.Flush(true);
                }
            }
        }
    }
}

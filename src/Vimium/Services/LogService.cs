using System;
using System.IO;

namespace Vimium.Services;

/// <summary>
/// Simple file-based debug logger. Writes timestamped messages to a
/// rotating log in %APPDATA%\Vimium\debug.log (max ~1 MB, then rolls).
/// All writes are synchronous and atomic — no buffering, no async,
/// so messages survive a crash.
/// </summary>
public static class LogService
{
    private static readonly string LogDir;
    private static readonly string LogPath;
    private static readonly object _lock = new();
    private const long MaxSize = 1_000_000; // 1 MB rollover

    static LogService()
    {
        LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Vimium");
        LogPath = Path.Combine(LogDir, "debug.log");
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Error(string message, Exception ex) => Write("ERROR", $"{message}: {ex.GetType().Name}: {ex.Message}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(LogDir);
                RollIfNeeded();
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(LogPath, $"{timestamp} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Never let logging itself crash the app
        }
    }

    private static void RollIfNeeded()
    {
        try
        {
            var info = new FileInfo(LogPath);
            if (info.Exists && info.Length > MaxSize)
            {
                var backup = LogPath + ".prev";
                if (File.Exists(backup))
                    File.Delete(backup);
                File.Move(LogPath, backup);
            }
        }
        catch
        {
            // If roll fails, just keep writing to the same file
        }
    }
}

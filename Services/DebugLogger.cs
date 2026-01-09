using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Blish_HUD;
using Blish_HUD.Input;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Services
{
    public class DebugLogger
    {
        private static readonly Logger Logger = Logger.GetLogger<DebugLogger>();
        private const string DEBUG_FOLDER = @"C:\git\Maestro\Debug";

        private readonly StringBuilder _log = new StringBuilder();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private long _lastEventMs;
        private bool _enabled;
        private string _songName;

        [Conditional("DEBUG")]
        public void Start(string songName)
        {
            _log.Clear();
            _songName = songName;
            _enabled = true;
            _lastEventMs = 0;
            _stopwatch.Restart();
        }

        [Conditional("DEBUG")]
        public void Stop()
        {
            _stopwatch.Stop();
            _enabled = false;
            if (_log.Length == 0) return;

            try
            {
                if (!Directory.Exists(DEBUG_FOLDER))
                    Directory.CreateDirectory(DEBUG_FOLDER);

                var safeFileName = SanitizeFileName(_songName);
                var logPath = Path.Combine(DEBUG_FOLDER, $"{safeFileName}.txt");

                var header = $"=== Debug Log for: {_songName} ===\n" +
                             $"=== Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n";
                File.WriteAllText(logPath, header + _log);
                Logger.Info($"Debug log written to: {logPath}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to write debug log");
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder(name);
            foreach (var c in invalid)
                sanitized.Replace(c, '_');
            return sanitized.ToString();
        }

        [Conditional("DEBUG")]
        public void Log(string message)
        {
            if (_enabled)
                _log.AppendLine(message);
        }

        [Conditional("DEBUG")]
        public void LogNote(Keys key, Keys targetKey)
        {
            if (!_enabled) return;

            var currentMs = _stopwatch.ElapsedMilliseconds;
            var deltaMs = currentMs - _lastEventMs;
            _lastEventMs = currentMs;

            _log.AppendLine($"[+{deltaMs,4}ms] NOTE: {key} -> {FormatKey(targetKey)}");
        }

        [Conditional("DEBUG")]
        public void LogSharp(Keys key, KeyBinding binding)
        {
            if (!_enabled) return;

            var currentMs = _stopwatch.ElapsedMilliseconds;
            var deltaMs = currentMs - _lastEventMs;
            _lastEventMs = currentMs;

            _log.AppendLine($"[+{deltaMs,4}ms] SHARP: {key} -> {FormatModifiers(binding.ModifierKeys)}+{FormatKey(binding.PrimaryKey)}");
        }

        [Conditional("DEBUG")]
        public void LogDelay(int delayMs)
        {
            if (_enabled)
                _log.AppendLine($"         DELAY: {delayMs}ms");
        }

        private static string FormatKey(Keys key)
        {
            var name = key.ToString();
            if (name.StartsWith("D") && name.Length == 2 && char.IsDigit(name[1]))
                return name[1].ToString();
            if (name.StartsWith("NumPad"))
                return "Num" + name.Substring(6);
            return name;
        }

        private static string FormatModifiers(ModifierKeys mods)
        {
            var parts = new List<string>();
            if (mods.HasFlag(ModifierKeys.Ctrl)) parts.Add("Ctrl");
            if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            return parts.Count > 0 ? string.Join("+", parts) : "";
        }
    }
}

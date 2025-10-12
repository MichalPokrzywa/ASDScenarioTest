using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

public static class Praat
{
    public static string praatPath = $"{Application.dataPath}/Praat/Praat.exe";
    public static string praatScriptPath = $"{Application.dataPath}/Praat/extract_voice_features.praat";
    public static bool RunPraatScript(string wavPath, string outTxt)
    {
        if (!File.Exists(wavPath))
        {
            UnityEngine.Debug.LogError($"Input WAV not found: {wavPath}");
            return false;
        }

        var psi = new ProcessStartInfo()
        {
            FileName = praatPath, 
            Arguments = $"--run \"{praatScriptPath}\" \"{wavPath}\" \"{outTxt}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using (var p = new Process { StartInfo = psi, EnableRaisingEvents = true })
        {
            p.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            p.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            try
            {
                p.Start();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to start Praat process: {ex}");
                return false;
            }

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            var outStr = stdout.ToString().Trim();
            var errStr = stderr.ToString().Trim();

            if (p.ExitCode != 0)
            {
                UnityEngine.Debug.LogError($"Praat failed (exit {p.ExitCode}). stderr: {errStr}\nstdout: {outStr}");
                return false;
            }

            UnityEngine.Debug.Log($"Praat finished successfully. stdout: {outStr}");
            return true;
        }
    }
}

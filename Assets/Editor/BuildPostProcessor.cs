#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildPostProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        string src = Path.Combine(Application.dataPath, "Praat"); // Assets/Praat
        if (!Directory.Exists(src))
        {
            Debug.LogWarning($"BuildPostProcessor: source folder not found: {src}");
            return;
        }

        string buildExePath = report.summary.outputPath; // path to .exe
        string buildRoot = Path.GetDirectoryName(buildExePath);
        string exeNameNoExt = Path.GetFileNameWithoutExtension(buildExePath);
        string dataFolderName = exeNameNoExt + "_Data";
        string dataFolderPath = Path.Combine(buildRoot, dataFolderName);

        // 1) Copy to build root (next to exe). Some apps expect helpers next to the executable.
        try
        {
            string destRoot = Path.Combine(buildRoot, "Praat");
            if (Directory.Exists(destRoot)) Directory.Delete(destRoot, true);
            FileUtil.CopyFileOrDirectory(src, destRoot);
            Debug.Log($"BuildPostProcessor: copied {src} -> {destRoot}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BuildPostProcessor: failed copying to build root: {e}");
        }

        // 2) Copy into the Data folder (create it if missing)
        try
        {
            if (!Directory.Exists(dataFolderPath))
                Directory.CreateDirectory(dataFolderPath);

            string destInData = Path.Combine(dataFolderPath, "Praat");
            if (Directory.Exists(destInData)) Directory.Delete(destInData, true);
            FileUtil.CopyFileOrDirectory(src, destInData);
            Debug.Log($"BuildPostProcessor: copied {src} -> {destInData}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BuildPostProcessor: failed copying into Data folder: {e}");
        }
    }
}
#endif

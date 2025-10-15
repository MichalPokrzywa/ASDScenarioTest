using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildPostProcessor : IPostprocessBuildWithReport
{
    // order if multiple callbacks exist
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        // source folder inside the project (change as needed)
        string src = Path.Combine(Application.dataPath, "Praat"); // Assets/ExtraFiles

        if (!Directory.Exists(src))
        {
            Debug.LogWarning($"BuildPostProcessor: source folder not found: {src}");
            return;
        }

        // path to the built player (exe or package)
        string buildExePath = report.summary.outputPath;                // e.g. /path/to/Builds/MyGame.exe
        string buildRoot = Path.GetDirectoryName(buildExePath);        // parent folder

        // destination next to the exe
        string destRoot = Path.Combine(buildRoot, "Praat");

        // copy into build root
        FileUtil.CopyFileOrDirectory(src, destRoot);

        // also copy into the Data folder for Standalone (some native libs/plugins expect files there)
        string dataFolderName = Path.GetFileNameWithoutExtension(buildExePath) + "_Data";
        string dataFolderPath = Path.Combine(buildRoot, dataFolderName);
        if (Directory.Exists(dataFolderPath))
        {
            string destInData = Path.Combine(dataFolderPath, "Praat");
            FileUtil.CopyFileOrDirectory(src, destInData);
        }

        Debug.Log($"BuildPostProcessor: copied {src} -> {destRoot} (and to Data folder if present)");
    }
}

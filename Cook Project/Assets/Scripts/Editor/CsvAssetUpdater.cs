using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CsvAssetUpdater
{
    public static string TableFolder => Path.Combine(Application.dataPath, "Res_Local", "Global", "Tables");

    [MenuItem("Tools/Update CSV Assets")]
    public static void UpdateCsvAssets()
    {
        try
        {
            DownloadGoogleSheets();
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError(ex);
        }
        finally
        {
            AssetDatabase.Refresh();
        }
    }

    private static void DownloadGoogleSheets()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName; // Go up 2 levels to ProjectFolder
        // Construct the paths for the executable and file locations
        string workingDirectory = Path.Combine(projectRoot, "Tools", "GoogleSheetBulkDownloader");
        string downloaderPath = Path.Combine(workingDirectory, "GoogleSheetBulkDownloader.exe");
        RunCommand(workingDirectory, downloaderPath, "");
    }

    private static void RunCommand(string workingDirectory, string exePath, string arguments)
    {
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // ±Ò°Ê¶iµ{
        using (Process process = Process.Start(processInfo))
        {
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            UnityEngine.Debug.Log(output);
        }
    }
}
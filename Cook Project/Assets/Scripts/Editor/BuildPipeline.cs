using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

public class BuildPipeline
{
    public static void Build()
    {
        var args = System.Environment.GetCommandLineArgs();
        try
        {
            _Build(args);
            System.Console.WriteLine($"Build Success!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
            System.Console.WriteLine($"Build Failed with exception: {ex}");
            EditorApplication.Exit(500);
        }
    }

    private static void _Build(string[] args)
    {
        if (args.Contains("-webgl"))
        {
            BuildWebGL(args);
        }
    }

    private static void BuildWebGL(string[] args)
    {
        System.Console.WriteLine("Starting WebGL Build...");
        var profileName = "";
        if (args.Contains("-fastbuild"))
            profileName = "Web_FastBuild";
        else if (args.Contains("-bestruntimebuild"))
            profileName = "Web_BestRuntimeBuild";

        var outputPath = "Builds/WebGL";
        var outputPathIndex = System.Array.IndexOf(args, "-outputpath");
        if (outputPathIndex >= 0)
        {
            var index = outputPathIndex + 1;
            if (index >= 0 && index < args.Length)
            {
                outputPath = args[index];
            }
        }
        System.Console.WriteLine($"Using profile: {profileName}, output path: {outputPath}");

        var buildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>($"Assets/Settings/Build Profiles/{profileName}.asset");
        var buildPlayerOptions = new BuildPlayerWithProfileOptions
        {
            buildProfile = buildProfile,
            locationPathName = outputPath,
            options = BuildOptions.None
        };
        var report = UnityEditor.BuildPipeline.BuildPlayer(buildPlayerOptions);

        PrintBuildReport(report);
    }

    private static void PrintBuildReport(UnityEditor.Build.Reporting.BuildReport report)
    {
        var summary = report.summary;
        var sb = new System.Text.StringBuilder();
        foreach (var step in report.steps)
        {
            sb.AppendLine($"Step: {step.name} ({step.duration.TotalSeconds:F1}s)");
            foreach (var message in step.messages)
            {
                sb.AppendLine($"  [{message.type}] {message.content}");
            }
        }
        sb.AppendLine($"Build result: {summary.result}")
            .AppendLine($"Platform: {summary.platform}")
            .AppendLine($"Output: {summary.outputPath}")
            .AppendLine($"Total size: {summary.totalSize / (1024f * 1024f):F2} MB")
            .AppendLine($"Build time: {summary.totalTime.TotalSeconds:F1} seconds")
            .AppendLine($"Warnings: {summary.totalWarnings}")
            .AppendLine($"Errors: {summary.totalErrors}");

        System.Console.WriteLine(sb.ToString());
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>Store usable game data directly in the APK, without dummy size padding.</summary>
public sealed class ReferenceAndroidPackaging : IPostGenerateGradleAndroidProject, IPostprocessBuildWithReport
{
    public int callbackOrder => 900;
    const string Marker = "// Reference edition 6 direct-read packaging";
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string root = Directory.GetParent(path).FullName;
        string properties = Path.Combine(root, "gradle.properties");
        if (!File.ReadAllText(properties).Contains("android.overridePathCheck=true"))
            File.AppendAllText(properties, "\nandroid.overridePathCheck=true\n");
        foreach (string module in new[] { path, Path.Combine(root, "launcher") })
        {
            string gradle = Path.Combine(module, "build.gradle");
            if (!File.Exists(gradle)) throw new BuildFailedException("Missing Gradle module: " + gradle);
            string source = File.ReadAllText(gradle);
            if (source.Contains(Marker)) continue;
            File.AppendAllText(gradle, "\n" + Marker + "\nandroid {\n    androidResources { noCompress += [''] }\n    packagingOptions { jniLibs { useLegacyPackaging = false } }\n}\n");
        }
    }
    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        long bytes = new FileInfo(report.summary.outputPath).Length;
        Directory.CreateDirectory("TestResults");
        File.WriteAllText("TestResults/PackageSize.txt", "APK bytes: " + bytes + "\nMinimum bytes: 500000000\nActual game assets and native libraries stored without ZIP compression; no filler files.\n");
        if (bytes < 500000000) throw new BuildFailedException("Edition 6 APK is below the requested 500 MB: " + bytes);
    }
}

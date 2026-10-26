using UnityEditor;
using UnityEngine;

public static class ReferenceSeventhBuilder
{
    public static void Prepare()
    {
        ReferenceSixthBuilder.Prepare();
        PlayerSettings.productName = "Hole It!";
        PlayerSettings.bundleVersion = "7.0.0";
        PlayerSettings.Android.bundleVersionCode = 9;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        Time.fixedDeltaTime = .02f;
        Time.maximumDeltaTime = .33333334f;
        AssetDatabase.SaveAssets();
        Debug.Log("EDITION SEVEN PREPARED: source text, physics metadata, and display branding.");
    }
}

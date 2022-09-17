using UnityEngine;
using UnityEditor;

public class PackageTool
{ 
    [MenuItem("Package/Update Package")]
    static void UpdatePackage()
    {   
        //for exporting ffmpeg files as unity package
        AssetDatabase.ExportPackage(
            "Assets/FFmpegOut",
            "FFmpegOut.unitypackage",
            ExportPackageOptions.Recurse
        );
    }
}

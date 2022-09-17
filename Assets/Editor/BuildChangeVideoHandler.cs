#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

public class ActiveBuildTargetListener : IActiveBuildTargetChanged
{
    public int callbackOrder { get { return 0; } }
    public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
    {
        Debug.Log("OnActiveBuildTargetChanged");
        #if UNITY_ANDROID
        if(System.IO.Directory.Exists("Assets/StreamingAssets")){
            DisableStreamingAssets();
        }
        #else
        if(System.IO.Directory.Exists("Assets/xxxStreamingAssets")){
            EnableStreamingAssets();
        }
        #endif
    }
    void DisableStreamingAssets()
    {
        System.IO.Directory.Move("Assets/StreamingAssets","Assets/xxxStreamingAssets");
        Debug.Log("Disabled Streaming Assets");
    }
    void EnableStreamingAssets()
    {
        System.IO.Directory.Move("Assets/xxxStreamingAssets","Assets/StreamingAssets");
        Debug.Log("Enabled Streaming Assets");
    }
}
#endif
using UnityEditor;
using UnityEngine;

namespace Hamster.EditorTools
{
    // Portrait-only phone settings shared by iOS and Android. Applied once automatically
    // (when the project is still on auto-rotate) and available from the Hamster menu.
    [InitializeOnLoad]
    public static class MobilePlayerSettings
    {
        static MobilePlayerSettings()
        {
            EditorApplication.delayCall += () =>
            {
                if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait) Apply();
            };
        }

        [MenuItem("Hamster/Apply Mobile Player Settings")]
        static void Apply()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.requiresFullScreen = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[Hamster] Portrait phone settings applied (iOS + Android).");
        }
    }
}

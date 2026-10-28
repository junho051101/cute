using System.IO;
using Utopia.Title;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utopia.EditorTools
{
    // One-time project setup, run inside the editor so Unity saves it itself:
    // product name "Utopia", portrait-only phone settings (iOS first, Android the same),
    // and the Title scene as the first and only build scene. Also on the Utopia menu.
    [InitializeOnLoad]
    public static class UtopiaProjectSetup
    {
        public const string ProductName = "Utopia";
        const string TitleScenePath = "Assets/Scenes/Title.unity";

        static UtopiaProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (PlayerSettings.productName != ProductName || PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                    ApplyPlayerSettings();
                if (!File.Exists(TitleScenePath)) CreateTitleScene(openAfter: true);
                else if (SceneManager.GetActiveScene().path is var open && open.Length > 0 && !File.Exists(open))
                    // The open scene was deleted (e.g. the old village); Play would run a ghost of it.
                    EditorSceneManager.OpenScene(TitleScenePath);
            };
        }

        [MenuItem("Utopia/Apply Player Settings")]
        static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.requiresFullScreen = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[Utopia] Player settings applied: product name Utopia, portrait only (iOS + Android).");
        }

        [MenuItem("Utopia/Rebuild Title Scene")]
        static void RebuildFromMenu() => CreateTitleScene(openAfter: true);

        static void CreateTitleScene(bool openAfter)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var go = new GameObject("TitleBootstrap");
            go.AddComponent<TitleBootstrap>();
            SceneManager.MoveGameObjectToScene(go, scene);
            EditorSceneManager.SaveScene(scene, TitleScenePath);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TitleScenePath, true) };

            if (openAfter && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(TitleScenePath);
            Debug.Log($"[Utopia] Title scene ready at {TitleScenePath}. Press Play to see the loading screen.");
        }
    }

    // Title art is pixel art: keep every pixel crisp and uncompressed.
    public class TitleArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Title/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
        }
    }
}

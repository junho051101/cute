using System.Linq;
using Hamster.Village;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hamster.EditorTools
{
    // Creates Assets/Scenes/Village.unity (one VillageBootstrap object) the first time scripts compile,
    // and puts it first in Build Settings. Also available from the Hamster menu.
    [InitializeOnLoad]
    public static class VillageSceneCreator
    {
        const string ScenePath = "Assets/Scenes/Village.unity";

        static VillageSceneCreator()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !System.IO.File.Exists(ScenePath))
                    Create(openAfter: true);
            };
        }

        [MenuItem("Hamster/Rebuild Village Scene")]
        static void RebuildFromMenu() => Create(openAfter: true);

        static void Create(bool openAfter)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var go = new GameObject("VillageBootstrap");
            go.AddComponent<VillageBootstrap>();
            SceneManager.MoveGameObjectToScene(go, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();

            var others = EditorBuildSettings.scenes.Where(s => s.path != ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(others).ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            if (openAfter && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);

            Debug.Log($"[Hamster] Village scene ready at {ScenePath}. Press Play to walk around.");
        }
    }
}

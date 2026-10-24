using System.Collections.Generic;
using ShapeInventory.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShapeInventory.Editor
{
    /// <summary>
    /// Writes the current campaign (ten levels, data order) into the serialized `levels` array of every GameFlowController in
    /// Assets/Scenes/Main.unity, so the user never has to rebuild the scene after the sample content changed.
    /// </summary>
    internal static class ContentSceneSync
    {
        internal const string MainScenePath = "Assets/Scenes/Main.unity";

        /// <summary>Returns true when at least one controller was updated (and saved, if the scene was ours to save).</summary>
        internal static bool SyncLevelsIntoMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("ShapeInventory: leave Play Mode before syncing levels into " + MainScenePath + ".");
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            {
                Debug.LogWarning("ShapeInventory: " + MainScenePath + " does not exist - skipped the level sync "
                                 + "(run 'ShapeInventory/Build Demo Scene' to create it).");
                return false;
            }

            List<LevelData> levels = SampleContentBuilder.LoadCampaignLevels(true);
            if (levels.Count == 0)
            {
                Debug.LogWarning("ShapeInventory: no campaign level assets found - skipped the level sync.");
                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(MainScenePath);
            bool alreadyOpen = scene.IsValid() && scene.isLoaded;
            bool wasDirty = alreadyOpen && scene.isDirty;
            SceneSetup[] previousSetup = null;

            if (!alreadyOpen)
            {
                // Never discard unsaved work in the scenes that are open right now.
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("ShapeInventory: level sync cancelled (unsaved changes were not resolved).");
                    return false;
                }

                previousSetup = EditorSceneManager.GetSceneManagerSetup();
                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Debug.LogWarning("ShapeInventory: could not open " + MainScenePath + " - skipped the level sync.");
                    return false;
                }
            }

            int updated = 0;
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    GameFlowController[] controllers = root.GetComponentsInChildren<GameFlowController>(true);
                    foreach (GameFlowController controller in controllers)
                    {
                        var so = new SerializedObject(controller);
                        SerializedProperty prop = so.FindProperty("levels");
                        if (prop == null || !prop.isArray)
                        {
                            Debug.LogWarning("ShapeInventory: GameFlowController '" + controller.name + "' has no serialized array 'levels'.");
                            continue;
                        }

                        prop.arraySize = levels.Count;
                        for (int i = 0; i < levels.Count; i++)
                            prop.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(controller);
                        updated++;
                    }
                }

                if (updated == 0)
                {
                    Debug.LogWarning("ShapeInventory: no GameFlowController found in " + MainScenePath + " - nothing to sync "
                                     + "(run 'ShapeInventory/Build Demo Scene').");
                }
                else
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (wasDirty)
                    {
                        Debug.LogWarning("ShapeInventory: " + MainScenePath + " had unsaved changes; the levels were synced but the "
                                         + "scene was left unsaved - save it yourself.");
                    }
                    else
                    {
                        EditorSceneManager.SaveScene(scene);
                        Debug.Log("ShapeInventory: synced " + levels.Count + " levels into " + updated
                                  + " GameFlowController(s) in " + MainScenePath + " and saved the scene.");
                    }
                }
            }
            finally
            {
                if (!alreadyOpen)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previousSetup != null && previousSetup.Length > 0 && SceneManager.sceneCount != previousSetup.Length)
                        EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }

            return updated > 0;
        }
    }
}

/*******************************************************************************
The content of this file includes portions of the proprietary AUDIOKINETIC Wwise
Technology released in source code form as part of the game integration package.
The content of this file may not be used without valid licenses to the
AUDIOKINETIC Wwise Technology.
Note that the use of the game engine is subject to the Unity(R) Terms of
Service at https://unity3d.com/legal/terms-of-service
 
License Usage
 
Licensees holding valid licenses to the AUDIOKINETIC Wwise Technology may use
this file in accordance with the end user license agreement provided with the
software or, alternatively, in accordance with the terms contained
in a written agreement between you and Audiokinetic Inc.
Copyright (c) 2024 Audiokinetic Inc.
*******************************************************************************/
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
public class SceneUpdater
{
    private static bool hierarchyChanged = false;
    [UnityEditor.MenuItem("Tools/Reload All Scenes")]
    public static void ReloadAllScenes()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.update += Update;
        // Start the process
        ReloadNextScene();
    }
    
    private static void OnHierarchyChanged()
    {
        hierarchyChanged = true;
    }
    
    private static List<string> scenePaths;
    private static int currentSceneIndex = 0;
    
    private static void ReloadNextScene()
    {
        if (scenePaths == null)
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            scenePaths = sceneGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToList();
        }
        if (currentSceneIndex < scenePaths.Count)
        {
            string scenePath = scenePaths[currentSceneIndex];
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            hierarchyChanged = false;
        }
        else
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.update -= Update;
            EditorUtility.DisplayDialog("Reload All Scenes", "All scenes have been reloaded.", "OK");
        }
    }
    
    private static void Update()
    {
        if (hierarchyChanged)
        {
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            currentSceneIndex++;
            ReloadNextScene();
        }
    }
}
#endif
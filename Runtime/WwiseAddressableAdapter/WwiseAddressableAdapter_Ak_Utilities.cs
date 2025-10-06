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
Copyright (c) 2026 Audiokinetic Inc.
*******************************************************************************/

#if ADDRESSABLES_API_BREAK_AK_UTILITIES || ADDRESSABLES_API_BROWSER_TREE_VIEW
public class WwiseAddressableAdapter_Ak_Utilities : WwiseAddressableAdapter_Null
{
    public override void ResetInstance(ref AkUnitySoundEngineInitialization instance)
    {
        if(instance != null)
        {
            System.Action copyInitialize = instance.initializationDelegate;
#if WWISE_ADDRESSABLES_24_1_OR_LATER
            System.Action copyReInitialize = instance.reInitializationDelegate;
#endif
            System.Action copyTerminate = instance.terminationDelegate;
#if WWISE_2024_OR_LATER
            instance = new AkUnityAddressablesSoundEngineInitialization();
#else
			instance = new AkAddressablesSoundEngineInitialization();
#endif
            instance.initializationDelegate = copyInitialize;
#if WWISE_ADDRESSABLES_24_1_OR_LATER
            instance.reInitializationDelegate = copyReInitialize;
#endif
            instance.terminationDelegate = copyTerminate;
        }
        else
        {
#if WWISE_2024_OR_LATER
            instance = new AkUnityAddressablesSoundEngineInitialization();
#else
			instance = new AkAddressablesSoundEngineInitialization();
#endif
        }
    }
    
#if UNITY_EDITOR
    public override void CreateFolderFromAkUtilities(string folderPath)
    {
        AkUtilities.CreateFolder(folderPath);
    }

    public override bool IsAutoBankEnabled()
    {
        return AkUtilities.IsAutoBankEnabled();
    }

    public override string GetFullPath(string basePath, string relativePath)
    {
        return AkUtilities.GetFullPath(basePath, relativePath);
    }

    public override string MakeRelativePath(string fromPath, string toPath)
    {
        return AkUtilities.MakeRelativePath(fromPath, toPath);
    }
#endif
}
#endif //ADDRESSABLES_API_BREAK_AK_UTILITIES || ADDRESSABLES_API_BROWSER_TREE_VIEW

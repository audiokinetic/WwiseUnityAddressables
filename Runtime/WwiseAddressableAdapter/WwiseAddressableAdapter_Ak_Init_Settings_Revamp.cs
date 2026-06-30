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
#if ADDRESSABLES_API_BREAK_AK_INIT_SETTINGS_REVAMP
using System.Collections.Generic;
using System.Linq;
using AK.Wwise.Unity.Settings;
using AK.Wwise.Unity.Logging;
using UnityEngine;
using Wwise.SDK;

public class WwiseAddressableAdapter_Ak_Init_Settings_Revamp : WwiseAddressableAdapter_Null
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
            System.Action<List<AkOptionNamespace>> copyPreInitialization = instance.preInitializationDelegate;
            System.Action copyPostTermination = instance.postTerminationDelegate;
            
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
            instance.preInitializationDelegate = copyPreInitialization;
            instance.postTerminationDelegate = copyPostTermination;
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
    
    public override string GetRootOuputPath()
    {
        return AkWwiseEditorSettings.GetRootOutputPath();
    }
#endif

    public override void WwiseLog(string message)
    {
        WwiseLogger.Log(message);
    }
    
    public override void WwiseWarning(string message)
    {
        WwiseLogger.Warning(message);
    }
    
    public override void WwiseError(string message)
    {
        WwiseLogger.Error(message);
    }
    
    public override void WwiseLogFormat(string message, params object[] args)
    {
        WwiseLogger.LogFormat(message, args);
    }
    
    public override void WwiseWarningFormat(string message, params object[] args)
    {
        WwiseLogger.WarningFormat(message, args);
    }
    
    public override void WwiseErrorFormat(string message, params object[] args)
    {
        WwiseLogger.ErrorFormat(message, args);
    }
}
#endif //ADDRESSABLES_API_BREAK_AK_INIT_SETTINGS_REVAMP

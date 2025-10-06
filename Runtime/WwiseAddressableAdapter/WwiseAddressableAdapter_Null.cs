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

using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

public class WwiseAddressableAdapter_Null
{
    private bool _shouldLogMissmatch = true;
    private string _currentIntegrationVersion;
    private string _packageVersion;

    public WwiseAddressableAdapter_Null()
    {
        string wwiseVersion = AkUnitySoundEngine.WwiseVersion;
        int firstSpaceIndex = wwiseVersion.IndexOf(' ');
        if (firstSpaceIndex < 0)
        {
            return;
        }
        string shortWwiseVersion = wwiseVersion.Substring(2, firstSpaceIndex-2);
        _currentIntegrationVersion = shortWwiseVersion;
        string[] versionParts = shortWwiseVersion.Split('.');
        int.TryParse(versionParts[2], out var currentMinor);
        
        // Get package version
        _packageVersion = GetPackageVersion();
    }
    
    private static string GetPackageVersion()
    {
        string version = "Unknown";
        
#if UNITY_EDITOR
        try
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.audiokinetic.wwise.addressables/package.json");
            if (packageInfo != null)
            {
                version = packageInfo.version;
            }
        }
        catch
        {
            // ignored
        }
#endif
        if (version == "Unknown")
        {
            try
            {
                string packageJsonPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Packages", "com.audiokinetic.wwise.addressables", "package.json");
                if (System.IO.File.Exists(packageJsonPath))
                {
                    string jsonContent = System.IO.File.ReadAllText(packageJsonPath);
                    // Simple JSON parsing for version field
                    string versionKey = "\"version\":";
                    int versionIndex = jsonContent.IndexOf(versionKey);
                    if (versionIndex >= 0)
                    {
                        int startQuote = jsonContent.IndexOf('"', versionIndex + versionKey.Length);
                        int endQuote = jsonContent.IndexOf('"', startQuote + 1);
                        if (startQuote >= 0 && endQuote >= 0)
                        {
                            version = jsonContent.Substring(startQuote + 1, endQuote - startQuote - 1);
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
        
        if (version != "Unknown")
        {
            version = version.Substring(2);
        }
        
        return version;
    }
    
    public void LogMissMatchVersion()
    {
        if (_shouldLogMissmatch)
        {
            UnityEngine.Debug.LogError(
                $"Package incompatibility detected: Current Wwise integration version is {_currentIntegrationVersion}, but the installed Addressables package version is {_packageVersion}. Please ensure you're using compatible versions.");
        }
    }

    private void LogNotImplemented([CallerMemberName] string methodName = null)
    {
        if (_shouldLogMissmatch)
        {
            UnityEngine.Debug.LogError($"Wwise Addressables function {methodName} is not implemented. Have you updated your Wwise Unity Addressables package?");
        }
    }

    public virtual void ResetInstance(ref AkUnitySoundEngineInitialization instance)
    {
        LogNotImplemented();
    }

    public virtual void CreateFolderFromAkUtilities(string folderPath)
    {
        LogNotImplemented();
    }

    public virtual bool IsAutoBankEnabled()
    {
        LogNotImplemented();
        return false;
    }

    public virtual string GetFullPath(string basePath, string relativePath)
    {
        LogNotImplemented();
        return string.Empty;
    }

    public virtual string MakeRelativePath(string fromPath, string toPath)
    {
        LogNotImplemented();
        return string.Empty;
    }
    
    public virtual string GetRootOuputPath()
    {
        LogNotImplemented();
        return string.Empty;
    }
}

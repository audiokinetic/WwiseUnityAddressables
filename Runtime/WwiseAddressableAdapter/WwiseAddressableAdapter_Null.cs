using System.Runtime.CompilerServices;

public class WwiseAddressableAdapter_Null : IWwiseAddressableAdapter
{
    private static bool _missMatchVersionLog = false;
    private static string currentIntegrationVersion;

    public WwiseAddressableAdapter_Null()
    {
        string wwiseVersion = AkUnitySoundEngine.WwiseVersion;
        int firstSpaceIndex = wwiseVersion.IndexOf(' ');
        if (firstSpaceIndex < 0)
        {
            return;
        }
        string shortWwiseVersion = wwiseVersion.Substring(2, firstSpaceIndex-2);
        currentIntegrationVersion = shortWwiseVersion;
        LogMissMatchVersion();
    }

    private static void LogMissMatchVersion()
    {
        UnityEngine.Debug.LogError($"The current Wwise Unity Addressables package version is not compatible with the installed integration. Please upgrade to the {currentIntegrationVersion} Wwise Addressables package.");
    }
    
    private static void LogNotImplemented([CallerMemberName] string methodName = null)
    {
        UnityEngine.Debug.LogError($"Wwise Addressables function {methodName} is not implemented. Have you updated your Wwise Unity Addressables package?");
    }

    public void ResetInstance(AkUnitySoundEngineInitialization instance)
    {
        LogNotImplemented();
    }

    public void CreateFolderFromAkUtilities(string folderPath)
    {
        LogNotImplemented();
    }

    public bool IsAutoBankEnabled()
    {
        LogNotImplemented();
        return false;
    }

    public string GetFullPath(string basePath, string relativePath)
    {
        LogNotImplemented();
        return string.Empty;
    }

    public string MakeRelativePath(string fromPath, string toPath)
    {
        LogNotImplemented();
        return string.Empty;
    }
}

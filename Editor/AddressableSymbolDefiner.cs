using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

[InitializeOnLoad]
public static class AddressableSymbolDefiner
{
    private const string CurrentVersion = "WWISE_ADDRESSABLES_24_1_OR_LATER";
    private static string _currentAdapterVersion = "";

    static AddressableSymbolDefiner()
    {
        AkUnitySoundEngineInitialization.Instance.initializationDelegate += AddAdapterSymbol;
        
        //Clean up function we subscribe to in order to properly unsubscribe from the initializationDelegate on domain reload or editor shutdown
        AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        EditorApplication.quitting += Cleanup;
        
        if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains(CurrentVersion))
        {
            return;
        }
#if !WWISE_2024_OR_LATER
        return;
#endif
        foreach (BuildTargetGroup targetGroup in GetNonObsoleteTargetGroups())
        {
            if (targetGroup == BuildTargetGroup.Unknown)
            {
                continue;
            }
            AddDefineSymbols(targetGroup);
        }
    }
    
    private static void Cleanup()
    {
        AkUnitySoundEngineInitialization.Instance.initializationDelegate -= AddAdapterSymbol;
        AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;
        EditorApplication.quitting -= Cleanup;
    }

    private static void SetCurrentAdapterVersion()
    {
        string wwiseVersion = AkUnitySoundEngine.WwiseVersion;
        int firstSpaceIndex = wwiseVersion.IndexOf(' ');
        if (firstSpaceIndex < 0)
        {
            return;
        }
        string shortWwiseVersion = wwiseVersion.Substring(0, firstSpaceIndex);
        string[] versionParts = shortWwiseVersion.Split('.');
        int currentMinor = 0;
        
        if (int.TryParse(versionParts[2], out currentMinor))
        {
            if (currentMinor is > 7 and < 10)
            {
                _currentAdapterVersion = "ADDRESSABLES_API_BREAK_AK_UTILITIES";
            }
            else
            {
                _currentAdapterVersion = "ADDRESSABLES_API_DEFAULT";
            }
        }
    }
    
    public static BuildTargetGroup[] GetNonObsoleteTargetGroups()
    {
        Type enumType = typeof(BuildTargetGroup);

        return Enum.GetValues(enumType).Cast<BuildTargetGroup>().Where(targetGroup =>
        {
            string name = targetGroup.ToString();
            MemberInfo member = enumType.GetMember(name).FirstOrDefault();

            if (member == null) return false; 

            return member.GetCustomAttribute<ObsoleteAttribute>() == null;
        }).ToArray();
    }

    private static void AddAdapterSymbol()
    {
        if (_currentAdapterVersion == "")
        {
            SetCurrentAdapterVersion();
        }
        if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains(_currentAdapterVersion))
        {
            return;
        }
        
        foreach (BuildTargetGroup targetGroup in GetNonObsoleteTargetGroups())
        {
#if UNITY_5_4_OR_NEWER
            if (targetGroup == BuildTargetGroup.Unknown)
            {
                continue;
            }
#endif
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var currentDefineList = currentDefines.Split(';').ToList();

            const string wwiseAddressablePrefix = "ADDRESSABLES_API";
            currentDefineList.RemoveAll(define => define.StartsWith(wwiseAddressablePrefix));

            if (!string.IsNullOrEmpty(_currentAdapterVersion))
            {
                currentDefineList.Add(_currentAdapterVersion);
            }

            string newDefinesString = string.Join(";", currentDefineList.Distinct());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefinesString);
        }
    }

    private static void AddDefineSymbols(BuildTargetGroup targetGroup)
    {
        string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
        var currentDefineList = currentDefines.Split(';').ToList();
        
        if (!currentDefineList.Contains(CurrentVersion))
        {
            currentDefineList.Add(CurrentVersion);
            string updatedDefines = string.Join(";", currentDefineList);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, updatedDefines);
        }
        
    }

}

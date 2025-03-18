using System;
using System.Linq;
using UnityEditor;

[InitializeOnLoad]
public static class AddressableSymbolDefiner
{
    private const string CurrentVersion = "WWISE_ADDRESSABLES_24_1_OR_LATER";

    static AddressableSymbolDefiner()
    {

        if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains(CurrentVersion))
        {
            return;
        }
#if !WWISE_2024_OR_LATER
        return;
#endif
        foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            if (targetGroup != BuildTargetGroup.Unknown)
            {
                AddDefineSymbols(targetGroup);
            }
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

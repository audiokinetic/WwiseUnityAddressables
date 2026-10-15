using System.Linq;
using AK.Wwise.Unity.WwiseAddressables;
using UnityEditor;
using UnityEditor.Build;

[InitializeOnLoad]
public static class AddressableSymbolDefiner
{
    private const string CurrentVersion = "WWISE_ADDRESSABLES_24_1_OR_LATER";

    static AddressableSymbolDefiner()
    {
        if (PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Contains(CurrentVersion))
        {
            return;
        }
#if !WWISE_2024_OR_LATER
        return;
#endif
        foreach (BuildTargetGroup targetGroup in AkAddressablesUtilities.GetNonObsoleteTargetGroups())
        {
            if (targetGroup == BuildTargetGroup.Unknown)
            {
                continue;
            }
            AddDefineSymbols(targetGroup);
        }
    }
    private static void AddDefineSymbols(BuildTargetGroup targetGroup)
    {
        string currentDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));
        var currentDefineList = currentDefines.Split(';').ToList();
        
        if (!currentDefineList.Contains(CurrentVersion))
        {
            currentDefineList.Add(CurrentVersion);
            string updatedDefines = string.Join(";", currentDefineList);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), updatedDefines);
        }
        
    }

}

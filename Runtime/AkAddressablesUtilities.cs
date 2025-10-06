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
Copyright (c) 2025 Audiokinetic Inc.
*******************************************************************************/

using System;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using System.Reflection;
#endif

namespace AK.Wwise.Unity.WwiseAddressables
{
    /// <summary>
    /// Utility class containing common helper functions for Wwise Addressables.
    /// This class is in the Runtime assembly so both Runtime and Editor code can reference it.
    /// </summary>
    public static class AkAddressablesUtilities
    {
#if UNITY_EDITOR
        /// <summary>
        /// Gets all non-obsolete build target groups.
        /// This is useful for iterating through platform targets without encountering obsolete values.
        /// </summary>
        /// <returns>Array of non-obsolete BuildTargetGroup values</returns>
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
#endif
    }
}

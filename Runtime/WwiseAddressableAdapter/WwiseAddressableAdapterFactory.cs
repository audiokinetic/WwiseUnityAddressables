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

using System;
using System.Reflection;
public static class WwiseAddressableAdapterFactory
{
    public static WwiseAddressableAdapter_Null CreateManager()
    {
#if ADDRESSABLES_API_BREAK_AK_UTILITIES_WWISE_LOGGER
        return new WwiseAddressableAdapter_Ak_Wwise_Logger();
#else
        WwiseAddressableAdapter_Null adapter = new WwiseAddressableAdapter_Null();
#if ADDRESSABLES_API_BREAK_AK_UTILITIES || ADDRESSABLES_API_BROWSER_TREE_VIEW
        adapter = new WwiseAddressableAdapter_Ak_Utilities();
#else
        adapter = new WwiseAddressableAdapter_Null();
#endif
        adapter.LogMissMatchVersion();
        return adapter;
#endif
    }
}

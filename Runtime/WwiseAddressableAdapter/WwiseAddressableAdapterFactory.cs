using System;
using System.Reflection;
public static class WwiseAddressableAdapterFactory
{
    public static IWwiseAddressableAdapter CreateManager()
    {
#if ADDRESSABLES_API_DEFAULT
        return new WwiseAddressableAdapter_Default();
#else
        return new WwiseAddressableAdapter_Null();
#endif
    }
}
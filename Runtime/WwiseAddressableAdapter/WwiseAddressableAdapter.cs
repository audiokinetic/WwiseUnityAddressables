using UnityEngine;

public class WwiseAddressableAdapter
{
    private static IWwiseAddressableAdapter _instance;
    public static IWwiseAddressableAdapter Instance {
        get
        {
            if (_instance == null)
            {
                _instance = WwiseAddressableAdapterFactory.CreateManager();
            }
            return _instance;
        }
    }
}

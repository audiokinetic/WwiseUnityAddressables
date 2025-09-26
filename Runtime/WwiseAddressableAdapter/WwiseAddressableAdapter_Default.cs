#if ADDRESSABLES_API_DEFAULT
public class WwiseAddressableAdapter_Default : IWwiseAddressableAdapter
{
        public void ResetInstance(AkUnitySoundEngineInitialization instance)
        {
            if(instance != null)
            {
                AkUnitySoundEngineInitialization.InitializationDelegate copyInitialize = instance.initializationDelegate;
                AkUnitySoundEngineInitialization.ReInitializationDelegate copyReInitialize = instance.reInitializationDelegate;
                AkUnitySoundEngineInitialization.TerminationDelegate copyTerminate = instance.terminationDelegate;
                instance = new AkUnityAddressablesSoundEngineInitialization();
                instance.initializationDelegate = copyInitialize;
                instance.reInitializationDelegate = copyReInitialize;
                instance.terminationDelegate = copyTerminate;
            }
            else
            {
                instance = new AkUnityAddressablesSoundEngineInitialization();
            }
        }

        public void CreateFolderFromAkUtilities(string folderPath)
        {
            AkUtilities.CreateFolder(folderPath);
        }

        public bool IsAutoBankEnabled()
        {
            return AkUtilities.IsAutoBankEnabled();
        }

        public string GetFullPath(string basePath, string relativePath)
        {
            return AkUtilities.GetFullPath(basePath, relativePath);
        }

        public string MakeRelativePath(string fromPath, string toPath)
        {
            return AkUtilities.MakeRelativePath(fromPath, toPath);
        }
}
#endif
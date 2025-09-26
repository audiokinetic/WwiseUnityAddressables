public interface IWwiseAddressableAdapter
{
    public void ResetInstance(AkUnitySoundEngineInitialization instance);
    public void CreateFolderFromAkUtilities(string folderPath);
    public bool IsAutoBankEnabled();
    public string GetFullPath(string basePath, string relativePath);
    public string MakeRelativePath(string fromPath, string toPath);
}

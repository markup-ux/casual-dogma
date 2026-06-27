namespace Arrowgene.Ddon.Shared.AssetReader
{
    public interface IDirectoryAssetHandler
    {
        string DirectoryKey { get; }
        string Filter { get; }
        object LiveAsset { get; }
        bool OnFileChanged(string filePath);
        void OnFileRemoved(string filePath);
    }
}

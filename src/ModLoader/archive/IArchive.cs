namespace ModLoader.archive;

public interface IArchive {
    public void Archive(Dictionary<string, byte[]> fileList);

    public void Recover(Func<string, byte[]> getDataFunc);
}
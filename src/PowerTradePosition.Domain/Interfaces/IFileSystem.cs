namespace PowerTradePosition.Domain.Interfaces;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    bool FileExists(string path);
    void DeleteFile(string path);
}
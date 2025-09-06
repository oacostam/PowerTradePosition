using Microsoft.Extensions.Logging;
using PowerTradePosition.Domain.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace PowerTradePosition.Domain.Domain;

/// <summary>
///     Filesystem wrapper, providing basic file and directory operations with logging, exception handling, and testability.
/// </summary>
/// <param name="logger"></param>
[ExcludeFromCodeCoverage]
public class FileSystem(ILogger<FileSystem> logger) : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if directory exists: {Path}", path);
            return false;
        }
    }

    public void CreateDirectory(string path)
    {
        try
        {
            if (DirectoryExists(path)) return;
            Directory.CreateDirectory(path);
            logger.LogInformation("Created directory: {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating directory: {Path}", path);
            throw;
        }
    }

    public bool FileExists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if file exists: {Path}", path);
            return false;
        }
    }

    public void DeleteFile(string path)
    {
        try
        {
            if (!FileExists(path)) return;
            File.Delete(path);
            logger.LogInformation("Deleted file: {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting file: {Path}", path);
            throw;
        }
    }
}
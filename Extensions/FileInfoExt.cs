
namespace RRBot.Extensions;
public static class FileInfoExt
{
    internal static string GetFullPath(this FileInfo fileInfo)
    {
        if (File.Exists(fileInfo.Name))
        {
            return Path.GetFullPath(fileInfo.Name);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string filePath = fileInfo.Name + ".exe";
            if (File.Exists(filePath))
            {
                return Path.GetFullPath(filePath);
            }
        }

        string environmentVariable = Environment.GetEnvironmentVariable("PATH");
        if (environmentVariable != null)
        {
            foreach (string path in environmentVariable.Split(Path.PathSeparator))
            {
                string fullPath = Path.Combine(path, fileInfo.Name);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    fullPath += ".exe";
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
        }

        return null;
    }
}
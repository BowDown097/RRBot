namespace RRBot.Extensions;
public static class ProcessExt
{
    public static async Task<string> RunWithOutputAsync(this Process proc, string cmd, string args)
    {
        proc.StartInfo.FileName = new FileInfo(cmd).GetFullPath();
        proc.StartInfo.Arguments = args;
        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.UseShellExecute = false;
        proc.Start();

        string output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        return output;
    }
}
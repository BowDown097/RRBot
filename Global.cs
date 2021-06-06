using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace RRBot
{
    internal static class Global
    {
        // methods
        public static string FormatTime(long secs) => FormatTime(TimeSpan.FromSeconds(secs));
        public static string FormatTime(TimeSpan ts) => string.Format("{0} minute(s) {1} second(s)", ts.Minutes, ts.Seconds).Replace("0 minutes ", "");

        public static void RunInBackground(Action action)
        {
            using (BackgroundWorker bw = new BackgroundWorker())
            {
                bw.DoWork += delegate (object sender, DoWorkEventArgs e)
                {
                    action.Invoke();
                };

                bw.RunWorkerAsync();
            }
        }

        public static long UnixTime(double addSecs = 0)
        {
            TimeSpan epoch = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            return (long)(epoch.TotalSeconds + addSecs);
        }

        // other
        public static readonly Regex niggerRegex = new Regex(@"[nɴⁿₙñńņňÑŃŅŇ][i1!¡ɪᶦᵢ¹₁jįīïîíì|;:𝗂][g9ɢᵍ𝓰𝓰qģğĢĞ][g9ɢᵍ𝓰𝓰qģğĢĞ][e3€ᴇᵉₑ³₃ĖĘĚĔėęěĕəèéêëē𝖾][rʀʳᵣŔŘŕř]");
    }
}
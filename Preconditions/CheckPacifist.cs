using Discord.Commands;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckPacifistAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            return snap.TryGetValue("perks", out Dictionary<string, long> perks) && perks.Keys.Contains("Pacifist")
                ? PreconditionResult.FromError("You cannot use this command as you have the Pacifist perk.")
                : PreconditionResult.FromSuccess();
        }
    }
}

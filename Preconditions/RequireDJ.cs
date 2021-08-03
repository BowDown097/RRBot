using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireDJAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            return snap.TryGetValue("djRole", out ulong djId) && (context.User as IGuildUser).RoleIds.Contains(djId)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"{context.User.Mention}, you must be DJ!");
        }
    }
}

using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireStaffAttribute : PreconditionAttribute
    {
        public override string ErrorMessage { get; set; }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            return (snap.TryGetValue("houseRole", out ulong staff1Id) && (context.User as IGuildUser).RoleIds.Contains(staff1Id))
            || (snap.TryGetValue("senateRole", out ulong staff2Id) && (context.User as IGuildUser).RoleIds.Contains(staff2Id))
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"{context.User.Mention}, you must be Staff!");
        }
    }
}

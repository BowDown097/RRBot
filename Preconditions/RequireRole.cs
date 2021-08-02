using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireRoleAttribute : PreconditionAttribute
    {
        public string DatabaseReference { get; }

        public RequireRoleAttribute(string dbRef) => DatabaseReference = dbRef;

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue(DatabaseReference, out ulong roleId))
            {
                IRole role = context.Guild.GetRole(roleId);
                return (context.User as IGuildUser).RoleIds.Contains(roleId)
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError($"{context.User.Mention}, you must have the {role.Name} role.");
            }

            return PreconditionResult.FromError($"{DatabaseReference} role is not set!");
        }
    }
}

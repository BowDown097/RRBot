using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequireRole : PreconditionAttribute
    {
        public string DatabaseReference { get; }
        public override string ErrorMessage { get; set; }

        public RequireRole(string dbRef) => DatabaseReference = dbRef;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = doc.GetSnapshotAsync().Result;

            if (snap.TryGetValue(DatabaseReference, out ulong roleId))
            {
                IRole role = context.Guild.GetRole(roleId);
                if ((context.Message.Author as IGuildUser).RoleIds.Contains(roleId)) return Task.FromResult(PreconditionResult.FromSuccess());
                return Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? $"{context.Message.Author.Mention}, you must have the {role.Name} role."));
            }
                
            return Task.FromResult(PreconditionResult.FromError("Role is not set"));
        }
    }
}

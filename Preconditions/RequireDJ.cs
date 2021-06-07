using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequireDJAttribute : PreconditionAttribute
    {
        public override string ErrorMessage { get; set; }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = doc.GetSnapshotAsync().Result;

            if (snap.TryGetValue("djRole", out ulong djId) & (context.Message.Author as IGuildUser).RoleIds.Contains(djId)) return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{context.Message.Author.Mention}, you must be Staff!"));
        }
    }
}

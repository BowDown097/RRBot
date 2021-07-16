using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireCooldownAttribute : PreconditionAttribute
    {
        public string CooldownNode { get; set; }
        public string Message { get; set; }

        public RequireCooldownAttribute(string cooldownNode, string message)
        {
            CooldownNode = cooldownNode;
            Message = message;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue(CooldownNode, out long cooldown) && cooldown != 0L)
            {
                if (cooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    return PreconditionResult.FromError(string.Format($"{context.User.Mention}, {Message}",
                            TimeSpan.FromSeconds(cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()));
                }

                await doc.SetAsync(new Dictionary<string, object>
                {
                    { CooldownNode, 0L }
                }, SetOptions.MergeAll);
            }

            return PreconditionResult.FromSuccess();
        }
    }
}

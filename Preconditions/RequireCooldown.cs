using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            if (snap.TryGetValue(CooldownNode, out long cooldown))
            {
                long cooldownOffset = cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (snap.TryGetValue("perks", out Dictionary<string, long> perks) && perks.Keys.Contains("Speed Demon"))
                    cooldownOffset = (long)(cooldownOffset * 0.85);

                long newCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(cooldownOffset);
                if (newCooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    return PreconditionResult.FromError(string.Format($"{context.User.Mention}, {Message}",
                            TimeSpan.FromSeconds(cooldownOffset).FormatCompound()));
                }

                await doc.SetAsync(new Dictionary<string, object>
                {
                    { CooldownNode, FieldValue.Delete }
                }, SetOptions.MergeAll);
            }

            return PreconditionResult.FromSuccess();
        }
    }
}

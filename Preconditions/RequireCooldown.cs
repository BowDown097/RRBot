using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Cloud.Firestore;

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
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.Message.Author.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue(CooldownNode, out long cooldown) && cooldown != 0L)
            {
                if (cooldown > Global.UnixTime())
                    return PreconditionResult.FromError(string.Format($"{context.Message.Author.Mention}, {Message}", Global.FormatTime(cooldown - Global.UnixTime())));

                await doc.SetAsync(new Dictionary<string, object>
                {
                    { CooldownNode, 0L }
                }, SetOptions.MergeAll);
            }

            return PreconditionResult.FromSuccess();
        }
    }
}

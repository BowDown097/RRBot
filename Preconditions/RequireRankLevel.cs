using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequireRankLevel : PreconditionAttribute
    {
        public int RankLevel { get; }
        public override string ErrorMessage { get; set; }

        public RequireRankLevel(int level) => RankLevel = level;

        // this entire thing is actually fucking AWFUL. there HAS to be a better way to do this, surely.
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/config").Document("ranks");
            DocumentSnapshot snap = doc.GetSnapshotAsync().Result;
            try
            {
                KeyValuePair<string, object> level = snap.ToDictionary().First(kvp => kvp.Key.StartsWith($"level{RankLevel}", StringComparison.Ordinal) &&
                kvp.Key.EndsWith("Id", StringComparison.Ordinal));

                ulong roleId = Convert.ToUInt64(level.Value);
                IRole role = context.Guild.GetRole(roleId);
                if ((context.Message.Author as IGuildUser).RoleIds.Contains(roleId)) return Task.FromResult(PreconditionResult.FromSuccess());
                return Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? $"{context.Message.Author.Mention}, you must have the {role.Name} role."));
            }
            catch (Exception)
            {
                return Task.FromResult(PreconditionResult.FromError($"No rank is configured at level {RankLevel}!"));
            }
        }
    }
}

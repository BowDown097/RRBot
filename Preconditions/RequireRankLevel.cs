using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireRankLevelAttribute : PreconditionAttribute
    {
        public int RankLevel { get; }

        public RequireRankLevelAttribute(int level) => RankLevel = level;

        // this entire thing is AWFUL. there HAS to be a better way to do this, surely.
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/config").Document("ranks");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            try
            {
                KeyValuePair<string, object> level = snap.ToDictionary().First(kvp => kvp.Key.StartsWith($"level{RankLevel}", StringComparison.Ordinal) &&
                kvp.Key.EndsWith("Id", StringComparison.Ordinal));

                ulong roleId = Convert.ToUInt64(level.Value);
                IRole role = context.Guild.GetRole(roleId);
                return (context.User as IGuildUser).RoleIds.Contains(roleId)
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError($"{context.User.Mention}, you must have the {role.Name} role.");
            }
            catch (Exception)
            {
                return PreconditionResult.FromError($"No rank is configured at level {RankLevel}!");
            }
        }
    }
}

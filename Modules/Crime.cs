using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    public class Crime : ModuleBase<SocketCommandContext>
    {
        public static readonly Random random = new Random();

        [Command("bully")]
        [Summary("Change the nickname of any victim you wish!")]
        [Remarks("``$bully [user] [nickname]``")]
        public async Task<RuntimeResult> Bully(IGuildUser user, [Remainder] string nickname)
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("houseRole", out ulong staffId))
            {
                if (user.Id == Context.User.Id) return CommandResult.FromError($"{Context.User.Mention}, no masochism here!");
                if (user.IsBot || user.RoleIds.Contains(staffId)) return CommandResult.FromError($"{Context.User.Mention}, you cannot bully someone who is a bot or staff member.");
                if (Filters.FUNNY_REGEX.Matches(new string(nickname.Where(char.IsLetter).ToArray()).ToLower()).Count != 0) 
                    return CommandResult.FromError($"{Context.User.Mention}, you cannot bully someone to the funny word.");
                if (nickname.Length > 32) return CommandResult.FromError($"{Context.User.Mention}, the bully nickname is longer than the maximum accepted length.");

                await user.ModifyAsync(props => { props.Nickname = nickname; });
                await Program.logger.Custom_UserBullied(user, Context.User, nickname);
                await ReplyAsync($"**{Context.User.ToString()}** has **BULLIED** **{user.ToString()}** to ``{nickname}``!");
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("This server's staff role has yet to be set.");
        }

        [Command("loot")]
        [Summary("Loot some locations.")]
        [Remarks("``$loot``")]
        [RequireCooldown("lootCooldown", "you cannot loot for {0}.")]
        public async Task<RuntimeResult> Loot()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            float cash = snap.GetValue<float>("cash");

            if (random.Next(10) > 7)
            {
                float lostCash = (float)random.NextDouble(69, 691);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - lostCash);
                switch (random.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, "There happened to be a cop coming out of the donut shop next door." +
                        $" You had to pay **{lostCash.ToString("C2")}** in fines.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, "The manager gave no fucks and beat the **SHIT** out of you." +
                        $" You lost **{lostCash.ToString("C2")}** paying for face stitches.");
                        break;
                }
            }
            else
            {
                float moneyLooted = (float)random.NextDouble(69, 551);
                switch (random.Next(3))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, "You joined your local BLM protest, looted a Footlocker, and sold what you got." +
                        $" You earned **{moneyLooted.ToString("C2")}**.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"That mall had a lot of shit! You earned **{moneyLooted.ToString("C2")}**.");
                        break;
                    case 2:
                        moneyLooted /= 10;
                        await Context.User.NotifyAsync(Context.Channel, "You stole from a gas station because you're a fucking idiot." +
                        $" You earned **{moneyLooted.ToString("C2")}**, basically nothing.");
                        break;
                }

                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + moneyLooted);
            }

            if (random.Next(20) == 1)
            {
                string item = await Items.RandomItem(Context.User as IGuildUser, random);
                if (!string.IsNullOrEmpty(item))
                {
                    await Items.RewardItem(Context.User as IGuildUser, item);
                    await ReplyAsync($"Well I'll be damned! You also got yourself a {item}! Check out ``$module tasks`` to see how you can use it.");
                }
            }

            await doc.SetAsync(new { lootCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600) }, SetOptions.MergeAll);
            return CommandResult.FromSuccess();
        }

        [Alias("strugglesnuggle")]
        [Command("rape")]
        [Summary("Go out on the prowl for some ass!")]
        [Remarks("``$rape [user]``")]
        [RequireCash]
        [RequireCooldown("rapeCooldown", "you cannot rape for {0}.")]
        public async Task<RuntimeResult> Rape(IGuildUser user)
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");
            if (user.Id == Context.User.Id) return CommandResult.FromError($"{Context.User.Mention}, how are you supposed to rape yourself?");

            CollectionReference users = Program.database.Collection($"servers/{Context.Guild.Id}/users");
            DocumentReference aDoc = users.Document(Context.User.Id.ToString());
            DocumentSnapshot aSnap = await aDoc.GetSnapshotAsync();
            if (aSnap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            float aCash = aSnap.GetValue<float>("cash");

            DocumentSnapshot tSnap = await users.Document(user.Id.ToString()).GetSnapshotAsync();
            float tCash = tSnap.GetValue<float>("cash");
            if (tCash > 0)
            {
                double rapePercent = random.NextDouble(5, 9);
                if (random.Next(10) > 4)
                {
                    float repairs = (float)(tCash / 100.0 * rapePercent);
                    await CashSystem.SetCash(user, Context.Channel, tCash - repairs);
                    await Context.User.NotifyAsync(Context.Channel, $"You DEMOLISHED **{user.ToString()}**'s asshole! They just paid **{repairs.ToString("C2")}** in asshole repairs.");
                }
                else
                {
                    float repairs = (float)(aCash / 100.0 * rapePercent);
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, aCash - repairs);
                    await Context.User.NotifyAsync(Context.Channel, $"You just got COUNTER-RAPED by **{user.ToString()}**! You just paid **{repairs.ToString("C2")}** in asshole repairs.");
                }

                await aDoc.SetAsync(new { rapeCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600) }, SetOptions.MergeAll);
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{Context.User.Mention} Jesus man, talk about kicking them while they're down! **{user.ToString()}** is broke! Have some decency.");
        }

        /*
        [Command("rob")]
        [Summary("Rob another user for some money (if you don't suck).")]
        [Remarks("``$rob [user] [money]")]
        [RequireCash]
        [RequireCooldown("robCooldown", "you cannot rob someone for {0}.")]
        public async Task<RuntimeResult> Rob(IGuildUser user, [Remainder] string amountText)
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");

            DocumentReference aDoc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot aSnap = await aDoc.GetSnapshotAsync();
            float aCash = aSnap.GetValue<float>("cash");

            DocumentReference tDoc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(user.Id.ToString());
            DocumentSnapshot tSnap = await tDoc.GetSnapshotAsync();
            float tCash = tSnap.GetValue<float>("cash");
            if (tCash < 0) return CommandResult.FromError($"{Context.User.Mention}, they're broke!");

            float amount = -1f;
            if (!float.TryParse(amountText, out amount))
            {
                if (amountText.Equals("all", StringComparison.OrdinalIgnoreCase))
                    amount = tCash;
                else
                    return CommandResult.FromError($"{Context.User.Mention}, you have specified an invalid amount.");
            }

            if (amount <= 0) return CommandResult.FromError($"{Context.User.Mention}, you can't rob for negative or no money!");
            if (tCash < amount) return CommandResult.FromError($"{Context.User.Mention}, they don't have ${amount}!");

            Random random = new Random();

            return CommandResult.FromSuccess();     
        }
        */

        [Alias("slavelabor", "labor")]
        [Command("slavery")]
        [Summary("Get some slave labor goin'.")]
        [Remarks("``$slavery``")]
        [RequireCooldown("slaveryCooldown", "the slaves will die if you keep going like this! You should wait {0}.")]
        [RequireRankLevel(2)]
        public async Task<RuntimeResult> Slavery()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            float cash = snap.GetValue<float>("cash");

            if (random.Next(10) > 7)
            {
                float lostCash = (float)random.NextDouble(69, 691);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - lostCash);
                switch (random.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, "Some fucker ratted you out and the police showed up." +
                        $" Thankfully, they're corrupt and you were able to sauce them **{lostCash.ToString("C2")}** to fuck off. Thank the lord.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"A slave got away and yoinked **{lostCash.ToString("C2")}** from you. Sad day.");
                        break;
                }
            }
            else
            {
                float moneyEarned = (float)random.NextDouble(69, 551);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + moneyEarned);
                switch (random.Next(3))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, "You got loads of newfags to tirelessly mine ender chests on the Oldest Anarchy Server in Minecraft." +
                        $" You made **{moneyEarned.ToString("C2")}** selling the newfound millions of obsidian to an interested party.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, "The innocent Uyghur children working in your labor factory did an especially good job making shoes in the past hour." +
                        $" You made **{moneyEarned.ToString("C2")}** from all of them, and lost only like 2 cents paying them their wages.");
                        break;
                    case 2:
                        await Context.User.NotifyAsync(Context.Channel, $"This cotton is BUSSIN! The Confederacy is proud. You have been awarded **{moneyEarned.ToString("C2")}**.");
                        break;
                }
            }

            if (random.Next(20) == 1)
            {
                string item = await Items.RandomItem(Context.User as IGuildUser, random);
                if (!string.IsNullOrEmpty(item))
                {
                    await Items.RewardItem(Context.User as IGuildUser, item);
                    await ReplyAsync($"Well I'll be damned! You also got yourself a {item}! Check out ``$module tasks`` to see how you can use it.");
                }
            }

            await doc.SetAsync(new { slaveryCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600) }, SetOptions.MergeAll);
            return CommandResult.FromSuccess();
        }

        [Command("whore")]
        [Summary("Sell your body for quick cash.")]
        [Remarks("``$whore``")]
        [RequireCooldown("whoreCooldown", "you cannot whore yourself out for {0}.")]
        [RequireRankLevel(1)]
        public async Task<RuntimeResult> Whore()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            float cash = snap.GetValue<float>("cash");

            if (random.Next(10) > 7)
            {
                float lostCash = (float)random.NextDouble(69, 691);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - lostCash);
                switch (random.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, $"You were too ugly and nobody wanted you. You lost **{lostCash.ToString("C2")}** buying clothes for the night.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"You didn't give good enough head to the cop! You had to pay **{lostCash.ToString("C2")}** in fines.");
                        break;
                }
            }
            else
            {
                float moneyWhored = (float)random.NextDouble(69, 551);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + moneyWhored);
                switch (random.Next(3))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, $"You went to the club and some weird fat dude sauced you **{moneyWhored.ToString("C2")}**.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"The dude you fucked looked super shady, but he did pay up. You earned **{moneyWhored.ToString("C2")}**.");
                        break;
                    case 2:
                        await Context.User.NotifyAsync(Context.Channel, $"You found the Chad Thundercock himself! **{moneyWhored.ToString("C2")}** and some amazing sex." +
                        $" What a great night.");
                        break;
                }
            }

            await doc.SetAsync(new { whoreCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600) }, SetOptions.MergeAll);
            return CommandResult.FromSuccess();
        }
    }
}

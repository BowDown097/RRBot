using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    public class Crime : ModuleBase<SocketCommandContext>
    {
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
                if (Global.niggerRegex.Matches(new string(nickname.Where(char.IsLetter).ToArray()).ToLower()).Count != 0) 
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
        public async Task Loot()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            float cash = snap.GetValue<float>("cash");
            Random random = new Random();

            if (random.Next(10) > 7)
            {
                float lostCash = (float)(69 + (690 - 69) * random.NextDouble()); // lose between $69-690
                await CashSystem.SetCash(Context.User as IGuildUser, cash - lostCash);
                switch (random.Next(2))
                {
                    case 0:
                        await ReplyAsync($"{Context.User.Mention}, there happened to be a cop coming out of the donut shop next door." + 
                        $" You had to pay **${string.Format("{0:0.00}", lostCash)}** in fines.");
                        break;
                    case 1:
                        await ReplyAsync($"{Context.User.Mention}, the manager gave no fucks and beat the **SHIT** out of you." + 
                        $" You lost **${string.Format("{0:0.00}", lostCash)}** paying for face stitches.");
                        break;
                }
            }
            else
            {
                float moneyLooted = (float)(69 + (550 - 69) * random.NextDouble()); // gain between $69-550
                switch (random.Next(3))
                {
                    case 0:
                        await ReplyAsync($"{Context.User.Mention}, you joined your local BLM protest, looted a Footlocker, and sold what you got." + 
                        $" You earned **${string.Format("{0:0.00}", moneyLooted)}**.");
                        break;
                    case 1:
                        await ReplyAsync($"{Context.User.Mention}, that mall had a lot of shit! You earned **${string.Format("{0:0.00}", moneyLooted)}**.");
                        break;
                    case 2:
                        moneyLooted /= 10;
                        await ReplyAsync($"{Context.User.Mention}, you stole from a gas station because you're a fucking idiot." + 
                        $" You earned **${string.Format("{0:0.00}", moneyLooted)}**, basically nothing.");
                        break;
                }

                await CashSystem.SetCash(Context.User as IGuildUser, cash + moneyLooted);
            }

            if (random.Next(25) == 1)
            {
                string item = await CashSystem.RandomItem(Context.User as IGuildUser, random);
                if (!string.IsNullOrEmpty(item))
                {
                    await CashSystem.RewardItem(Context.User as IGuildUser, item);
                    await ReplyAsync($"Well I'll be damned! You also got yourself a {item}! Try going for a ``$mine``.");
                }
            }

            await doc.SetAsync(new { lootCooldown = Global.UnixTime(3600) }, SetOptions.MergeAll);
        }

        [Alias("strugglesnuggle")]
        [Command("rape")]
        [Summary("Go out on the prowl for some ass!")]
        [Remarks("``$rape [user]``")]
        [RequireCash(500f)]
        [RequireCooldown("rapeCooldown", "you cannot rape for {0}.")]
        public async Task<RuntimeResult> Rape(IGuildUser user)
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");
            if (user.Id == Context.User.Id) return CommandResult.FromError($"{Context.User.Mention}, how are you supposed to rape yourself?");

            DocumentReference aDoc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot aSnap = await aDoc.GetSnapshotAsync();
            float aCash = aSnap.GetValue<float>("cash");

            DocumentReference tDoc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(user.Id.ToString());
            DocumentSnapshot tSnap = await tDoc.GetSnapshotAsync();
            float tCash = tSnap.GetValue<float>("cash");
            if (tCash > 0)
            {
                Random random = new Random();
                double rapePercent = 5 + (8 - 5) * random.NextDouble(); // lose/gain between 5-8% depending on outcome
                if (random.Next(10) > 4)
                {
                    float repairs = (float)(tCash / 100.0 * rapePercent);
                    await CashSystem.SetCash(user, tCash - repairs);
                    await ReplyAsync($"{Context.User.Mention}, you fucking DEMOLISHED **{user.ToString()}**'s asshole!" + 
                    $" They just paid **${string.Format("{0:0.00}", repairs)}** in asshole repairs.");
                }
                else
                {
                    float repairs = (float)(aCash / 100.0 * rapePercent);
                    await CashSystem.SetCash(Context.User as IGuildUser, aCash - repairs);
                    await ReplyAsync($"{Context.User.Mention}, you just got COUNTER-RAPED by **{user.ToString()}**!" + 
                    $" You just paid **${string.Format("{0:0.00}", repairs)}** in asshole repairs.");
                }

                await aDoc.SetAsync(new { rapeCooldown = Global.UnixTime(3600) }, SetOptions.MergeAll);
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
        public async Task Slavery()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            float cash = snap.GetValue<float>("cash");
            Random random = new Random();

            if (random.Next(10) > 7)
            {
                float lostCash = (float)(69 + (690 - 69) * random.NextDouble()); // lose between $69-690
                await CashSystem.SetCash(Context.User as IGuildUser, cash - lostCash);
                switch (random.Next(2))
                {
                    case 0:
                        await ReplyAsync($"{Context.User.Mention}, some fucker ratted you out and the police showed up." +
                        $" Thankfully, they're corrupt and you were able to sauce them **${string.Format("{0:0.00}", lostCash)}** to fuck off. Thank the lord.");
                        break;
                    case 1:
                        await ReplyAsync($"{Context.User.Mention}, a slave got away and yoinked **${string.Format("{0:0.00}", lostCash)}** from you. Sad day.");
                        break;
                }
            }
            else
            {
                float moneyEarned = (float)(69 + (550 - 69) * random.NextDouble()); // gain between $69-550
                await CashSystem.SetCash(Context.User as IGuildUser, cash + moneyEarned);
                switch (random.Next(3))
                {
                    case 0:
                        await ReplyAsync($"{Context.User.Mention}, you got loads of newfags to tirelessly mine ender chests on the Oldest Anarchy Server in Minecraft." +
                        $" You made **${string.Format("{0:0.00}", moneyEarned)}** selling the newfound millions of obsidian to an interested party.");
                        break;
                    case 1:
                        await ReplyAsync($"{Context.User.Mention}, the innocent Uyghur children working in your labor factory did an especially good job making shoes in the past hour." +
                        $" You made **${string.Format("{0:0.00}", moneyEarned)}** from all of them, and lost only like 2 cents paying them their wages.");
                        break;
                    case 2:
                        await ReplyAsync($"{Context.User.Mention}, this cotton is BUSSIN! The Confederacy is proud." +
                        $" You have been awarded **${string.Format("{0:0.00}", moneyEarned)}**.");
                        break;
                }
            }

            if (random.Next(25) == 1)
            {
                string item = await CashSystem.RandomItem(Context.User as IGuildUser, random);
                if (!string.IsNullOrEmpty(item))
                {
                    await CashSystem.RewardItem(Context.User as IGuildUser, item);
                    await ReplyAsync($"Well I'll be damned! You also got yourself a {item}! Try going for a ``$mine``.");
                }
            }

            await doc.SetAsync(new { slaveryCooldown = Global.UnixTime(3600) }, SetOptions.MergeAll);
        }

        [Command("whore")]
        [Summary("Sell your body for quick cash.")]
        [Remarks("``$whore``")]
        [RequireCooldown("whoreCooldown", "you cannot whore yourself out for {0}.")]
        [RequireRankLevel(1)]
        public async Task Whore()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            float cash = snap.GetValue<float>("cash");
            Random random = new Random();

            if (random.Next(10) > 7)
            {
                float lostCash = (float)(69 + (690 - 69) * random.NextDouble()); // lose between $69-690
                await CashSystem.SetCash(Context.User as IGuildUser, cash - lostCash);
                switch (random.Next(2))
                {
                    case 0:
                        await ReplyAsync($"{Context.User.Mention}, you were too ugly and nobody wanted you." + 
                        $" You lost **${string.Format("{0:0.00}", lostCash)}** buying clothes for the night.");
                        break;
                    case 1:
                        await ReplyAsync($"{Context.User.Mention}, you didn't give good enough head to the cop! You had to pay **${string.Format("{0:0.00}", lostCash)}** in fines.");
                        break;
                }
            }
            else
            {
                float moneyWhored = (float)(69 + (550 - 69) * random.NextDouble()); // gain between $69-550
                await CashSystem.SetCash(Context.User as IGuildUser, cash + moneyWhored);
                switch (random.Next(3))
                {
                    case 0:
                        await ReplyAsync($"{Context.User.Mention}, you went to the club and some weird fat dude sauced you **${string.Format("{0:0.00}", moneyWhored)}**.");
                        break;
                    case 1:
                        await ReplyAsync($"{Context.User.Mention}, the dude you fucked looked super shady, but he did pay up. You earned **${string.Format("{0:0.00}", moneyWhored)}**.");
                        break;
                    case 2:
                        await ReplyAsync($"{Context.User.Mention}, you found the Chad Thundercock himself! **${string.Format("{0:0.00}", moneyWhored)}** and some amazing sex." + 
                        $" What a great night.");
                        break;
                }
            }

            await doc.SetAsync(new { whoreCooldown = Global.UnixTime(3600) }, SetOptions.MergeAll);
        }
    }
}

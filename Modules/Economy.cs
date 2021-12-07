namespace RRBot.Modules
{
    [Summary("This is the hub for checking and managing your economy stuff. Wanna know how much cash you have? Or what items you have? Or do you want to check out le shoppe? It's all here.")]
    public class Economy : ModuleBase<SocketCommandContext>
    {
        public static readonly string[] CMDS_WITH_COOLDOWN = { "Deal", "Loot", "Rape", "Rob", "Slavery", "Whore", "Bully",
            "Chop", "Dig", "Farm", "Fish", "Hunt", "Mine", "Support", "Hack" };

        private void AddBackUserSettings(DbUser user, double btc, double doge, double eth, double ltc, double xrp,
            bool dmNotifs, bool noReplyPings, Dictionary<string, string> stats, long whoreCd,
            long slaveryCd, long rapeCd, long lootCd, long dealCd, long bullyCd, long mineCd, long huntCd, long farmCd,
            long digCd, long chopCd, long supportCd)
        {
            user.BTC = btc;
            user.DOGE = doge;
            user.ETH = eth;
            user.LTC = ltc;
            user.XRP = xrp;
            user.DMNotifs = dmNotifs;
            user.NoReplyPings = noReplyPings;
            user.Stats = stats;
            user.WhoreCooldown = whoreCd;
            user.SlaveryCooldown = slaveryCd;
            user.RapeCooldown = rapeCd;
            user.LootCooldown = lootCd;
            user.DealCooldown = dealCd;
            user.BullyCooldown = bullyCd;
            user.MineCooldown = mineCd;
            user.HuntCooldown = huntCd;
            user.FarmCooldown = farmCd;
            user.DigCooldown = digCd;
            user.ChopCooldown = chopCd;
            user.SupportCooldown = supportCd;
        }

        [Alias("bal", "cash")]
        [Command("balance")]
        [Summary("Check your own or someone else's balance.")]
        [Remarks("$balance <user>")]
        public async Task<RuntimeResult> Balance(IGuildUser user = null)
        {
            if (user?.IsBot == true)
                return CommandResult.FromError("Nope.");

            ulong userId = user == null ? Context.User.Id : user.Id;
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
            if (dbUser.Cash < 0.01)
                return CommandResult.FromError(user == null ? "You're broke!" : $"**{user.Sanitize()}** is broke!");

            if (user == null)
                await Context.User.NotifyAsync(Context.Channel, $"You have **{dbUser.Cash:C2}**.");
            else
                await ReplyAsync($"**{user.Sanitize()}** has **{dbUser.Cash:C2}**.");

            return CommandResult.FromSuccess();
        }

        [Alias("purchase")]
        [Command("buy")]
        [Summary("Buy an item or perk from the shop.")]
        [Remarks("$buy [item]")]
        public async Task<RuntimeResult> Buy([Remainder] string item)
        {
            if (ItemSystem.items.Any(i => i == item))
                return await ItemSystem.BuyItem(item, Context.User, Context.Guild, Context.Channel);
            else if (ItemSystem.perks.Any(perk => perk.name == item))
                return await ItemSystem.BuyPerk(item, Context.User, Context.Guild, Context.Channel);
            else
                return CommandResult.FromError($"**{item}** is not a valid item or perk!\n*Tip: This command is case sensitive.*");
        }

        [Alias("cd")]
        [Command("cooldowns")]
        [Summary("Check your own or someone else's crime cooldowns.")]
        [Remarks("$cooldowns <user>")]
        public async Task Cooldowns(IGuildUser user = null)
        {
            ulong userId = user == null ? Context.User.Id : user.Id;
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
            StringBuilder description = new();
            double mult = dbUser.Perks.ContainsKey("Speed Demon") ? 0.85 : 1;

            foreach (string cmd in CMDS_WITH_COOLDOWN)
            {
                long cooldown = (long)dbUser[$"{cmd}Cooldown"];
                long fullCd = (long)((cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) * mult);
                if (fullCd > 0L)
                    description.AppendLine($"**{cmd}**: {TimeSpan.FromSeconds(fullCd).FormatCompound()}");
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(user == null ? "Cooldowns" : $"{user.Sanitize()}'s Cooldowns")
                .WithDescription(description.Length > 0 ? description.ToString() : "None");
            await ReplyAsync(embed: embed.Build());
        }

        [Alias("sell")]
        [Command("discard")]
        [Summary("Discard an item or the Pacifist perk.")]
        [Remarks("$discard [item]")]
        public async Task<RuntimeResult> Discard([Remainder] string item)
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            if (user.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            if (item == "Pacifist")
            {
                if (!user.Perks.ContainsKey("Pacifist"))
                    return CommandResult.FromError("You do not have the Pacifist perk!");

                user.Perks.Remove("Pacifist");
                user.PacifistCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(259200);
                await Context.User.NotifyAsync(Context.Channel, "You discarded your Pacifist perk. If you wish to buy it again, you will have to wait 3 days.");
            }
            else if (user.Items.Remove(item))
            {
                double price = ItemSystem.ComputeItemPrice(item) / 1.5;
                await user.SetCash(Context.User, user.Cash + price);
                await Context.User.NotifyAsync(Context.Channel, $"You sold your {item} to some dude for **{price:C2}**.");
            }
            else
            {
                return CommandResult.FromError($"You do not have a(n) {item}!" +
                    "\n*Tip: This command is case sensitive and does not accept perks other than Pacifist.*");
            }

            return CommandResult.FromSuccess();
        }

        [Command("items")]
        [Summary("Check your own or someone else's items.")]
        [Remarks("$items <user>")]
        [RequireItem]
        public async Task GetItems(IGuildUser user = null)
        {
            ulong userId = user == null ? Context.User.Id : user.Id;
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(user == null ? "Items" : $"{user.Sanitize()}'s Items")
                .WithDescription(string.Join(", ", dbUser.Items));
            await ReplyAsync(embed: embed.Build());
        }

        [Alias("lb")]
        [Command("leaderboard")]
        [Summary("Check the leaderboard for cash or for a specific currency.")]
        [Remarks("$leaderboard <currency>")]
        public async Task<RuntimeResult> Leaderboard(string currency = "cash")
        {
            string cUp = currency.Equals("cash", StringComparison.OrdinalIgnoreCase) ? "Cash" : currency.ToUpper();
            if (!cUp.In("Cash", "BTC", "DOGE", "ETH", "LTC", "XRP"))
                return CommandResult.FromError($"**{currency}** is not a currently accepted currency!");

            double cryptoValue = cUp != "Cash" ? await Investments.QueryCryptoValue(cUp) : 0;
            QuerySnapshot users = await Program.database.Collection($"servers/{Context.Guild.Id}/users")
                .OrderByDescending(cUp).GetSnapshotAsync();
            StringBuilder lb = new();
            int processedUsers = 0, failedUsers = 0;
            foreach (DocumentSnapshot doc in users.Documents)
            {
                if (processedUsers == 10)
                    break;

                SocketGuildUser guildUser = Context.Guild.GetUser(Convert.ToUInt64(doc.Id));
                if (guildUser == null)
                {
                    failedUsers++;
                    continue;
                }

                DbUser user = await DbUser.GetById(Context.Guild.Id, guildUser.Id);
                if (user.Perks.ContainsKey("Pacifist"))
                {
                    failedUsers++;
                    continue;
                }

                double val = (double)user[cUp];
                if (val < Constants.INVESTMENT_MIN_AMOUNT)
                    break;

                if (cUp == "Cash")
                    lb.AppendLine($"{processedUsers + 1}: **{guildUser.Sanitize()}**: {val:C2}");
                else
                    lb.AppendLine($"{processedUsers + 1}: **{guildUser.Sanitize()}**: {val:0.####} ({cryptoValue * val:C2})");

                processedUsers++;
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"{cUp} Leaderboard")
                .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
            ComponentBuilder component = new ComponentBuilder()
                .WithButton("Back", "dddd", disabled: true)
                .WithButton("Next", $"lbnext-{Context.User.Id}-{cUp}-11-20-{failedUsers}-False", disabled: processedUsers != 10 || users.Documents.Count < 11);
            await ReplyAsync(embed: embed.Build(), component: component.Build());
            return CommandResult.FromSuccess();
        }

        [Command("perks")]
        [Summary("View info about your currently active perks.")]
        [Remarks("$perks")]
        [RequirePerk]
        public async Task Perks()
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            StringBuilder perksBuilder = new();
            foreach (KeyValuePair<string, long> kvp in user.Perks.OrderBy(p => p.Key))
            {
                if (kvp.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && kvp.Key != "Pacifist")
                    return;

                Perk perk = Array.Find(ItemSystem.perks, p => p.name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                perksBuilder.AppendLine($"**{perk.name}**: {perk.description}" +
                    $"\nTime Left: {(perk.name != "Pacifist" ? TimeSpan.FromSeconds(kvp.Value - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound() : "Indefinite")}");
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Perks")
                .WithDescription(perksBuilder.Length > 0 ? perksBuilder.ToString() : "None");
            await ReplyAsync(embed: embed.Build());
        }

        [Alias("roles")]
        [Command("ranks")]
        [Summary("View all the ranks and their costs.")]
        [Remarks("$ranks")]
        public async Task Ranks()
        {
            DbConfigRanks ranks = await DbConfigRanks.GetById(Context.Guild.Id);
            StringBuilder description = new();
            foreach (KeyValuePair<string, double> kvp in ranks.Costs.OrderBy(kvp => int.Parse(kvp.Key)))
            {
                SocketRole role = Context.Guild.GetRole(ranks.Ids[kvp.Key]);
                if (role == null) continue;
                description.AppendLine($"**{role.Name}**: {kvp.Value:C2}");
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Available Ranks")
                .WithDescription(description.Length > 0 ? description.ToString() : "None");
            await ReplyAsync(embed: embed.Build());
        }

        [Alias("give", "transfer")]
        [Command("sauce")]
        [Summary("Sauce someone some cash.")]
        [Remarks("$sauce [user] [amount]")]
        public async Task<RuntimeResult> Sauce(IGuildUser user, double amount)
        {
            if (amount < Constants.TRANSACTION_MIN || double.IsNaN(amount))
                return CommandResult.FromError($"You need to sauce at least {Constants.TRANSACTION_MIN:C2}.");
            if (Context.User == user)
                return CommandResult.FromError("You can't sauce yourself money. Don't even know how you would.");
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
            if (author.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
            if (author.Cash < amount)
                return CommandResult.FromError("You do not have that much money!");

            await author.SetCash(Context.User, author.Cash - amount);
            await target.SetCash(user as SocketUser, target.Cash + amount);

            await Context.User.NotifyAsync(Context.Channel, $"You sauced **{user.Sanitize()}** {amount:C2}.");
            return CommandResult.FromSuccess();
        }

        [Command("shop")]
        [Summary("Check out what's available for purchase in the shop.")]
        [Remarks("$shop <items|perks>")]
        public async Task Shop(string category = "")
        {
            StringBuilder items = new();
            StringBuilder perks = new();

            foreach (string item in ItemSystem.items)
            {
                double price = ItemSystem.ComputeItemPrice(item);
                items.AppendLine($"**{item}**: {price:C2}");
            }

            foreach (Perk perk in ItemSystem.perks)
                perks.AppendLine($"**{perk.name}**: {perk.description}\nDuration: {TimeSpan.FromSeconds(perk.duration).FormatCompound()}\nPrice: {perk.price:C2}");

            EmbedBuilder itemsEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("⛏️Items⛏️️")
                .WithDescription(items.ToString());
            EmbedBuilder perksEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("️️🧪Perks🧪")
                .WithDescription(perks.ToString());

            if (string.IsNullOrWhiteSpace(category))
            {
                await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: itemsEmbed.Build());
                await ReplyAsync(embed: perksEmbed.Build());
            }
            else if (category.Equals("items", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: itemsEmbed.Build());
            }
            else if (category.Equals("perks", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: perksEmbed.Build());
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention}, **{category}** is not a valid category!");
            }
        }

        [Alias("kms", "selfend")]
        [Command("suicide")]
        [Summary("Kill yourself.")]
        [Remarks("$suicide")]
        public async Task<RuntimeResult> Suicide()
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            DbUser temp = user;
            if (user.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            switch (RandomUtil.Next(4))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel, "You attempted to hang yourself, but the rope snapped. You did not die.");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel, "You shot yourself, but somehow the bullet didn't kill you. Lucky or unlucky?");
                    break;
                case 2:
                    await Context.User.NotifyAsync(Context.Channel, "​DAMN that shotgun made a fucking mess out of you! You're DEAD DEAD, and lost everything.");
                    await user.Reference.DeleteAsync();
                    await user.SetCash(Context.User, 0);
                    AddBackUserSettings(user, temp.BTC, temp.DOGE, temp.ETH, temp.LTC, temp.XRP, temp.DMNotifs,
                        temp.NoReplyPings, temp.Stats, temp.WhoreCooldown, temp.SlaveryCooldown, temp.RapeCooldown,
                        temp.LootCooldown, temp.DealCooldown, temp.BullyCooldown, temp.MineCooldown, temp.HuntCooldown,
                        temp.FarmCooldown, temp.DigCooldown, temp.ChopCooldown, temp.SupportCooldown);
                    break;
                case 3:
                    await Context.User.NotifyAsync(Context.Channel, "It was quite a struggle, but the noose put you out of your misery. You lost everything.");
                    await user.Reference.DeleteAsync();
                    await user.SetCash(Context.User, 0);
                    AddBackUserSettings(user, temp.BTC, temp.DOGE, temp.ETH, temp.LTC, temp.XRP, temp.DMNotifs,
                        temp.NoReplyPings, temp.Stats, temp.WhoreCooldown, temp.SlaveryCooldown, temp.RapeCooldown,
                        temp.LootCooldown, temp.DealCooldown, temp.BullyCooldown, temp.MineCooldown, temp.HuntCooldown,
                        temp.FarmCooldown, temp.DigCooldown, temp.ChopCooldown, temp.SupportCooldown);
                    break;
            }

            return CommandResult.FromSuccess();
        }
    }
}

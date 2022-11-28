namespace RRBot.Modules;
[Summary("All about that gang shit.")]
public class Gangs : ModuleBase<SocketCommandContext>
{
    [Command("buyvault")]
    [Summary("Buy a vault for your gang.")]
    [RequireCash((double)Constants.GangVaultCost)]
    public async Task<RuntimeResult> BuyVault()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        
        DbGang gang = await MongoManager.FetchGangAsync(user.Gang, Context.Guild.Id);
        if (gang.VaultUnlocked)
            return CommandResult.FromError("Your gang already has a vault!");

        gang.VaultUnlocked = true;
        await user.SetCash(Context.User, user.Cash - Constants.GangVaultCost);

        await Context.User.NotifyAsync(Context.Channel, $"Unlocked a vault for your gang for {Constants.GangVaultCost:C2}!");
        await MongoManager.UpdateObjectAsync(gang);
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("creategang")]
    [Summary("Create a gang.")]
    [Remarks("$creategang Vrilerinnen")]
    [RequireCash((double)Constants.GangCreationCost)]
    public async Task<RuntimeResult> CreateGang([Remainder] string name)
    {
        IAsyncCursor<DbGang> cursor = await MongoManager.Gangs.FindAsync(g => g.GuildId == Context.Guild.Id);
        List<DbGang> gangs = await cursor.ToListAsync();
        if (name.Length is <= 2 or > 32 || !Regex.IsMatch(name, "^[a-zA-Z0-9\x20]*$") || await FilterSystem.ContainsFilteredWord(Context.Guild, name))
            return CommandResult.FromError("That gang name is not allowed.");
        if (gangs.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return CommandResult.FromError("There is already a gang with that name.");
        if (gangs.Count == Constants.MaxGangsPerGuild)
            return CommandResult.FromError($"This server has reached the maximum of {Constants.MaxGangsPerGuild} gangs.");
        
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (!string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are already in a gang!");
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
 
        user.Gang = name;
        await user.SetCash(Context.User, user.Cash - Constants.GangCreationCost);

        await MongoManager.Gangs.InsertOneAsync(new DbGang
        {
            GuildId = Context.Guild.Id,
            Leader = Context.User.Id,
            Members = new Dictionary<ulong, string> {{Context.User.Id, Constants.GangPositions[0]}},
            Name = name
        });

        await Context.User.NotifyAsync(Context.Channel, $"Created a gang with the name **{name}** for {Constants.GangCreationCost:C2}.");
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("deposit")]
    [Summary("Deposit cash into your gang's vault.")]
    [Remarks("$deposit 6969.69")]
    public async Task<RuntimeResult> Deposit(decimal amount)
    {
        if (amount < Constants.TransactionMin)
            return CommandResult.FromError($"You need to deposit at least {Constants.TransactionMin:C2}.");
        
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        if (user.Cash < amount)
            return CommandResult.FromError("You do not have that much money!");
        
        DbGang gang = await MongoManager.FetchGangAsync(user.Gang, Context.Guild.Id);
        if (!gang.VaultUnlocked)
            return CommandResult.FromError("Your gang does not have a vault!");

        decimal finalAmount = amount / 100.0m * (100 - Constants.VaultTaxPercent);
        gang.VaultBalance += finalAmount;
        await user.SetCash(Context.User, user.Cash - amount);

        await Context.User.NotifyAsync(Context.Channel, $"Deposited **{finalAmount:C2}** into your gang's vault ({Constants.VaultTaxPercent}% tax).");
        await MongoManager.UpdateObjectAsync(gang);
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Alias("destroygang")]
    [Command("disband")]
    [Summary("Disband your gang.")]
    public async Task<RuntimeResult> Disband()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        
        DbGang gang = await MongoManager.FetchGangAsync(user.Gang, Context.Guild.Id);
        if (gang.Leader != Context.User.Id)
            return CommandResult.FromError("You are not the leader of your gang!");

        await MongoManager.DeleteObjectAsync(gang);
        MemoryCache.Default.Remove($"gang-{Context.Guild.Id}-{user.Gang.ToLower()}");

        user.Gang = null;
        foreach (KeyValuePair<ulong, string> kvp in gang.Members.Where(m => m.Key != user.UserId))
        {
            DbUser member = await MongoManager.FetchUserAsync(kvp.Key, Context.Guild.Id);
            member.Gang = null;
            await MongoManager.UpdateObjectAsync(member);
        }

        await Context.User.NotifyAsync(Context.Channel, "Your gang has been disbanded!");
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Alias("ganginfo")]
    [Command("gang")]
    [Summary("View info about your own gang or another.")]
    [Remarks("$gang Sex Havers")]
    public async Task<RuntimeResult> Gang([Remainder] string name = null)
    {
        if (name == null)
        {
            DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
            if (string.IsNullOrWhiteSpace(user.Gang))
                return CommandResult.FromError("You are not in a gang!");
            name = user.Gang;
        }
        
        DbGang gang = await MongoManager.FetchGangAsync(name, Context.Guild.Id);
        if (gang == null)
            return CommandResult.FromError("There is no gang with that name.");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(gang.Name)
            .RrAddField("Leader", Context.Guild.GetUser(gang.Leader).Sanitize());

        foreach (string position in Constants.GangPositions.Take(1..))
        {
            var posMems = gang.Members.Where(m => m.Value == position);
            IEnumerable<string> posMemNames = posMems.Select(m => Context.Guild.GetUser(m.Key).Sanitize());
            embed.RrAddField($"{position}s", string.Join('\n', posMemNames));
        }

        if (gang.VaultBalance >= 0.01m)
            embed.RrAddField("Vault Balance", gang.VaultBalance.ToString("C2"));
        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Command("ganglb")]
    [Summary("Leaderboard for gang vaults.")]
    public async Task GangLb()
    {
        SortDefinition<DbGang> sort = Builders<DbGang>.Sort.Descending(g => g.VaultBalance);
        IAsyncCursor<DbGang> cursor = await MongoManager.Gangs.FindAsync(u => u.GuildId == Context.Guild.Id,
            new FindOptions<DbGang> { Sort = sort });
        List<DbGang> gangs = await cursor.ToListAsync();

        StringBuilder lb = new("*Note: The leaderboard updates every 10 minutes, so stuff may not be up to date.*\n");
        int processedGangs = 0;
        foreach (DbGang gang in gangs)
        {
            if (processedGangs == 10 || gang.VaultBalance < Constants.InvestmentMinAmount)
                break;
            lb.AppendLine($"{processedGangs + 1}: **{Format.Sanitize(gang.Name).Replace("\\:", ":").Replace("\\/", "/").Replace("\\.", ".")}**: {gang.VaultBalance:C2}");
            processedGangs++;
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Gang Leaderboard")
            .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
        ComponentBuilder component = new ComponentBuilder()
            .WithButton("Back", "dddd", disabled: true)
            .WithButton("Next", $"ganglbnext-{Context.User.Id}-11-20", disabled: processedGangs != 10 || gangs.Count < 11);
        await ReplyAsync(embed: embed.Build(), components: component.Build());
    }

    [Command("invite")]
    [Summary("Invite a member to your gang (if it is private).")]
    [Remarks("$invite Barcode3")]
    public async Task<RuntimeResult> Invite([Remainder] IGuildUser user)
    {
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("You got any brain cells in that head of yours?");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(author.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (!string.IsNullOrWhiteSpace(target.Gang))
            return CommandResult.FromError($"**{user.Sanitize()}** is already in a gang!");
        
        DbGang gang = await MongoManager.FetchGangAsync(author.Gang, Context.Guild.Id);
        if (gang.IsPublic)
            return CommandResult.FromError("No need to invite people! Your gang is public!");
        if (gang.Members.Count == Constants.GangMaxMembers)
            return CommandResult.FromError($"Your gang has already reached the maximum of {Constants.GangMaxMembers} members.");
        if (Array.IndexOf(Constants.GangPositions, gang.Members[Context.User.Id]) > 1)
            return CommandResult.FromError($"You need to be a(n) {Constants.GangPositions[1]} or higher in your gang.");

        target.PendingGangInvites.Add(gang.Name);
        await Context.User.NotifyAsync(Context.Channel, $"Invited **{user.Sanitize()}** to your gang.");
        await MongoManager.UpdateObjectAsync(target);
        return CommandResult.FromSuccess();
    }

    [Alias("joingang")]
    [Command("join")]
    [Summary("Join a gang.")]
    [Remarks("$join Comedy Central")]
    public async Task<RuntimeResult> JoinGang([Remainder] string name)
    {
        DbGang gang = await MongoManager.FetchGangAsync(name, Context.Guild.Id);
        if (gang == null)
            return CommandResult.FromError("There is no gang with that name.");

        if (gang.Members.Count == Constants.GangMaxMembers)
            return CommandResult.FromError($"That gang has already reached the maximum of {Constants.GangMaxMembers} members.");
        
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (!string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are already in a gang!");

        if (!gang.IsPublic && !user.PendingGangInvites.Contains(gang.Name))
            return CommandResult.FromError($"That gang is private! You will need to be invited by a(n) {Constants.GangPositions[1]} or above.");

        gang.Members[Context.User.Id] = Constants.GangPositions.Last();
        user.Gang = gang.Name;
        user.PendingGangInvites.Remove(gang.Name);

        await Context.User.NotifyAsync(Context.Channel, $"You are now a member of **{gang.Name}**.");
        await MongoManager.UpdateObjectAsync(gang);
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("kickgangmember")]
    [Summary("Kick a member from your gang.")]
    [Remarks("$kickgangmember Thunderstar")]
    public async Task<RuntimeResult> KickGangMember([Remainder] IGuildUser user)
    {
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("Sorry bro! You'll have to transfer leadership and leave the gang.");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(author.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (author.Gang != target.Gang)
            return CommandResult.FromError("They are not in your gang!");
        
        DbGang gang = await MongoManager.FetchGangAsync(author.Gang, Context.Guild.Id);
        int authorIndex = Array.IndexOf(Constants.GangPositions, gang.Members[Context.User.Id]);
        int targetIndex = Array.IndexOf(Constants.GangPositions, gang.Members[user.Id]);
        if (authorIndex > 1)
            return CommandResult.FromError($"You need to be a(n) {Constants.GangPositions[1]} or higher in your gang.");
        if (authorIndex > targetIndex)
            return CommandResult.FromError($"**{user.Sanitize()}** is in a higher position than you in your gang.");

        gang.Members.Remove(user.Id);
        target.Gang = null;

        await Context.User.NotifyAsync(Context.Channel, $"Kicked **{user.Sanitize()}** from your gang.");
        await MongoManager.UpdateObjectAsync(gang);
        await MongoManager.UpdateObjectAsync(target);
        return CommandResult.FromSuccess();
    }

    [Command("leavegang")]
    [Summary("Leave your current gang.")]
    public async Task<RuntimeResult> LeaveGang()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        
        DbGang gang = await MongoManager.FetchGangAsync(user.Gang, Context.Guild.Id);
        if (gang.Leader == Context.User.Id)
            return CommandResult.FromError("You'll need to transfer leadership first.");

        gang.Members.Remove(Context.User.Id);
        user.Gang = null;

        await Context.User.NotifyAsync(Context.Channel, "You left your gang.");
        await MongoManager.UpdateObjectAsync(gang);
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("setposition")]
    [Summary("Set a member of your gang's position.")]
    [Remarks("$setposition Murumu1 Member")]
    public async Task<RuntimeResult> SetPosition(IGuildUser user, [Remainder] string position)
    {
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("Probably not a good idea to demote yourself.");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        string foundPosition = Array.Find(Constants.GangPositions, p => p.Equals(position, StringComparison.OrdinalIgnoreCase));
        if (foundPosition == null)
            return CommandResult.FromError("That is not a valid gang position!");
        if (foundPosition == Constants.GangPositions[0])
            return CommandResult.FromError("Use $transferleadership.");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(author.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (author.Gang != target.Gang)
            return CommandResult.FromError("They are not in your gang!");
        
        DbGang gang = await MongoManager.FetchGangAsync(author.Gang, Context.Guild.Id);
        if (gang.Leader != Context.User.Id)
            return CommandResult.FromError("You are not the leader of your gang!");
        if (gang.Members[user.Id] == foundPosition)
            return CommandResult.FromError($"They are already a(n) {foundPosition}!");

        gang.Members[user.Id] = foundPosition;
        await Context.User.NotifyAsync(Context.Channel, $"Changed **{user.Sanitize()}** to a(n) {foundPosition}.");
        await MongoManager.UpdateObjectAsync(gang);
        return CommandResult.FromSuccess();
    }

    [Command("togglepublic")]
    [Summary("Toggles the publicity of your gang.")]
    public async Task<RuntimeResult> TogglePublic()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        
        DbGang gang = await MongoManager.FetchGangAsync(user.Gang, Context.Guild.Id);
        if (gang.Leader != Context.User.Id)
            return CommandResult.FromError("You are not the leader of your gang!");

        gang.IsPublic = !gang.IsPublic;
        await Context.User.NotifyAsync(Context.Channel, $"Your gang is {(gang.IsPublic ? "now" : "no longer")} public.");
        await MongoManager.UpdateObjectAsync(gang);
        return CommandResult.FromSuccess();
    }

    [Command("transferleadership")]
    [Summary("Transfer leadership of your gang to another member.")]
    [Remarks("$transferleadership \"Mr. DeeJay\"")]
    public async Task<RuntimeResult> TransferLeadership([Remainder] IGuildUser user)
    {
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("Hey, dumbass, you're already the leader (probably).");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(author.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (author.Gang != target.Gang)
            return CommandResult.FromError("They are not in your gang!");
        
        DbGang gang = await MongoManager.FetchGangAsync(author.Gang, Context.Guild.Id);
        if (gang.Leader != Context.User.Id)
            return CommandResult.FromError("You are not the leader of your gang!");

        gang.Leader = user.Id;
        gang.Members[Context.User.Id] = Constants.GangPositions.Last();
        gang.Members[user.Id] = Constants.GangPositions[0];

        await Context.User.NotifyAsync(Context.Channel, $"Transferred leadership to **{user.Sanitize()}**.");
        await MongoManager.UpdateObjectAsync(gang);
        return CommandResult.FromSuccess();
    }

    [Alias("vb", "vaultbal", "vaultbalance")]
    [Command("vault")]
    [Summary("Check your gang's vault balance.")]
    public async Task<RuntimeResult> VaultBalance()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");

        DbGang gang = await MongoManager.FetchGangAsync(user.Gang, Context.Guild.Id);
        if (!gang.VaultUnlocked)
            return CommandResult.FromError("Your gang does not have a vault!");
        if (gang.VaultBalance < 0.01m)
            return CommandResult.FromError("Your gang is broke!");

        await Context.User.NotifyAsync(Context.Channel, $"Your gang's vault has **{gang.VaultBalance:C2}**.");
        return CommandResult.FromSuccess();
    }

    [Alias("wv")]
    [Command("withdrawvault")]
    [Summary("Withdraw money from your gang's vault.")]
    [Remarks("$withdrawvault 1000000")]
    public async Task<RuntimeResult> WithdrawVault(decimal amount)
    {
        if (amount < Constants.TransactionMin)
            return CommandResult.FromError($"You need to deposit at least {Constants.TransactionMin:C2}.");
        
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        
        DbGang gang = await MongoManager.FetchGangAsync(user.Gang, Context.Guild.Id);
        if (!gang.VaultUnlocked)
            return CommandResult.FromError("Your gang does not have a vault!");
        if (gang.VaultBalance < amount)
            return CommandResult.FromError("Your gang's vault does not have that much money!");

        gang.VaultBalance -= amount;
        await user.SetCash(Context.User, user.Cash + amount);

        await Context.User.NotifyAsync(Context.Channel, $"Withdrew **{amount:C2}** from your gang's vault.");
        await MongoManager.UpdateObjectAsync(gang);
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }
}
namespace RRBot.Modules;
[Summary("All about that gang shit.")]
public class Gangs : ModuleBase<SocketCommandContext>
{
    [Command("buyvault")]
    [Summary("Buy a vault for your gang.")]
    [RequireCash(Constants.GANG_VAULT_COST)]
    public async Task<RuntimeResult> BuyVault()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (gang.VaultUnlocked)
            return CommandResult.FromError("Your gang already has a vault!");

        gang.VaultUnlocked = true;
        await user.SetCash(Context.User, user.Cash - Constants.GANG_VAULT_COST);

        await Context.User.NotifyAsync(Context.Channel, $"Unlocked a vault for your gang for {Constants.GANG_VAULT_COST:C2}!");
        return CommandResult.FromSuccess();
    }

    [Command("creategang")]
    [Summary("Create a gang.")]
    [Remarks("$creategang Vrilerinnen")]
    [RequireCash(Constants.GANG_CREATION_COST)]
    public async Task<RuntimeResult> CreateGang([Remainder] string name)
    {
        QuerySnapshot gangs = await Program.database.Collection($"servers/{Context.Guild.Id}/gangs").GetSnapshotAsync();
        if (name.Length <= 2 || name.Length > 32 || !Regex.IsMatch(name, "^[a-zA-Z0-9\x20]*$") || await FilterSystem.ContainsFilteredWord(Context.Guild, name))
            return CommandResult.FromError("That gang name is not allowed.");
        if (gangs.Any(r => r.Id.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return CommandResult.FromError("There is already a gang with that name.");
        if (gangs.Documents.Count == Constants.MAX_GANGS_PER_GUILD)
            return CommandResult.FromError($"This server has reached the maximum of {Constants.MAX_GANGS_PER_GUILD} gangs.");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (!string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are already in a gang!");
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        user.Gang = name;
        await user.SetCash(Context.User, user.Cash - Constants.GANG_CREATION_COST);

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, name);
        gang.Leader = Context.User.Id;
        gang.Members.Add(Context.User.Id.ToString(), Constants.GANG_POSITIONS[0]);

        await Context.User.NotifyAsync(Context.Channel, $"Created a gang with the name **{name}** for {Constants.GANG_CREATION_COST:C2}.");
        return CommandResult.FromSuccess();
    }

    [Command("deposit")]
    [Summary("Deposit cash into your gang's vault.")]
    [Remarks("$deposit 6969.69")]
    public async Task<RuntimeResult> Deposit(double amount)
    {
        if (amount < Constants.TRANSACTION_MIN || double.IsNaN(amount))
            return CommandResult.FromError($"You need to deposit at least {Constants.TRANSACTION_MIN:C2}.");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        if (user.Cash < amount)
            return CommandResult.FromError("You do not have that much money!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (!gang.VaultUnlocked)
            return CommandResult.FromError("Your gang does not have a vault!");

        double finalAmount = amount / 100.0 * (100 - Constants.VAULT_TAX_PERCENT);
        gang.VaultBalance += finalAmount;
        await user.SetCash(Context.User, user.Cash - amount);
        await Context.User.NotifyAsync(Context.Channel, $"Deposited **{finalAmount:C2}** into your gang's vault ({Constants.VAULT_TAX_PERCENT}% tax).");
        return CommandResult.FromSuccess();
    }

    [Alias("destroygang")]
    [Command("disband")]
    [Summary("Disband your gang.")]
    public async Task<RuntimeResult> Disband()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (gang.Leader != Context.User.Id)
            return CommandResult.FromError("You are not the leader of your gang!");

        await gang.Reference.DeleteAsync();
        MemoryCache.Default.Remove($"gang-{Context.Guild.Id}-{user.Gang.ToLower()}");
        foreach (KeyValuePair<string, string> kvp in gang.Members)
        {
            DbUser dbu = await DbUser.GetById(Context.Guild.Id, Convert.ToUInt64(kvp.Key));
            dbu.Gang = null;
        }

        await Context.User.NotifyAsync(Context.Channel, "Your gang has been disbanded!");
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
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            if (string.IsNullOrWhiteSpace(user.Gang))
                return CommandResult.FromError("You are not in a gang!");
            name = user.Gang;
        }

        QuerySnapshot gangs = await Program.database.Collection($"servers/{Context.Guild.Id}/gangs").GetSnapshotAsync();
        if (!gangs.Any(r => r.Id.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return CommandResult.FromError("There is no gang with that name.");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, name);
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(gang.Name)
            .RRAddField("Leader", Context.Guild.GetUser(gang.Leader).Sanitize());

        foreach (string position in Constants.GANG_POSITIONS.Take(1..))
        {
            var posMems = gang.Members.Where(m => m.Value == position);
            IEnumerable<string> posMemNames = posMems.Select(m => Context.Guild.GetUser(Convert.ToUInt64(m.Key)).Sanitize());
            embed.RRAddField($"{position}s", string.Join('\n', posMemNames));
        }

        if (gang.VaultBalance >= 0.01)
            embed.RRAddField("Vault Balance", gang.VaultBalance.ToString("C2"));
        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Command("ganglb")]
    [Summary("Leaderboard for gang vaults.")]
    public async Task GangLb()
    {
        QuerySnapshot gangs = await Program.database.Collection($"servers/{Context.Guild.Id}/gangs")
            .OrderByDescending("VaultBalance").GetSnapshotAsync();
        StringBuilder lb = new("*Note: The leaderboard updates every 10 minutes, so stuff may not be up to date.*\n");
        int processedGangs = 0;
        foreach (DocumentSnapshot doc in gangs.Documents)
        {
            if (processedGangs == 10)
                break;

            DbGang gang = await DbGang.GetByName(Context.Guild.Id, doc.Id, false);
            if (gang.VaultBalance < Constants.INVESTMENT_MIN_AMOUNT)
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
            .WithButton("Next", $"ganglbnext-{Context.User.Id}-11-20", disabled: processedGangs != 10 || gangs.Documents.Count < 11);
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

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (string.IsNullOrWhiteSpace(author.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (!string.IsNullOrWhiteSpace(target.Gang))
            return CommandResult.FromError($"**{user.Sanitize()}** is already in a gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, author.Gang);
        if (gang.IsPublic)
            return CommandResult.FromError("No need to invite people! Your gang is public!");
        if (gang.Members.Count == Constants.GANG_MAX_MEMBERS)
            return CommandResult.FromError($"Your gang has already reached the maximum of {Constants.GANG_MAX_MEMBERS} members.");
        if (Array.IndexOf(Constants.GANG_POSITIONS, gang.Members[Context.User.Id.ToString()]) > 1)
            return CommandResult.FromError($"You need to be a(n) {Constants.GANG_POSITIONS[1]} or higher in your gang.");

        target.PendingGangInvites.Add(gang.Name);
        await Context.User.NotifyAsync(Context.Channel, $"Invited **{user.Sanitize()}** to your gang.");
        return CommandResult.FromSuccess();
    }

    [Alias("joingang")]
    [Command("join")]
    [Summary("Join a gang.")]
    [Remarks("$join Comedy Central")]
    public async Task<RuntimeResult> JoinGang([Remainder] string name)
    {
        QuerySnapshot gangs = await Program.database.Collection($"servers/{Context.Guild.Id}/gangs").GetSnapshotAsync();
        if (!gangs.Any(r => r.Id.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return CommandResult.FromError("There is no gang with that name.");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, name);
        if (gang.Members.Count == Constants.GANG_MAX_MEMBERS)
            return CommandResult.FromError($"That gang has already reached the maximum of {Constants.GANG_MAX_MEMBERS} members.");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (!string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are already in a gang!");

        if (!gang.IsPublic && !user.PendingGangInvites.Contains(gang.Name))
            return CommandResult.FromError($"That gang is private! You will need to be invited by a(n) {Constants.GANG_POSITIONS[1]} or above.");

        gang.Members[Context.User.Id.ToString()] = Constants.GANG_POSITIONS.Last();
        user.Gang = gang.Name;
        user.PendingGangInvites.Remove(gang.Name);

        await Context.User.NotifyAsync(Context.Channel, $"You are now a member of **{gang.Name}**.");
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

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (string.IsNullOrWhiteSpace(author.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (author.Gang != target.Gang)
            return CommandResult.FromError("They are not in your gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, author.Gang);
        int authorIndex = Array.IndexOf(Constants.GANG_POSITIONS, gang.Members[Context.User.Id.ToString()]);
        int targetIndex = Array.IndexOf(Constants.GANG_POSITIONS, gang.Members[user.Id.ToString()]);
        if (authorIndex > 1)
            return CommandResult.FromError($"You need to be a(n) {Constants.GANG_POSITIONS[1]} or higher in your gang.");
        if (authorIndex > targetIndex)
            return CommandResult.FromError($"**{user.Sanitize()}** is in a higher position than you in your gang.");

        gang.Members.Remove(user.Id.ToString());
        target.Gang = null;

        await Context.User.NotifyAsync(Context.Channel, $"Kicked **{user.Sanitize()}** from your gang.");
        return CommandResult.FromSuccess();
    }

    [Command("leavegang")]
    [Summary("Leave your current gang.")]
    public async Task<RuntimeResult> LeaveGang()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (gang.Leader == Context.User.Id)
            return CommandResult.FromError("You'll need to transfer leadership first.");

        gang.Members.Remove(Context.User.Id.ToString());
        user.Gang = null;

        await Context.User.NotifyAsync(Context.Channel, "You left your gang.");
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

        string foundPosition = Array.Find(Constants.GANG_POSITIONS, p => p.Equals(position, StringComparison.OrdinalIgnoreCase));
        if (foundPosition == null)
            return CommandResult.FromError("That is not a valid gang position!");
        if (foundPosition == Constants.GANG_POSITIONS[0])
            return CommandResult.FromError("Use $transferleadership.");

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (string.IsNullOrWhiteSpace(author.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (author.Gang != target.Gang)
            return CommandResult.FromError("They are not in your gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, author.Gang);
        if (gang.Leader != Context.User.Id)
            return CommandResult.FromError("You are not the leader of your gang!");
        if (gang.Members[user.Id.ToString()] == foundPosition)
            return CommandResult.FromError($"They are already a(n) {foundPosition}!");

        gang.Members[user.Id.ToString()] = foundPosition;
        await Context.User.NotifyAsync(Context.Channel, $"Changed **{user.Sanitize()}** to a(n) {foundPosition}.");
        return CommandResult.FromSuccess();
    }

    [Command("togglepublic")]
    [Summary("Toggles the publicity of your gang.")]
    public async Task<RuntimeResult> TogglePublic()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (gang.Leader != Context.User.Id)
            return CommandResult.FromError("You are not the leader of your gang!");

        gang.IsPublic = !gang.IsPublic;
        await Context.User.NotifyAsync(Context.Channel, $"Your gang is {(gang.IsPublic ? "now" : "no longer")} public.");
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

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (string.IsNullOrWhiteSpace(author.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (author.Gang != target.Gang)
            return CommandResult.FromError("They are not in your gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, author.Gang);
        if (gang.Leader != Context.User.Id)
            return CommandResult.FromError("You are not the leader of your gang!");

        gang.Leader = user.Id;
        gang.Members[Context.User.Id.ToString()] = Constants.GANG_POSITIONS.Last();
        gang.Members[user.Id.ToString()] = Constants.GANG_POSITIONS[0];

        await Context.User.NotifyAsync(Context.Channel, $"Transferred leadership to **{user.Sanitize()}**.");
        return CommandResult.FromSuccess();
    }

    [Alias("vb", "vaultbal", "vaultbalance")]
    [Command("vault")]
    [Summary("Check your gang's vault balance.")]
    public async Task<RuntimeResult> VaultBalance()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (!gang.VaultUnlocked)
            return CommandResult.FromError("Your gang does not have a vault!");
        if (gang.VaultBalance < 0.01)
            return CommandResult.FromError("Your gang is broke!");

        await Context.User.NotifyAsync(Context.Channel, $"Your gang's vault has **{gang.VaultBalance:C2}**.");
        return CommandResult.FromSuccess();
    }

    [Alias("wv")]
    [Command("withdrawvault")]
    [Summary("Withdraw money from your gang's vault.")]
    [Remarks("$withdrawvault 1000000")]
    public async Task<RuntimeResult> WithdrawVault(double amount)
    {
        if (amount < Constants.TRANSACTION_MIN || double.IsNaN(amount))
            return CommandResult.FromError($"You need to deposit at least {Constants.TRANSACTION_MIN:C2}.");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (!gang.VaultUnlocked)
            return CommandResult.FromError("Your gang does not have a vault!");
        if (gang.VaultBalance < amount)
            return CommandResult.FromError("Your gang's vault does not have that much money!");

        gang.VaultBalance -= amount;
        await user.SetCash(Context.User, user.Cash + amount);
        await Context.User.NotifyAsync(Context.Channel, $"Withdrew **{amount:C2}** from your gang's vault.");
        return CommandResult.FromSuccess();
    }
}
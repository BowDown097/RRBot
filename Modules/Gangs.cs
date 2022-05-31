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

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (gang.VaultUnlocked)
            return CommandResult.FromError("Your gang already has a vault!");

        gang.VaultUnlocked = true;
        await Context.User.NotifyAsync(Context.Channel, "Unlocked a vault for your gang!");
        return CommandResult.FromSuccess();
    }

    [Command("creategang")]
    [Summary("Create a gang.")]
    [Remarks("$creategang Vrilerinnen")]
    [RequireCash(Constants.GANG_CREATION_COST)]
    public async Task<RuntimeResult> CreateGang([Remainder] string name)
    {
        QuerySnapshot gangs = await Program.database.Collection($"servers/{Context.Guild.Id}/gangs").GetSnapshotAsync();
        if (name.Length <= 2 || name.Length > 32 || !Regex.IsMatch(name, "^[a-zA-Z0-9]*$") || await FilterSystem.ContainsFilteredWord(Context.Guild, name))
            return CommandResult.FromError("That gang name is not allowed.");
        if (gangs.Any(r => r.Id == name))
            return CommandResult.FromError("There is already a gang with that name.");
        if (gangs.Documents.Count == Constants.MAX_GANGS_PER_GUILD)
            return CommandResult.FromError($"This server has reached the maximum of {Constants.MAX_GANGS_PER_GUILD} gangs.");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, name);
        gang.Leader = Context.User.Id;
        gang.Members.Add(Context.User.Id, Constants.GANG_POSITIONS[0]);

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        await user.SetCash(Context.User, user.Cash - Constants.GANG_CREATION_COST);

        await Context.User.NotifyAsync(Context.Channel, $"Created a gang with the name **{name}** for {Constants.GANG_CREATION_COST:C2}.");
        return CommandResult.FromSuccess();
    }

    [Command("demote")]
    [Summary("Demote a member of your gang.")]
    [Remarks("$demote Murumu1 Member")]
    public async Task<RuntimeResult> DemoteMember(IGuildUser user, [Remainder] string position)
    {
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("You have to use $transferleadership to demote yourself.");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        string foundPosition = Array.Find(Constants.GANG_POSITIONS, p => p.Equals(position, StringComparison.OrdinalIgnoreCase));
        if (foundPosition == null)
            return CommandResult.FromError("That is not a valid gang position!");

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (author.Gang != target.Gang)
            return CommandResult.FromError("They are not in your gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, author.Gang);
        gang.Members[user.Id] = foundPosition;

        await Context.User.NotifyAsync(Context.Channel, $"Demoted **{user.Sanitize()}** to a {foundPosition}.");
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

        await user.SetCash(Context.User, user.Cash - amount);

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (!gang.VaultUnlocked)
            return CommandResult.FromError("Your gang does not have a vault!");

        gang.VaultBalance += amount * 0.95;
        await Context.User.NotifyAsync(Context.Channel, $"Deposited **{amount * 0.95:C2}** into the vault (-{amount * 0.05:C2} from tax).");
        return CommandResult.FromSuccess();
    }

    [Command("disband")]
    [Summary("Disband your gang.")]
    public async Task<RuntimeResult> Disband()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, user.Gang);
        if (gang.Members[Context.User.Id] != Constants.GANG_POSITIONS[0])
            return CommandResult.FromError("You are not the leader of your gang!");

        await gang.Reference.DeleteAsync();
        MemoryCache.Default.Remove($"gang-{Context.Guild.Id}-{user.Gang}");
        await Context.User.NotifyAsync(Context.Channel, "Your gang has been disbanded!");
        return CommandResult.FromSuccess();
    }

    [Command("gang")]
    [Summary("View info about your own gang or another.")]
    [Remarks("$gang Sex Havers")]
    public async Task<RuntimeResult> Gang([Remainder] string name = null)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (name == null && string.IsNullOrWhiteSpace(user.Gang))
            return CommandResult.FromError("You are not in a gang!");
        name = user.Gang;

        QuerySnapshot gangs = await Program.database.Collection($"servers/{Context.Guild.Id}/gangs").GetSnapshotAsync();
        if (!gangs.Any(r => r.Id == name))
            return CommandResult.FromError("There is no gang with that name.");

        DbGang gang = await DbGang.GetByName(Context.Guild.Id, name);
        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle(gang.Reference.Id)
            .RRAddField("Leader", Context.Guild.GetUser(gang.Leader).Sanitize());

        foreach (string position in Constants.GANG_POSITIONS)
        {
            var posMems = gang.Members.Where(m => m.Value == position);
            IEnumerable<string> posMemNames = posMems.Select(m => Context.Guild.GetUser(m.Key).Sanitize());
            embed.RRAddField($"{position}s", string.Join('\n', posMemNames));
        }

        embed.RRAddField("Vault Balance", gang.VaultBalance);
        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }
}
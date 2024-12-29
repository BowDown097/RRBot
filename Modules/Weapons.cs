namespace RRBot.Modules;
[Summary("So you've chosen the path of death huh? The rewards here may be pretty large, but be warned - I don't want to hear about any of this \"guilt\" stuff. Proceed with caution.")]
public class Weapons : ModuleBase<SocketCommandContext>
{
    [Command("shoot")]
    [Summary("Blast someone into oblivion with your gun.")]
    [RequireCooldown("ShootCooldown", "Woah woah woah! You got a killing addiction there or something? You've gotta wait {0}. Sorry.")]
    public async Task<RuntimeResult> Shoot(IGuildUser user, [Remainder] string gun)
    {
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("​I don't think shooting yourself is a great idea.");
        if (user.IsBot)
            return CommandResult.FromError("Shoot a bot? Hell nah. Not a chance.");

        Weapon weapon = Array.Find(Constants.Weapons,
            w => w.Name.Equals(gun, StringComparison.OrdinalIgnoreCase) && w.Type == "Gun");
        if (weapon is null)
            return CommandResult.FromError("That is not a gun!");

        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (!author.Ammo.TryGetValue(weapon.Ammo, out int amount) || amount == 0)
            return CommandResult.FromError($"You need {weapon.Ammo}s!");
        if (!author.Weapons.Contains(weapon.Name))
            return CommandResult.FromError("You do not have that gun!");

        author.Ammo[weapon.Ammo]--;
        
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (target.UsingSlots)
            return CommandResult.FromError($"**{user.Sanitize()}** is currently gambling. They cannot do any transactions at the moment.");

        int accuracyRoll = RandomUtil.Next(1, 101);
        if (accuracyRoll > weapon.Accuracy)
        {
            switch (RandomUtil.Next(3))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel,
                        "The dude did some fucking Matrix shit and literally dodged your bullet. What in the sac of nuts.");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel,
                        "The gun jammed and the son of a bitch got away. Looks like it's time to kill the retard that made this gun too.");
                    break;
                case 2:
                    await Context.User.NotifyAsync(Context.Channel,
                        "You just straight up missed! Skill issue.");
                    break;
            }
        }
        else
        {
            int damageRoll = RandomUtil.Next(weapon.DamageMin, weapon.DamageMax + 1);
            target.Health -= damageRoll;
            if (target.Health > 0)
            {
                switch (RandomUtil.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel,
                            $"Clean shot. Right in the leg. Cool beans. You dealt **{damageRoll}** damage.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel,
                            $"You saw a miracle today bro! You shot the dude in the FACE and he lived. He certainly don't look like a miracle now though lmao. You dealt **{damageRoll}** damage.");
                        break;
                }
            }
            else
            {
                switch (RandomUtil.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel,
                            $"​HIS HEAD FUCKING BLEW UP LMFAO 😂! GUTS AND GORE BABY! You got **{target.Cash:C2}**.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel,
                            $"Well damn, that was nice and easy. Dude went down like nothing and is now in a river somewhere 100 miles away. You got **{target.Cash:C2}**.");
                        break;
                }

                await author.SetCashWithoutAdjustment(Context.User, author.Cash + target.Cash);
                await target.SetCashWithoutAdjustment(user, 0);
                target.Health = 100;
            }
        }

        await author.SetCooldown("ShootCooldown", Constants.ShootCooldown, Context.Guild, Context.User);
        await MongoManager.UpdateObjectAsync(author);
        await MongoManager.UpdateObjectAsync(target);
        return CommandResult.FromSuccess();
    }
}
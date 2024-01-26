<a href="https://github.com/BowDown097/RRBot">
	<img src="https://i.imgur.com/qAz3zEk.png" />
</a>
<p align="center">
    <a href="https://discord.gg/USpJnaaNap" alt="Discord">
        <img src="https://img.shields.io/discord/809485099238031420" />
    </a>
    <img src="https://img.shields.io/codefactor/grade/github/BowDown097/RRBot" />
</p>
<p align="center">The bot for the <a href="https://discord.gg/USpJnaaNap">Rush Reborn Discord server</a>, heavily inspired by the <a href="https://github.com/Asshley/DEA">DEA Discord bot</a>.</p>
<p align="center">
    <a href="https://discord.com/api/oauth2/authorize?client_id=817790099823525909&permissions=1392042404951&scope=bot">
        <img src="https://i.imgur.com/5qpaqiQ.png" width="248px" height="63px" />
    </a>
</p>

## Features
This bot is JAM-PACKED with features done like no other, such as:
- 💵 **Cash system**
    - 🕵🏻‍♂️ Crime
    - 🎲 Gambling
    - ⛏️ Items
    - 🪙 Supplementary cryptocurrency system, modeled after **REAL WORLD PRICES**
- 🪵 **Logging**
    - 📝 Logs pretty much everything possible. Probably the most verbose out of any bot.
- 🧹 **Moderation**
- 📻 **Music**
    - 🥇 Supports literally HUNDREDS of websites, as well as direct links and attachments. Completely unmatched.
- 🗳️ **Polls**
    - 🏛️ Easy to set up elections, perfect for democratic servers

and best of all, it's **HIGHLY** customizable.

## Issues?
The bot is constantly in development, and issues are bound to come up (especially because I suck at testing features). If you've run into an issue, it is greatly preferred that it is reported in the #bug-reports channel of the official Discord server. If you absolutely don't want to, then the Issues section of the repository is fine.

If you know what you're doing, it is encouraged that you create a pull request. I will make sure to go over it and respond as quickly as possible, and you will receive a role in the Discord for your work if it makes it in.

## Running your own instance of the bot
In order to get the bot running good n' gold, you will need to supply appropriate credentials in the credentials.json file in the bot's build directory. Here's what it looks like:
```json
{
    "mongoConnectionString": "[CONNECTION STRING]",
    "token": "[DISCORD BOT TOKEN]"
}
```

``mongoConnectionString`` is a MongoDB connection string. If you don't know what that is and how to get it, get Googling. By the way, I highly recommend you use a self-hosted database.

``token`` is the bot token, which you can get from your instance of the bot's application at https://discord.com/developers, under the "Bot" tab. 


In order to use the music features of the bot, you will need to get the [latest release of Lavalink](https://github.com/freyacodes/Lavalink/releases) and ideally throw it in the bot's build directory. You will need to run it alongside the bot. [yt-dlp](https://github.com/yt-dlp/yt-dlp) is also required for support for more platforms than just YouTube and SoundCloud. **Not everything that works with yt-dlp will work with Lavalink.**

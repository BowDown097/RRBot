<a href="https://github.com/BowDown097/RRBot">
	<img src="https://i.imgur.com/qAz3zEk.png" />
</a>
<p align="center">
    <a href="https://discord.gg/USpJnaaNap" alt="Discord">
        <img src="https://img.shields.io/discord/809485099238031420" />
    </a>
    <img src="https://img.shields.io/codefactor/grade/github/BowDown097/RRBot" />
	<img src="https://img.shields.io/tokei/lines/github/BowDown097/RRBot" />
</p>
<p align="center">The bot for the <a href="https://discord.gg/USpJnaaNap">Rush Reborn Discord server</a>, heavily inspired by the <a href="https://github.com/Asshley/DEA">DEA Discord bot</a>.</p>
<p align="center">
    <a href="https://discord.com/oauth2/authorize?response_type=code&client_id=817790099823525909&permissions=8&scope=bot">
        <img src="https://i.imgur.com/5qpaqiQ.png" width="248px" height="63px" />
    </a>
</p>

## Features
This bot offers a lot of things:
- 💵 **Cash system**
    - 🕵🏻‍♂️ Crime
    - 🎲 Gambling
    - ⛏️ Items
    - 🪙 Supplementary cryptocurrency system, modeled after **REAL WORLD PRICES**
- 🎮 **Games**
- 🧹 **Moderation**
- 📻 **Music**
- 🔞 **NSFW** (optional of course)
- 🗳️ **Polls**

and best of all, it's **HIGHLY** customizable.

## Issues?
The bot is constantly in development, and issues are bound to come up (especially because I suck at testing features). If you've run into an issue, it is greatly preferred that it is reported in the #bug-reports channel of the official Discord server. If you absolutely don't want to, then the Issues section of the repository is fine.

If you know what you're doing, it is encouraged that you create a pull request. I will make sure to go over it and respond as quickly as possible, and you will receive a role in the Discord for your work if it makes it in.

## Running your own instance of the bot
In order to build the bot, you will need to create a file named Credentials.cs in the bot's main directory. The code will need to look exactly like this:
```cs
namespace RRBot;
public static class Credentials
{
    public const string CREDENTIALS_PATH = "[PATH TO FIRESTORE CREDENTIALS]";
    public const string TOKEN = "[DISCORD BOT TOKEN]";
}
```
You can get the bot token from your instance of the bot's application at https://discord.com/developers, under the "Bot" tab. 

As the bot uses Firestore as its database, you will need to create your own database and generate a credentials file. For information on how to, see [this page for creating the database](https://firebase.google.com/docs/firestore/quickstart) and/or [this page for authentication](https://cloud.google.com/docs/authentication/getting-started#creating_a_service_account).

In order to use the music features of the bot, you will need to get the [latest release of Lavalink](https://github.com/freyacodes/Lavalink/releases) and throw it in the bot's main directory. You will need to run it alongside the bot. [yt-dlp](https://github.com/yt-dlp/yt-dlp) is also optionally required for support for more platforms than just YouTube and SoundCloud. Keep in mind that not everything that works with yt-dlp will work with Lavalink (ie. Pornhub, Vimeo).

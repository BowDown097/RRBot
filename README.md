# RR Bot
The bot for the Rush Reborn Discord server, heavily inspired by the [DEA Discord bot](https://github.com/Asshley/DEA).

<p align="center">
    <a href="https://discord.gg/USpJnaaNap" alt="Discord">
        <img src="https://img.shields.io/discord/809485099238031420" />
    </a>
</p>

[Join the official Discord!](https://discord.gg/USpJnaaNap)

## Features
RR is a Discord bot that can do many, many things. It's got a cash system, it's got music playing support, it's got various moderation commands, and tons of other stuff. Oh, and it's highly configurable.

## Issues?
The bot is constantly in development, and issues are bound to come up (especially because I suck at testing features). If you've run into an issue, it is greatly preferred that it is reported in the #bug-reports channel of the official Discord server. If you absolutely don't want to, then the Issues section of the repository is fine.

If you know what you're doing, it is encouraged that you create a pull request. I will make sure to go over it and respond as quickly as possible, and you will receive a role in the Discord for your work if it makes it in.

## Running your own instance of the bot
In order to build the bot, you will need to create a file named Credentials.cs in the bot's main directory. The code will need to look exactly like this:
```cs
namespace RRBot
{
    public static class Credentials
    {
        public static readonly string TOKEN = "[DISCORD BOT TOKEN]";
        public static readonly string CREDENTIALS_PATH = "[PATH TO FIRESTORE CREDENTIALS]";
    }
}
```
You can get the bot token from your instance of the bot's application at https://discord.com/developers, under the "Bot" tab. 

As the bot uses Firestore as its database, you will need to create your own database and generate a credentials file. For information on how to, see [this page for creating the database](https://firebase.google.com/docs/firestore/quickstart) and/or [this page for authentication](https://cloud.google.com/docs/authentication/getting-started#creating_a_service_account).

In order to use the music features of the bot, you will need to get the [latest release of Lavalink](https://github.com/freyacodes/Lavalink/releases) and throw it in the bot's main directory. You will need to run it alongside the bot.

# Rusty Watcher

This is a RUST (game) Discord Bot. It allows you to control/monitor your server from discord.
It supports many features such as, chatlogs, detailed fully customizable server info embeds, command execution from discord, 
multiple connections at once and much more.

## Example

![Server Info Message](https://i.imgur.com/4UkWPlD.png)

![Chatlog Feature & Command](https://i.imgur.com/LyFsqBT.png)

## Getting Started

These instructions will get you a copy of the project up and running on your local machine. See 'Installing' for notes on how to deploy the project on a live system.

### Installing

A step by step series of examples that tell you how to get this programm running

1. Downloading

```
Head over to the 'Release' tab and download the latest release.
```

2. Setting up the files

```
Unzip the folder
```

3. Setting up the program

```
Now run the RustyWatcher.exe, keep in mind that your antivirus might warn you that this is a virus.
If it identifies it as a virus click on 'Show details' and 'Allow'.
```

4. Setting up the bot

```
Now run the program and a folder 'Files' will appear.
Then close the program and edit the 'config.json' in 'Files' folder.
After you set up all the options you can run the program and everything should be working!
```

Now you should be ready to use the bot. Enjoy :-)
If you plan to use the chatlog feature, be sure to also install the plugin on your server.

5. Setting up your server (only needed for the chatlog feature!)

```
For this head over to https://umod.org/games/rust and install oxide.
Then move the RustyWatcher.cs file into your ./oxide/plugins folder.
```

### Discord Bot Token

If you don't know what a discord bot token is, or how to retrieve it, then head over to [this](https://github.com/reactiflux/discord-irc/wiki/Creating-a-discord-bot-&-getting-a-token) website. 

### Discord Webhook Url

Since on high populated servers the chat can get heated and therefore a lot of messages sent in a short period of time, 
I opted for using Webhooks instead of the usual message embeds as they can contain multiple embeds per message.

You can create a Webhook via right-clicking the channel you want to receive the chat messages in, then 'Edit Channel', 
next head over to 'Integrations' and click 'Create Webhook'. There you will find the option 'Copy Webhook Url'.

### Steam API Key

To allow for the steam icons to show in the chatlog you need to provide a Steam API key. You can find/create your own Steam API Key [here](https://steamcommunity.com/dev/apikey).

### Default Configuration

```json
{
  "Discord refresh (seconds)": 30,
  "Reconnect delay (seconds)": 10,
  "Create output file": false,
  "Steam API Key": "",
  "Servers": [
    {
      "Discord": {
        "Bot Token": "",
        "Guild ID": 0,
        "Activity type (0 = Playing, 1 = Streaming, 2 = Listening, 3 = Watching)": 0
      },
      "Rcon": {
        "Server IP": "",
        "Rcon Port": "",
        "Rcon Password": "",
        "Server Port (Optional only used in ServerInfo Embed)": ""
      },
      "Chatlog": {
        "Use Chatlog": false,
        "Chatlog Webhook Url": "",
        "Command Prefix": "!",
        "Chatlog Channel Id": 0,
        "Default Name Color (for send messages when no steamId provided)": "#af5",
        "Server Message Colour (RGB)": {
          "Red": 255,
          "Green": 0,
          "Blue": 0
        },
        "Can Use Commands Role Ids": [
          0,
          0
        ],
        "Show team chat": true
      },
      "Serverinfo": {
        "Server info channel ID": 0,
        "Show player count in status": true,
        "Status message (only if player count is not shown in status)": "",
        "Get server region data over (https://ipinfo.io)": false,
        "Show server info in embed": true,
        "Server info embed title hyperlink": "",
        "Server info embed color (RGB)": {
          "Red": 44,
          "Green": 47,
          "Blue": 51
        }
      },
      "Localization": {
        "Player Count Status": "{0} / {1} {2}",
        "Embed Title": "{0} {1}",
        "Embed Description": "{0}:{1}",
        "Embed Footer": "Last Wiped {0}",
        "Embed Field Player": {
          "Name": "Players",
          "Value": "{0}",
          "Inline": true
        },
        "Embed Field FPS": {
          "Name": "FPS",
          "Value": "{0}",
          "Inline": true
        },
        "Embed Field Entities": {
          "Name": "Entities",
          "Value": "{0}",
          "Inline": true
        },
        "Embed Field Game time": {
          "Name": "Game time",
          "Value": "{0}",
          "Inline": true
        },
        "Embed Field Uptime": {
          "Name": "Uptime",
          "Value": "{0}",
          "Inline": true
        },
        "Embed Field Map": {
          "Name": "Map",
          "Value": "[View here]({0})",
          "Inline": true
        }
      }
    }
  ],
  "Staff Discord & SteamIds (Key: DiscordId; Value: SteamId)": {
    "0": 0
  },
  "Debug": false
}
```

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details



# Player Count Bot

This is a RUST (game) Player Count Bot that links the current players in game with its discord status.
It also supports embeded messages that display some more detailed information about the server.

## Example

![Server Info Message](https://i.imgur.com/YtwrM5X.png)

## Getting Started

These instructions will get you a copy of the project up and running on your local machine. See 'Installing' for notes on how to deploy the project on a live system.

### Installing

A step by step series of examples that tell you how to get this programm running

1. Downloading

```
Head over to the 'Release' tab and download the latest release.
```

2. Getting the file ready

```
Unzip the folder
```

3. Setting up the program

```
Now run the DiscordBot.exe, keep in mind that your antivirus might warn you that this is a virus.
If it identifies it as a virus click on 'Show details' and 'Allow'.
```

4. Setting up the bot

```
Now run the program and a folder 'Files' will appear.
Then close the program and edit the 'config.json' in 'Files' folder.
After you set up all the options you can run the program and everything should be set up!
```

Now you should be ready to use the bot. Enjoy :-)

### Discord Bot Token

If you don't know what a discord bot token is, or how to retrieve it, then head over to [this](https://github.com/reactiflux/discord-irc/wiki/Creating-a-discord-bot-&-getting-a-token) website. 

### Default Configuration

```json
{
  "Discord refresh (seconds)": 30,
  "Reconnect delay (seconds)": 10,
  "Create output file": false,
  "Servers": [
    {
      "Discord": {
        "Bot Token": "",
        "Guild ID": 0,
        "Server info channel ID": 0
      },
      "Rcon": {
        "Server IP": "",
        "Rcon Port": "",
        "Server Port": "",
        "Rcon Password": ""
      },
      "Settings": {
        "Show player count in status": true,
        "Status message (only if player count is not shown in status)": "",
        "Get server region data over (https://ipinfo.io)": false,
        "Show server info in embed": true,
        "Server info embed link": ""
      }
    }
  ],
  "Debug": false
}
```

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details



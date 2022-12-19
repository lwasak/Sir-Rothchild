# Sir-Rothchild
Discord bot some task automation purposes.

Right now the bot will generate each Saturday at 00:00 UTC a series of messages. The messages will reflect the following week with short date and day name (format `dd.MM ddd`). 

```
===== [ 19.12 - 25.12 ] =====
19.12 mon.
20.12 tue.
21.12 wen.
22.12 thu.
23.12 fri.
24.12 sat.
25.12 sun.
========================
```

When 4 (configurable via `DISCORD__REACTIONNUMBERFORTHREADCREATION`) reactions are placed from `:house:`, `:question:` or `:white_check_mark:`, the bot will create a thread for that message.

## Configuring image
* `DISCORD__TOKEN` - token that you discord bot uses
* `DISCORD__CHANNELID` - discord channel on which bot should operate (bit also has to be invited toe the guild where the channel is)
* `DISCORD__LOCALE` - locale in which localised strings should be deisplayed
* `DISCORD__REACTIONNUMBERFORTHREADCREATION` - number of reactions needed on message for bot to create thread

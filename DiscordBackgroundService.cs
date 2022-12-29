using System.Globalization;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SirRothchild.Settings;

namespace SirRothchild;

public class DiscordServiceBackgroundService : BackgroundService
{
    private readonly DiscordOptions _discordOptions;
    private readonly DiscordSocketClient _client;
    private readonly string _dataFileUrl = $"{AppContext.BaseDirectory}last_execution.data";

    public DiscordServiceBackgroundService(IOptions<DiscordOptions> discordOptions)
    {
        _discordOptions = discordOptions.Value;

        Console.WriteLine("[2] Settings:");
        Console.WriteLine(JsonConvert.SerializeObject(_discordOptions, Formatting.Indented));
        
        var culture = new CultureInfo(_discordOptions.Locale);
        Thread.CurrentThread.CurrentCulture = culture;
        
        _client = new DiscordSocketClient();
        _client.Ready += Ready;
        _client.ReactionAdded += ReactionAdded;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime? lastExecutionTime = null;
        Console.WriteLine("Starting Sir Rothchild service..");
        if (File.Exists(_dataFileUrl))
        {
            var text = await File.ReadAllTextAsync(_dataFileUrl, stoppingToken);
            if (DateTime.TryParse(text, out var parsedDate))
            {
                lastExecutionTime = parsedDate;
            }
        }
        
        await _client.LoginAsync(TokenType.Bot, _discordOptions.Token);
        await _client.StartAsync();
        
        Console.WriteLine("Sir Rothchild started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;

                if (now.DayOfWeek == DayOfWeek.Saturday && now.Date != lastExecutionTime?.Date)
                {
                    var channel = _client.GetChannel(_discordOptions.ChannelId);

                    if (channel is ITextChannel textChannel)
                    {
                        var monday = now.AddDays(2);
                        var sunday = monday.AddDays(6);

                        Console.WriteLine($"Generating schedule for {monday:dd.MM} - {sunday:dd.MM}");
                        await textChannel.SendMessageAsync($"===== [ {monday:dd.MM} - {sunday:dd.MM} ] =====");

                        var day = monday;
                        while (day <= sunday)
                        {
                            await textChannel.SendMessageAsync($"{day:dd.MM ddd}");

                            day = day.AddDays(1);
                        }

                        await textChannel.SendMessageAsync($"========================");
                    }

                    lastExecutionTime = now;
                    await File.WriteAllTextAsync(_dataFileUrl, lastExecutionTime.Value.ToString("O"));

                    Console.WriteLine($"Schedule generated [{lastExecutionTime}]");
                }

                Console.WriteLine("working...");

                await Task.Delay(_discordOptions.SchedulerInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Worker failed processing {ex}");
            }
        }
        
        Console.WriteLine("Stopping Sir Rothchild service..");
        
        await _client.LogoutAsync();
        await _client.StopAsync();
        
        Console.WriteLine("Sir Rothchild stopped");
    }

    private async Task Ready()
    {
        try
        {
                var channel = _client.GetChannel(_discordOptions.ChannelId);

                if (channel is ITextChannel textChannel)
                    await textChannel.SendMessageAsync("Ready to serve M'Lord / M'Lady.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on app ready {ex}");
        }
    }

    private async Task ReactionAdded(
        Cacheable<IUserMessage, ulong> messageCache,
        Cacheable<IMessageChannel, ulong> channelCache,
        SocketReaction reaction)
    {
        try
        {
            if (channelCache.Id == _discordOptions.ChannelId)
            {
                var channel = await channelCache.GetOrDownloadAsync();

                if (channel == null) return;

                if (channel is ITextChannel textChannel)
                {
                    var message = await messageCache.GetOrDownloadAsync();

                    if (message == null) return;

                    message.Reactions.TryGetValue(Emojis.House, out var hostReactions);
                    message.Reactions.TryGetValue(Emojis.WhiteCheckMark, out var attendingReactions);
                    message.Reactions.TryGetValue(Emojis.QuestionMark, out var maybeReactions);

                    if (hostReactions.ReactionCount + 
                        attendingReactions.ReactionCount + 
                        maybeReactions.ReactionCount >=
                        _discordOptions.ReactionNumberForThreadCreation)
                    {
                        await textChannel.CreateThreadAsync(
                            message.Content,
                            message: message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on reaction added {ex}");
        }
    }
}

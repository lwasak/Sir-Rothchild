using System.Globalization;
using System.Text.Json.Nodes;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SirRothchild.Settings;

namespace SirRothchild;

public class DiscordService : IHostedService
{
    private readonly DiscordOptions _discordOptions;
    private readonly DiscordSocketClient _client;
    private readonly CancellationTokenSource _cts;
    private Task? _workerTask;

    private DateTime? _lastExecutionTime;
    private readonly string _dataFileUrl = $"{AppContext.BaseDirectory}last_execution.data";

    public DiscordService(IOptions<DiscordOptions> discordOptions)
    {
        _discordOptions = discordOptions.Value;
        _client = new DiscordSocketClient();
        _cts = new CancellationTokenSource();

        Console.WriteLine("Settings:");
        Console.WriteLine(JsonConvert.SerializeObject(_discordOptions, Formatting.Indented));
        
        var culture = new CultureInfo(_discordOptions.Locale);
        Thread.CurrentThread.CurrentCulture = culture;

        _client.Ready += Ready;
        _client.ReactionAdded += ReactionAdded;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_dataFileUrl))
        {
            var text = await File.ReadAllTextAsync(_dataFileUrl, cancellationToken);
            if (DateTime.TryParse(text, out var parsedDate))
            {
                _lastExecutionTime = parsedDate;
            }
        }

        await _client.LoginAsync(TokenType.Bot, _discordOptions.Token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        if (_workerTask != null) await _workerTask;

        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private async Task Worker()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;

                if (now.DayOfWeek == DayOfWeek.Saturday && now.Date != _lastExecutionTime?.Date)
                {
                    var channel = _client.GetChannel(_discordOptions.ChannelId);

                    if (channel is ITextChannel textChannel)
                    {
                        var monday = now.AddDays(2);
                        var sunday = monday.AddDays(6);

                        await textChannel.SendMessageAsync($"===== [ {monday:dd.MM} - {sunday:dd.MM} ] =====");

                        var day = monday;
                        while (day <= sunday)
                        {
                            await textChannel.SendMessageAsync($"{day:dd.MM ddd}");

                            day = day.AddDays(1);
                        }

                        await textChannel.SendMessageAsync($"========================");
                    }

                    _lastExecutionTime = now;
                    await File.WriteAllTextAsync(_dataFileUrl, _lastExecutionTime.Value.ToString("O"));
                }

                await Task.Delay(_discordOptions.SchedulerInterval, _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private async Task Ready()
    {
        try
        {
            var channel = _client.GetChannel(_discordOptions.ChannelId);

            if (channel is ITextChannel textChannel) await textChannel.SendMessageAsync("Connected");

            _workerTask = Task.Run(Worker);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
            Console.WriteLine(ex);
        }
    }
}
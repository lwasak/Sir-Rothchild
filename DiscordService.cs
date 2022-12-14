using System.Globalization;
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

        Console.WriteLine($"{nameof(DiscordService)} Settings:");
        Console.WriteLine(JsonConvert.SerializeObject(_discordOptions, Formatting.Indented));
        
        var culture = new CultureInfo(_discordOptions.Locale);
        Thread.CurrentThread.CurrentCulture = culture;

        _client.Ready += Ready;
        _client.ReactionAdded += ReactionAdded;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting Sir Rothchild service..");
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
        
        Console.WriteLine("Sir Rothchild started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Stopping Sir Rothchild service..");
        
        _cts.Cancel();
        if (_workerTask != null) await _workerTask;

        await _client.StopAsync();
        await _client.LogoutAsync();
        
        Console.WriteLine("Sir Rothchild stopped");
    }

    private async Task Worker()
    {
        Console.WriteLine("Worker initiated");
        
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

                    _lastExecutionTime = now;
                    await File.WriteAllTextAsync(_dataFileUrl, _lastExecutionTime.Value.ToString("O"));
                    
                    Console.WriteLine($"Schedule generated [{_lastExecutionTime}]");
                }
                
                Console.WriteLine("working...");

                await Task.Delay(_discordOptions.SchedulerInterval, _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Worker failed processing {ex}");
            }
        }
        
        Console.WriteLine("Worked finished working");
    }

    private async Task Ready()
    {
        try
        {
            if (_workerTask == null)
            {
                var channel = _client.GetChannel(_discordOptions.ChannelId);

                if (channel is ITextChannel textChannel)
                    await textChannel.SendMessageAsync("Ready to serve M'Lord / M'Lady.");

                Console.WriteLine("Spawning task..");
                _workerTask = Task.Run(Worker, _cts.Token);
                Console.WriteLine("Task spawned");
                PrintTaskStatus();
            }

            Console.WriteLine($"Last schedule was sent at {_lastExecutionTime:O}");
            PrintTaskStatus();

            void PrintTaskStatus()
            {
                Console.WriteLine(JsonConvert.SerializeObject(new
                {
                    _workerTask?.Id,
                    _workerTask?.Exception,
                    _workerTask?.IsCompleted,
                    _workerTask?.IsCompletedSuccessfully,
                    _workerTask?.IsCanceled,
                    _workerTask?.IsFaulted,
                    _workerTask?.Status
                }, Formatting.Indented));
            }
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

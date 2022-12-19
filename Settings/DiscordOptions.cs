using System.ComponentModel.DataAnnotations;

namespace SirRothchild.Settings;

public class DiscordOptions
{
    public const string SectionName = "Discord";
    
    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public ulong ChannelId { get; set; }
    
    [Required]
    public TimeSpan SchedulerInterval { get; set; }

    [Required]
    public string Locale { get; set; } = null!;

    [Required]
    public int ReactionNumberForThreadCreation { get; set; }
}
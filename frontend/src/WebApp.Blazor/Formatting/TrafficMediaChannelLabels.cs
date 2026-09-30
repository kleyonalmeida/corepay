using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class TrafficMediaChannelLabels
{
    public static string Format(TrafficMediaChannelDto channel) =>
        channel switch
        {
            TrafficMediaChannelDto.Telegram => "Telegram",
            TrafficMediaChannelDto.Instagram => "Instagram",
            TrafficMediaChannelDto.Story => "Story",
            TrafficMediaChannelDto.Direct => "Direto",
            TrafficMediaChannelDto.Remarketing => "Remarketing",
            TrafficMediaChannelDto.Other => "Outros",
            _ => channel.ToString()
        };

    public static IReadOnlyList<TrafficMediaChannelDto> AllChannels { get; } =
    [
        TrafficMediaChannelDto.Telegram,
        TrafficMediaChannelDto.Instagram,
        TrafficMediaChannelDto.Story,
        TrafficMediaChannelDto.Direct,
        TrafficMediaChannelDto.Remarketing,
        TrafficMediaChannelDto.Other
    ];
}

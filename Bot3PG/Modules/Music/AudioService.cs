using Discord;
using Bot3PG.Handlers;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using System;

namespace Bot3PG.Modules.Music
{
    public sealed class AudioService
    {
        public LavaPlayer Player { get; private set; }
        public LavaSocketClient LavaClient { get; private set; }
        public LavaRestClient LavaRestClient { get; private set; }

        public AudioService(LavaSocketClient lavaSocketClient, LavaRestClient lavaRestClient) 
        { 
            LavaClient = lavaSocketClient;
            LavaRestClient = lavaRestClient;
            Music.AudioService = this;
        }

        public async Task OnFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            System.Console.WriteLine("on finish");
            System.Console.WriteLine(reason);
            if (reason is TrackEndReason.LoadFailed || reason is TrackEndReason.Cleanup || reason is TrackEndReason.Replaced || reason is TrackEndReason.Stopped) return;

            player.Queue.TryDequeue(out var queueObject);
            var nextTrack = queueObject as LavaTrack;

            if (nextTrack != null)
            {
                await player.PlayAsync(nextTrack);
                if (player.TextChannel != null)
                {
                    await player.TextChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Now Playing", $"{Hyperlink(nextTrack)}", Color.Blue));
                }
            }
        }
        
        private static string GetDuration(LavaTrack track) => track.Length.ToString(track.Length > TimeSpan.FromHours(1) ? @"hh\:mm\:ss" : @"mm\:ss");
        public static string Hyperlink(LavaTrack track) => $"[{track.Title}]({track.Uri}) `{GetDuration(track)}`";
    }
}
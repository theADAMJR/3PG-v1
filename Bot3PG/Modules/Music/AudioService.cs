using Discord;
using Discord.WebSocket;
using Bot3PG.DataStructs;
using Bot3PG.Handlers;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;
using Bot3PG.Core.Data;
using Bot3PG.Services;

namespace Bot3PG.Modules.Music
{
    public sealed class AudioService
    {
        private LavaPlayer player;
        private readonly LavaSocketClient _lavaSocketClient;
        private readonly LavaRestClient _lavaRestClient;

        public AudioService(LavaSocketClient lavaSocketClient, LavaRestClient lavaRestClient)
        {
            _lavaSocketClient = lavaSocketClient;
            _lavaRestClient = lavaRestClient;
        }
        #region Music Region

        private readonly Lazy<ConcurrentDictionary<ulong, AudioOptions>> _lazyOptions
            = new Lazy<ConcurrentDictionary<ulong, AudioOptions>>();

        private ConcurrentDictionary<ulong, AudioOptions> Options
            => _lazyOptions.Value;
        
        public async Task<Embed> JoinOrPlayAsync(SocketGuildUser user, ITextChannel textChannel, ulong guildId, string query = null, string platform = "youtube")
        {
            if (user.VoiceChannel is null)
                return await EmbedHandler.CreateErrorEmbed("Music, Join/Play", "You Must First Join a Voice Channel.");
            
            if (query is null)
            {
                await _lavaSocketClient.ConnectAsync(user.VoiceChannel);//, textChannel /*This Param is Optional, Only used If we want to bind the Bot to a TextChannel For commands.*/);
                Options.TryAdd(user.Guild.Id, new AudioOptions
                {
                    Summoner = user
                });
                await Debug.LogInformationAsync("Music", $"Now connected to {user.VoiceChannel.Name}.");// and bound to {textChannel.Name}.");
                return await EmbedHandler.CreateBasicEmbed("Music", $"Now connected to {user.VoiceChannel.Name}.", Color.Blue);// and bound to {textChannel.Name}. **Headphone warning**...", Color.Blue);
            }
            else
            {
                try
                {
                    var player = _lavaSocketClient.GetPlayer(guildId);
                    if (player is null)
                    {
                        Options.TryAdd(user.Guild.Id, new AudioOptions
                        {
                            Summoner = user
                        });
                        await this.player.VoiceChannel.ConnectAsync();
                        player = _lavaSocketClient.GetPlayer(guildId);
                    }

                    SearchResult search;
                    switch (platform)
                    {
                        case "soundcloud":
                            search = await _lavaRestClient.SearchSoundcloudAsync(query);
                            break;
                        default:
                            search = await _lavaRestClient.SearchYouTubeAsync(query);
                            break;
                    }

                    if (search.LoadType == LoadType.NoMatches)
                        return await EmbedHandler.CreateErrorEmbed("Music", $"OOF! I wasn't able to find anything for {query}.");

                    var track = search.Tracks.FirstOrDefault();

                    if (track.Length > TimeSpan.FromHours(2))
                    {
                        return await EmbedHandler.CreateBasicEmbed("Music", $"Track duration must be under 2 hours.", Color.Red);
                    }

                    if (player.CurrentTrack != null && player.IsPlaying || player.IsPaused)
                    {
                        player.Queue.Enqueue(track);
                        await Debug.LogInformationAsync("Music", $"{track.Title} has been added to the music queue.");
                        return await EmbedHandler.CreateBasicEmbed("Music", $"{track.Title} has been added to queue.", Color.Blue);
                    }
                    //Player was not playing anything, so lets play the requested track.
                    await player.PlayAsync(track);
                    await Debug.LogInformationAsync("Music", $"Bot Now Playing: {track.Title}\nUrl: {track.Uri}");
                    return await EmbedHandler.CreateBasicEmbed("Music", $"Now Playing: {track.Title}\nUrl: {track.Uri}", Color.Blue);
                }
                //If after all the checks we did, something still goes wrong. Tell the user about it so they can report it back to us.
                catch (Exception ex)
                {
                    return await EmbedHandler.CreateErrorEmbed("Music, Join/Play", ex.ToString());
                }
            }

        }
        
        public async Task<Embed> LeaveAsync(ulong guildId)
        {
            try
            {
                var player = _lavaSocketClient.GetPlayer(guildId);

                if (player is null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check {GetCommandPrefix(guildId)}help for info on how to use the bot.");
                
                if (player.IsPlaying)
                    await player.StopAsync();
                
                var channel = player.VoiceChannel;
                await _lavaSocketClient.DisconnectAsync(channel);
                await Debug.LogInformationAsync("Music", $"Bot has left {channel.Name}.");
                return await EmbedHandler.CreateBasicEmbed("Music", $"I've left {channel.Name}. Thank you for playing music.", Color.Blue);
            }
            catch (InvalidOperationException ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Leave", ex.ToString());
            }
        }
        
        public async Task<Embed> ListAsync(ulong guildId)
        {
            try
            {
                var descriptionBuilder = new StringBuilder();

                var player = _lavaSocketClient.GetPlayer(guildId);
                if (player is null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check {GetCommandPrefix(guildId)}help for info on how to use the bot.");

                if (player.IsPlaying)
                {
                    if (player.Queue.Count < 1 && player.CurrentTrack != null)
                    {
                        return await EmbedHandler.CreateBasicEmbed($"Now Playing: {player.CurrentTrack.Title}", "Nothing else is queued.", Color.Blue);
                    }
                    else
                    {
                        var trackNum = 2;
                        foreach (LavaTrack track in player.Queue.Items)
                        {
                            descriptionBuilder.Append($"{trackNum}: [{track.Title}]({track.Uri}) - {track.Id}\n");
                            trackNum++;
                        }
                        return await EmbedHandler.CreateBasicEmbed("Music Playlist", $"Now Playing: [{player.CurrentTrack.Title}]({player.CurrentTrack.Uri})\n{descriptionBuilder.ToString()}", Color.Blue);
                    }
                }
                else
                {
                    return await EmbedHandler.CreateErrorEmbed("Music, List", "Player doesn't seem to be playing anything right now. If this is an error, Please contact admins.");
                }
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, List", ex.Message);
            }

        }

        public async Task<Embed> SkipTrackAsync(ulong guildId)
        {
            try
            {
                var player = _lavaSocketClient.GetPlayer(guildId);
                if (player is null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check {GetCommandPrefix(guildId)}help for info on how to use the bot.");
                if (player.Queue.Count < 1)
                {
                    return await EmbedHandler.CreateErrorEmbed("Music, SkipTrack", $"Unable To skip a track as there are either one or no songs currently playing." +
                        $"\n\nDid you mean {GetCommandPrefix(guildId)}stop?");
                }
                else
                {
                    try
                    {
                        var currentTrack = player.CurrentTrack;
                        await player.SkipAsync();
                        await Debug.LogInformationAsync("Music", $"Skipped: {currentTrack.Title}");
                        return await EmbedHandler.CreateBasicEmbed("Music Skip", $"{currentTrack.Title} successfully skipped", Color.Blue);
                    }
                    catch (Exception ex)
                    {
                        return await EmbedHandler.CreateErrorEmbed("Music, Skip", ex.ToString());
                    }

                }
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Skip", ex.ToString());
            }
        }

        public async Task<Embed> StopAsync(ulong guildId)
        {
            try
            {
                var player = _lavaSocketClient.GetPlayer(guildId);

                if (player is null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check {GetCommandPrefix(guildId)}help for info on how to use the bot.");
                /* Check if the player exists, if it does, check if it is playing.
                     If it is playing, we can stop.*/
                if (player.IsPlaying)
                    await player.StopAsync();
                /* Not sure if this is required as I think player.StopAsync(); clears the queue anyway. */
                foreach (var track in player.Queue.Items)
                    player.Queue.Dequeue();
                await Debug.LogInformationAsync("Music", $"Bot has stopped playback.");
                return await EmbedHandler.CreateBasicEmbed("Music Stop", "I Have stopped playback & the playlist has been cleared.", Color.Blue);
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Stop", ex.ToString());
            }
        }

        public async Task<Embed> VolumeAsync(ulong guildId, int volume)
        {
            if (volume < 0 || volume > 150)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Volume", "Volume must be between 0 and 150.");
            }
            try
            {
                var player = _lavaSocketClient.GetPlayer(guildId);
                if (player is null)
                {
                    return await EmbedHandler.CreateErrorEmbed("Music, Volume", $"Could not aquire player.\nAre you using the bot right now? check {GetCommandPrefix(guildId)}help for info on how to use the bot.");
                }
                if (volume == 0)
                {
                    return await EmbedHandler.CreateBasicEmbed("Music", $"**Current Volume:** {player.CurrentVolume}", Color.Blue);
                }
                await player.SetVolumeAsync(volume);
                await Debug.LogInformationAsync("Music", $"Bot Volume set to: {volume}");
                return await EmbedHandler.CreateBasicEmbed("Music", $"Volume has been set to {volume}.", Color.Blue);
            }
            catch (InvalidOperationException ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Volume", ex.ToString());
            }
        }

        public async Task<Embed> PauseOrResume(ulong guildId)
        {
            try
            {
                var player = _lavaSocketClient.GetPlayer(guildId);
                if (!player.IsPaused)
                {
                    await player.PauseAsync();
                    return await EmbedHandler.CreateBasicEmbed("Music", $"**Paused:** {player.CurrentTrack.Title}", Color.Blue);
                }

                await player.ResumeAsync();
                return await EmbedHandler.CreateBasicEmbed("Music", $"Resumed:** Now Playing {player.CurrentTrack.Title}.", Color.Blue);
            }
            catch (InvalidOperationException ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Pause/Resume", ex.Message);
            }
        }

        public async Task OnFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            if (reason is TrackEndReason.LoadFailed || reason is TrackEndReason.Cleanup)
                return;
            player.Queue.TryDequeue(out IQueueObject queueObject);
            var nextTrack = queueObject as LavaTrack;

            if (nextTrack is null)
            {
                await Debug.LogInformationAsync("Music", $"[{player.VoiceChannel.Guild.Name}] Bot has stopped playback.");
                await player.StopAsync();
            }
            else
            {
                await player.PlayAsync(nextTrack);
                await Debug.LogInformationAsync("Music", $"[{player.VoiceChannel.Guild.Name}] Bot Now Playing: {nextTrack.Title} - {nextTrack.Uri}");
                await player.TextChannel.SendMessageAsync("", false, await EmbedHandler.CreateBasicEmbed("Now Playing", $"[{nextTrack.Title}]({nextTrack.Uri})", Color.Blue));
            }
        }

        private async Task<string> GetCommandPrefix(ulong guildId)
        {
            var socketGuild = Global.Client.GetGuild(guildId);
            var guild = await Guilds.GetAsync(socketGuild);
            return guild.General.CommandPrefix;
        }
        #endregion

        #region Other

        public async Task<Embed> DisplayStatsAsync()
        {
            var node = _lavaSocketClient.ServerStats;
            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle("Lavalink Stats")
                .WithCurrentTimestamp()
                .WithColor(Color.DarkMagenta)
                .AddField("Uptime", node.Uptime, true));
            return embed.Build();
        }

        #endregion
    }
}
using Discord;
using Discord.WebSocket;
using Bot3PG.DataStructs;
using Bot3PG.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Entities.Enums;
using Victoria.Entities.Statistics;

namespace Bot3PG.Services
{
    public sealed class AudioService
    {
        private Lavalink lavalink;

        public AudioService(Lavalink lavalink)
            => this.lavalink = lavalink;

        #region Music Region

        private readonly Lazy<ConcurrentDictionary<ulong, AudioOptions>> _lazyOptions
            = new Lazy<ConcurrentDictionary<ulong, AudioOptions>>();

        private ConcurrentDictionary<ulong, AudioOptions> Options
            => _lazyOptions.Value;
        
        public async Task<Embed> JoinOrPlayAsync(SocketGuildUser user, IMessageChannel textChannel, ulong guildId, string query = null)
        {
            if (user.VoiceChannel == null)
                return await EmbedHandler.CreateErrorEmbed("Music, Join/Play", "You Must First Join a Voice Channel.");
            
            if (query == null)
            {
                await lavalink.DefaultNode.ConnectAsync(user.VoiceChannel, textChannel /*This Param is Optional, Only used If we want to bind the Bot to a TextChannel For commands.*/);
                Options.TryAdd(user.Guild.Id, new AudioOptions
                {
                    Summoner = user
                });
                await LoggingService.LogInformationAsync("Music", $"Now connected to {user.VoiceChannel.Name} and bound to {textChannel.Name}.");
                return await EmbedHandler.CreateBasicEmbed("Music", $"Now connected to {user.VoiceChannel.Name} and bound to {textChannel.Name}. **Headphone warning**...", Color.Blue);
            }
            else
            {
                try
                {
                    //Try get the player. If it returns null then the user has used the command !Play without using the command !Join.
                    var player = lavalink.DefaultNode.GetPlayer(guildId);
                    if (player == null)
                    {
                        //User Used Command !Play before they used !Join
                        //So We Create a Connection To The Users Voice Channel.
                        await lavalink.DefaultNode.ConnectAsync(user.VoiceChannel, textChannel);
                        Options.TryAdd(user.Guild.Id, new AudioOptions
                        {
                            Summoner = user
                        });
                        //Now we can set the player to out newly created player.
                        player = lavalink.DefaultNode.GetPlayer(guildId);
                    }

                    //Find The Youtube Track the User requested.
                    LavaTrack track;
                    var search = await lavalink.DefaultNode.SearchYouTubeAsync(query);

                    //If we couldn't find anything, tell the user.
                    if (search.LoadResultType == LoadResultType.NoMatches)
                        return await EmbedHandler.CreateErrorEmbed("Music", $"OOF! I wasn't able to find anything for {query}.");

                    //Get the first track from the search results.
                    //TODO: Add a 1-5 list for the user to pick from. (Like Fredboat)
                    track = search.Tracks.FirstOrDefault();

                    if (track.Length > TimeSpan.FromHours(2))
                    {
                        return await EmbedHandler.CreateBasicEmbed("Music", $"Track duration must be under 2 hours.", Color.Red);
                    }

                    //If the Bot is already playing music, or if it is paused but still has music in the playlist, Add the requested track to the queue.
                    if (player.CurrentTrack != null && player.IsPlaying || player.IsPaused)
                    {
                        player.Queue.Enqueue(track);
                        await LoggingService.LogInformationAsync("Music", $"{track.Title} has been added to the music queue.");
                        return await EmbedHandler.CreateBasicEmbed("Music", $"{track.Title} has been added to queue.", Color.Blue);
                    }
                    //Player was not playing anything, so lets play the requested track.
                    await player.PlayAsync(track);
                    await LoggingService.LogInformationAsync("Music", $"Bot Now Playing: {track.Title}\nUrl: {track.Uri}");
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
                //Get The Player Via GuildID.
                var player = lavalink.DefaultNode.GetPlayer(guildId);

                if (player == null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check {Global.Config.CommandPrefix}help for info on how to use the bot.");

                //if The Player is playing, Stop it.
                if (player.IsPlaying)
                    await player.StopAsync();

                //Leave the voice channel.
                var channelName = player.VoiceChannel.Name;
                await lavalink.DefaultNode.DisconnectAsync(guildId);
                await LoggingService.LogInformationAsync("Music", $"Bot has left {channelName}.");
                return await EmbedHandler.CreateBasicEmbed("Music", $"I've left {channelName}. Thank you for playing music.", Color.Blue);
            }
            //Tell the user about the error so they can report it back to us.
            catch (InvalidOperationException ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Leave", ex.ToString());
            }
        }

        /*This is ran when a user uses the command List 
            Task Returns an Embed which is used in the command call. */
        public async Task<Embed> ListAsync(ulong guildId)
        {
            try
            {
                /* Create a string builder we can use to format how we want our list to be displayed. */
                var descriptionBuilder = new StringBuilder();

                /* Get The Player and make sure it isn't null. */
                var player = lavalink.DefaultNode.GetPlayer(guildId);
                if (player == null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check {Global.Config.CommandPrefix}help for info on how to use the bot.");

                if (player.IsPlaying)
                {
                    /*If the queue count is less than 1 and the current track IS NOT null then we wont have a list to reply with.
                        In this situation we simply return an embed that displays the current track instead. */
                    if (player.Queue.Count < 1 && player.CurrentTrack != null)
                    {
                        return await EmbedHandler.CreateBasicEmbed($"Now Playing: {player.CurrentTrack.Title}", "Nothing else is queued.", Color.Blue);
                    }
                    else
                    {
                        /* Now we know if we have something in the queue worth replying with, so we itterate through all the Tracks in the queue.
                         *  Next Add the Track title and the url however make use of Discords Markdown feature to display everything neatly.
                            This trackNum variable is used to display the number in which the song is in place. (Start at 2 because we're including the current song.*/
                        var trackNum = 2;
                        foreach (var track in player.Queue.Items)
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

        /*This is ran when a user uses the command Skip 
            Task Returns an Embed which is used in the command call. */
        public async Task<Embed> SkipTrackAsync(ulong guildId)
        {
            try
            {
                var player = lavalink.DefaultNode.GetPlayer(guildId);
                /* Check if the player exists */
                if (player == null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check{Global.Config.CommandPrefix}help for info on how to use the bot.");
                /* Check The queue, if it is less than one (meaning we only have the current song available to skip) it wont allow the user to skip.
                     User is expected to use the Stop command if they're only wanting to skip the current song. */
                if (player.Queue.Count < 1)
                {
                    return await EmbedHandler.CreateErrorEmbed("Music, SkipTrack", $"Unable To skip a track as there are either one or no songs currently playing." +
                        $"\n\nDid you mean {Global.Config.CommandPrefix}stop?");
                }
                else
                {
                    try
                    {
                        /* Save the current song for use after we skip it. */
                        var currentTrack = player.CurrentTrack;
                        /* Skip the current song. */
                        await player.SkipAsync();
                        await LoggingService.LogInformationAsync("Music", $"Bot skipped: {currentTrack.Title}");
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

        /*This is ran when a user uses the command Stop 
            Task Returns an Embed which is used in the command call. */
        public async Task<Embed> StopAsync(ulong guildId)
        {
            try
            {
                var player = lavalink.DefaultNode.GetPlayer(guildId);
                if (player == null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check{Global.Config.CommandPrefix}help for info on how to use the bot.");
                /* Check if the player exists, if it does, check if it is playing.
                     If it is playing, we can stop.*/
                if (player.IsPlaying)
                    await player.StopAsync();
                /* Not sure if this is required as I think player.StopAsync(); clears the queue anyway. */
                foreach (var track in player.Queue.Items)
                    player.Queue.Dequeue();
                await LoggingService.LogInformationAsync("Music", $"Bot has stopped playback.");
                return await EmbedHandler.CreateBasicEmbed("Music Stop", "I Have stopped playback & the playlist has been cleared.", Color.Blue);
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Stop", ex.ToString());
            }
        }

        /*This is ran when a user uses the command Volume 
            Task Returns a String which is used in the command call. */
        public async Task<Embed> VolumeAsync(ulong guildId, int volume)
        {
            if (volume >= 150 || volume <= 0)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Volume", "Volume must be between 0 and 150.");
            }
            try
            {
                var player = lavalink.DefaultNode.GetPlayer(guildId);
                if (player == null)
                {
                    return await EmbedHandler.CreateErrorEmbed("Music, Volume", $"Could not aquire player.\nAre you using the bot right now? check {Global.Config.CommandPrefix}help for info on how to use the bot.");
                }
                await player.SetVolumeAsync(volume);
                await LoggingService.LogInformationAsync("Music", $"Bot Volume set to: {volume}");
                return await EmbedHandler.CreateBasicEmbed("Music, Volume", $"Volume has been set to {volume}.", Color.Blue);
            }
            catch (InvalidOperationException ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Volume", ex.ToString());
            }
        }

        public async Task<string> Pause(ulong guildId)
        {
            try
            {
                var player = lavalink.DefaultNode.GetPlayer(guildId);
                if (player.IsPaused)
                {
                    await player.PauseAsync();
                    return $"**Resumed:** Now Playing {player.CurrentTrack.Title}";
                }

                await player.PauseAsync();
                return $"**Paused:** {player.CurrentTrack.Title}.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> Resume(ulong guildId)
        {
            try
            {
                var player = lavalink.DefaultNode.GetPlayer(guildId);
                if (!player.IsPaused)
                    await player.PauseAsync();
                return $"**Resumed:** {player.CurrentTrack.Title}";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        public async Task OnFinished(LavaPlayer player, LavaTrack track, TrackReason reason)
        {
            if (reason is TrackReason.LoadFailed || reason is TrackReason.Cleanup)
                return;
            player.Queue.TryDequeue(out LavaTrack nextTrack);

            if (nextTrack == null)
            {
                await LoggingService.LogInformationAsync("Music", $"[{player.VoiceChannel.Guild.Name}] Bot has stopped playback.");
                await player.StopAsync();
            }
            else
            {
                await player.PlayAsync(nextTrack);
                await LoggingService.LogInformationAsync("Music", $"[{player.VoiceChannel.Guild.Name}] Bot Now Playing: {nextTrack.Title} - {nextTrack.Uri}");
                await player.TextChannel.SendMessageAsync("", false, await EmbedHandler.CreateBasicEmbed("Now Playing", $"[{nextTrack.Title}]({nextTrack.Uri})", Color.Blue));
            }
        }
        #endregion

        #region Other

        public async Task<Embed> DisplayStatsAsync()
        {
            var node = lavalink.DefaultNode.Stats;
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
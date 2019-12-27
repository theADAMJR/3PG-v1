using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace Bot3PG.Modules.Music
{
    [Color(45, 25, 25)]
    [RequireUserPermission(GuildPermission.Speak)]
    [RequireBotPermission(GuildPermission.Speak)]
    public sealed class Music : CommandBase
    {
        private readonly string moduleName = "Music 🎵";

        public static AudioService AudioService { get; internal set; }

        private LavaPlayer player => AudioService.Player;
        private LavaSocketClient lavaClient => AudioService.LavaClient;
        private LavaRestClient lavaRestClient => AudioService.LavaRestClient;

        private ConcurrentDictionary<ulong, int> Votes; 

        [Command("Join"), Alias("J")]
        [Summary("Get bot to join your voice channel")]
        [RequireUserPermission(GuildPermission.Connect)]
        [RequireBotPermission(GuildPermission.Connect)]
        public async Task JoinAndPlay()
        {
            var user = Context.User as SocketGuildUser;
            if (user.VoiceChannel is null)
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"You must be in a channel first, for {Context.Client.CurrentUser.Mention} to join.", Color.Red));
                return;
            }
            await lavaClient.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
            await lavaClient.MoveChannelsAsync(user.VoiceChannel);
            
            var player = lavaClient.GetPlayer(Context.Guild.Id);
            await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Now connected to `{user.VoiceChannel.Name}` and bound to `{player.TextChannel.Name}`.", Color.Blue));
        }

        [Command("Leave"), Alias("L")]
        [Summary("Get bot to leave your voice channel")]
        [RequireUserPermission(GuildPermission.Connect)]
        [RequireBotPermission(GuildPermission.Connect)]
        public async Task Leave()
        {
            CurrentGuild ??= await Guilds.GetAsync(Context.Guild);
            string prefix = CurrentGuild.General.CommandPrefix;

            try
            {
                var player = lavaClient.GetPlayer(Context.Guild.Id);
                if (player is null)
                {
                    await SendNoPlayerPrompt();
                    return;
                }                
                else if (player.IsPlaying)
                {
                    await player.StopAsync();
                }                
                var channel = player.VoiceChannel;
                await lavaClient.DisconnectAsync(channel);
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"I've left `{channel.Name}`. Thank you for playing music 🎵.", Color.Blue));
            }
            catch (InvalidOperationException ex)
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, ex.ToString()));
                return;
            }
        }

        [Command("Play"), Alias("YouTube", "P")]
        [Summary("Search YouTube for tracks to play")]
        [RequireUserPermission(GuildPermission.Speak)]
        [RequireBotPermission(GuildPermission.Speak), RequireBotPermission(GuildPermission.Connect)]
        public async Task Play([Remainder]string query)
        {
            var user = Context.User as SocketGuildUser;
            if (user.VoiceChannel is null) 
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"You must be in a channel first, for {Context.Client.CurrentUser.Mention} to play.", Color.Red));
                return;
            }
            try
            {
                await lavaClient.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
                await lavaClient.MoveChannelsAsync(user.VoiceChannel);

                var player = lavaClient.GetPlayer(Context.Guild.Id);

                query = query.Replace("https://www.youtube.com/watch?v=", "");
                var search = await lavaRestClient.SearchYouTubeAsync(query);
                if (search.LoadType == LoadType.NoMatches) 
                {
                    await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, $"OOF! I wasn't able to find anything for '{query}'."));
                    return;
                }

                var track = search.Tracks.FirstOrDefault();
                if (track is null)
                {
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, "No results found.", Color.Red));
                    return;
                }
                else if (track.Length > TimeSpan.FromHours(CurrentGuild.Music.MaxTrackHours)) 
                {
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Track duration must be less than `{CurrentGuild.Music.MaxTrackHours} hours`.", Color.Red));
                    return;
                }

                if (player.CurrentTrack != null || player.IsPaused)
                {
                    player.Queue.Enqueue(track);
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"{Hyperlink(track)} has been added to queue.", Color.Blue));
                }
                else
                {
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"**Now playing**: {Hyperlink(track)}", Color.Blue));
                    await player.PlayAsync(track);
                    await player.SetVolumeAsync(CurrentGuild.Music.DefaultVolume);
                }
            }
            catch (Exception ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, ex.ToString())); }
        }

        [Command("Stop"), Alias("S")]
        [Summary("Stop all music playback")]
        [RequireUserPermission(GuildPermission.Speak)]
        [RequireBotPermission(GuildPermission.Speak)]
        public async Task Stop()
        {
            try
            {
                var player = lavaClient.GetPlayer(Context.Guild.Id);
                if (player is null)
                {
                    await SendNoPlayerPrompt();
                    return;
                }
                if (player.IsPlaying)
                {
                    await player.StopAsync();
                }
                foreach (var track in player.Queue.Items)
                {
                    player.Queue.Dequeue();
                }
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, "I've stopped playback and the playlist has been cleared.", Color.Blue));
            }
            catch (Exception ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, ex.ToString())); }
        }

        [Command("Queue"), Alias("List", "Playlist", "Q")]
        [Summary("Display currently playing or listed tracks")]
        public async Task Queue()
        {
            try
            {
                var player = lavaClient.GetPlayer(Context.Guild.Id);
                if (player is null)
                {
                    await SendNoPlayerPrompt();
                    return;
                }
                else if (player.IsPlaying)
                {
                    if (player.Queue.Count < 1 && player.CurrentTrack != null) 
                    {
                        await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"**Now Playing**: {Hyperlink(player.CurrentTrack)}\n\nNothing else is queued.", Color.Blue));
                        return;
                    }
                    
                    string description = "";
                    int trackNum = 2;
                    foreach (LavaTrack track in player.Queue.Items)
                    {
                        description += $"**[{trackNum}]**: {Hyperlink(track)}\n";
                        trackNum++;
                    }
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, 
                        $"**Now Playing**: {Hyperlink(player.CurrentTrack)}\n\n{description}", Color.Blue));
                    return;
                }
                await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, "Player doesn't seem to be playing anything right now."));
            }
            catch {}
        }

        [Command("Skip"), Alias("Next")]
        [Summary("Play next track in queue")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Skip()
        {
            int count = 1;
                        
            var player = lavaClient.GetPlayer(Context.Guild.Id);
            if (player is null) 
            {
                await SendNoPlayerPrompt();
                return;
            }
            else if (player.Queue.Count < 1) 
            {
                await SendNotPlayingPrompt("skip");
                return;
            }
            else if (player.Queue.Count < count)
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, "Tracks to skip must be less than playlist size.", Color.Red));
                return;                    
            }
            /*if (CurrentGuild.Music.VoteToSkip)
            {
                var channelMembers = player.VoiceChannel.GetUsersAsync();
                int memberCount = await channelMembers.Count();
                int votes = 0;
                votes++;
                
                bool hasEnoughVotes = CurrentGuild.Music.AllVotesToSkip ? votes == memberCount : votes >= (memberCount / 2);
                if (!hasEnoughVotes)
                {
                    string requirement = CurrentGuild.Music.AllVotesToSkip ? "Every channel member is" : "At least 50% of members are";
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"{requirement} required to vote to skip this track", Color.Blue));
                    return;
                }
            }*/
            try
            {
                for (int i = 0; i < count; i++)
                {
                    var oldTrack = await player.SkipAsync();
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"{Hyperlink(oldTrack)} successfully skipped", Color.Blue));
                    await Task.Delay(150);
                }
            }
            catch (Exception ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, ex.ToString())); }
        }

        [Command("Volume"), Alias("V")]
        [Summary("View or set the volume of tracks")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Volume(int volume = -1)
        {
            var player = lavaClient.GetPlayer(Context.Guild.Id);
            if (player is null) 
            {
                await SendNoPlayerPrompt();
            }
            else if (volume == -1)
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"**Current Volume:** {player.CurrentVolume}", Color.Blue));
            }
            else if (volume < 0 || volume > 200) 
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, "Volume must be between 0 and 200."));
            }
            else
            {
                try
                {
                    await player.SetVolumeAsync(volume);
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Volume has been set to `{volume}`.", Color.Blue));
                }
                catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, ex.ToString())); }
            }
        }

        [Command("Replay"), Alias("Position", "Seek")]
        [Summary("Go to current track position in seconds")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Replay(int seconds = -1)
        {
            var player = lavaClient.GetPlayer(Context.Guild.Id);
            if (player is null) 
            {
                await SendNoPlayerPrompt();
                return;
            }
            else if (player.CurrentTrack is null)
            {
                await SendNotPlayingPrompt("Replay");
                return;
            }
            
            var trackLength = player.CurrentTrack.Length;
            int totalSeconds = (int)trackLength.TotalSeconds;
            if (seconds == -1)
            {
                await ReplyAsync(embed: EmbedHandler.CreateBasicEmbed(moduleName, $"{Hyperlink(player.CurrentTrack)} is `{totalSeconds}` seconds long.", Color.Blue));
                return;
            }
            if (seconds < 0 || seconds > totalSeconds)
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Track length must be between `0` seconds and `{totalSeconds}`.", Color.Red));
                return;
            }
            await player.SeekAsync(TimeSpan.FromSeconds(seconds));
            await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Position is now at {GetPosition(player.CurrentTrack)}.", Color.Blue));
        }

        [Command("Pause")]
        [Summary("Pause playback, if playing")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Pause()
        {            
            try
            {
                var player = lavaClient.GetPlayer(Context.Guild.Id);
                if (player is null)
                {
                    await SendNoPlayerPrompt();
                    return;
                }
                else if (!player.IsPaused)
                {
                    await player.PauseAsync();
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"**Paused:** {player.CurrentTrack.Title}", Color.Blue));
                }
                else
                {
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Track already paused.", Color.Red));
                }
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, ex.Message)); }
        }
        
        [Command("Pause"), Alias("Resume")]
        [Summary("Resume playback, if playing")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Resume()
        {            
            try
            {
                var player = lavaClient.GetPlayer(Context.Guild.Id);
                if (player is null)
                {
                    await SendNoPlayerPrompt();
                    return;
                }
                else if (player.IsPaused)
                {
                    await player.ResumeAsync();
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"**Resumed:** Now Playing {player.CurrentTrack.Title}.", Color.Blue));
                }
                else
                {
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Track already resumed.", Color.Red));
                }
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, ex.Message)); }
        }
        
        [Command("Remove"), Alias("RM")]
        [Summary("Remove a playlist item by its number")]
        public async Task Remove(int number = 2)
        {
            var player = lavaClient.GetPlayer(Context.Guild.Id);
            if (player is null)
            {
                await SendNoPlayerPrompt();
            }
            else if (number <= 1 || player.Queue.Items.Count() < number - 1)
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, "Track to remove must be less than playlist size and greater than 2.", Color.Red));
            }
            else
            {
                var oldTrack = player.Queue.RemoveAt(number - 2) as LavaTrack;
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Removed {Hyperlink(oldTrack)} from playlist.", Color.Blue));
            }
        }

        [Command("Shuffle")]
        [Summary("Randomly shuffle a playlist")]
        public async Task Shuffle()
        {
            var player = lavaClient.GetPlayer(Context.Guild.Id);
            if (player is null)
            {
                await SendNoPlayerPrompt();
                return;
            }
            player.Queue.Shuffle();
            await ReplyAsync(EmbedHandler.CreateBasicEmbed(moduleName, $"Shuffled playlist.", Color.Blue));
        }

        [Command("NowPlaying"), Alias("NP")]
        [Summary("Show info about the current playing track")]
        public async Task NowPlaying()
        {
            var player = lavaClient.GetPlayer(Context.Guild.Id);
            if (player is null)
            {
                await SendNoPlayerPrompt();
            }
            else if (!player.IsPlaying)
            {
                await SendNotPlayingPrompt("show track");
            }
            else
            {
                var track = player.CurrentTrack;
                string thumbnail = await track.FetchThumbnailAsync();

                var embed = new EmbedBuilder();
                embed.WithTitle("Now Playing 🎶");
                embed.WithThumbnailUrl(thumbnail);
                embed.AddField($"[{track.Title}]({track.Uri})", GetPosition(track));
                embed.WithColor(Color.Blue);

                await ReplyAsync(embed);             
            }
        }

        private async Task SendNoPlayerPrompt()
        {
            CurrentGuild ??= await Guilds.GetAsync(Context.Guild);
            string prefix = CurrentGuild.General.CommandPrefix;
            await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, 
            $"Could not get player.\nAre you using {Context.Client.CurrentUser.Mention} right now? Type `{prefix}help` for more info."));
        }

        private async Task SendNotPlayingPrompt(string action)
        {
            CurrentGuild ??= await Guilds.GetAsync(Context.Guild);
            string prefix = CurrentGuild.General.CommandPrefix;           
            await ReplyAsync(EmbedHandler.CreateErrorEmbed(moduleName, $"Unable to {action} as there are one or no songs playing.\nDid you mean `{prefix}stop`?"));
        }

        public static string GetPosition(LavaTrack track) => $"`{GetDuration(track.Position)}`/`{GetDuration(track.Length)}`";
        public static string GetDuration(TimeSpan timeSpan) => timeSpan.ToString(timeSpan > TimeSpan.FromHours(1) ? @"hh\:mm\:ss" : @"mm\:ss");
        public static string Hyperlink(LavaTrack track) => AudioService.Hyperlink(track);
    }
}
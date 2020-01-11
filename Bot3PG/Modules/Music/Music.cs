using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Bot3PG.Data;
using Bot3PG.Handlers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Bot3PG.Data.Structs;
using System.Collections.Generic;

namespace Bot3PG.Modules.Music
{
    [Color(45, 25, 25)]
    [RequireUserPermission(GuildPermission.Speak)]
    [RequireBotPermission(GuildPermission.Speak)]
    public sealed class Music : CommandBase
    {
        internal override string ModuleName => "Music 🎵";
        internal override Color ModuleColour => Color.Blue;

        public static AudioService AudioService { get; internal set; }
        public LavaPlayer Player { get; internal set; }

        private LavaSocketClient lavaClient => AudioService.LavaClient;
        private LavaRestClient lavaRestClient => AudioService.LavaRestClient;

        [Command("Join"), Alias("J")]
        [Summary("Get bot to join your voice channel")]
        [RequireUserPermission(GuildPermission.Connect)]
        [RequireBotPermission(GuildPermission.Connect)]
        public async Task JoinAndPlay()
        {
            try
            {
                var user = Context.User as SocketGuildUser;
                Player = await JoinAndGetPlayer();
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"Now connected to `{user.VoiceChannel.Name}` and bound to `{Player.TextChannel.Name}`.", Color.Blue));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Leave"), Alias("L")]
        [Summary("Get bot to leave your voice channel")]
        [RequireUserPermission(GuildPermission.Connect)]
        [RequireBotPermission(GuildPermission.Connect)]
        public async Task Leave()
        {
            try
            {
                var botUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
                if (botUser.VoiceChannel is null) 
                    throw new InvalidOperationException($"I'm not in a voice channel.");

                Player = await JoinAndGetPlayer();
                if (Player.IsPlaying)
                    await Player.StopAsync();

                var channel = Player.VoiceChannel;
                await lavaClient.DisconnectAsync(channel);
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"I've left `{channel.Name}`. Thank you for playing music 🎵.", Color.Blue));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Play"), Alias("P")]
        [Summary("Search YouTube for tracks to play")]
        [RequireUserPermission(GuildPermission.Speak)]
        [RequireBotPermission(GuildPermission.Speak), RequireBotPermission(GuildPermission.Connect)]
        public async Task Play([Remainder]string query)
        {
            try
            {
                Player = await JoinAndGetPlayer();

                query = query.Replace("https://www.youtube.com/watch?v=", "");
                var search = await lavaRestClient.SearchYouTubeAsync(query);
                if (search.LoadType == LoadType.NoMatches)
                {
                    await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, $"OOF! I wasn't able to find anything for '{query}'."));
                    return;
                }

                var track = search.Tracks.FirstOrDefault();
                if (track is null)
                {
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, "No results found.", Color.Red));
                    return;
                }
                else if (track.Length > TimeSpan.FromHours(CurrentGuild.Music.MaxTrackHours))
                    throw new ArgumentException($"Track duration must be less than `{CurrentGuild.Music.MaxTrackHours} hours`.");

                if (Player.CurrentTrack != null || Player.IsPaused)
                {
                    Player.Queue.Enqueue(track);
                    AddEnqueuedTrack(track);
                    await SendEnqueuedEmbed(track);
                }
                else
                {
                    AddEnqueuedTrack(track);
                    await Player.PlayAsync(track);
                    await Player.SetVolumeAsync(CurrentGuild.Music.DefaultVolume);
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"**Now playing**: {FullHyperlink(track)}", Color.Blue));
                }
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Stop"), Alias("S")]
        [Summary("Stop all music playback")]
        [RequireUserPermission(GuildPermission.Speak)]
        [RequireBotPermission(GuildPermission.Speak)]
        public async Task Stop()
        {
            try
            {
                await CheckPlayer();
                
                if (Player.IsPlaying)
                    await Player.StopAsync();
                    
                foreach (var track in Player.Queue.Items)
                    Player.Queue.Dequeue();
                
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, "I've stopped playback and the playlist has been cleared.", Color.Blue));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }


        [Command("Skip"), Alias("Next")]
        [Summary("Play the next queued track")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Skip(int count = 1)
        {
            try
            {
                await CheckPlayer();
                await CheckIsPlaying("skip");

                if (count < 1 || count > Player.Queue.Count)
                    throw new InvalidOperationException("Tracks to skip must be within playlist size.");

                for (int i = 1; i < count; i++)
                    Player.Queue.Dequeue();
                
                var oldTrack = await Player.SkipAsync();

                var message = count > 1 ? $"`{count}` tracks successfully skipped." : $"{Hyperlink(oldTrack)} successfully skipped";
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, message, Color.Blue));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Volume"), Alias("V")]
        [Summary("View or set the volume of tracks")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Volume(int volume = -1)
        {
            try
            {
                await CheckPlayer();

                if (volume == -1)
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"**Current Volume:** `{Player.CurrentVolume}`", Color.Blue));
                else if (volume < 0 || volume > 200)
                    throw new ArgumentException("Volume must be between 0 and 200.");
                else
                {
                    await Player.SetVolumeAsync(volume);
                    string warning = volume > 150 && CurrentGuild.Music.HeadphoneWarning ? "**HEADPHONE WARNING** ⚠\n" : "";
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"{warning}Volume has been set to `{volume}`.", Color.Blue));
                }
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Seek"), Alias("Position", "Pos")]
        [Summary("Go to current track position in seconds")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Replay(int seconds = -1)
        {
            try
            {
                await CheckPlayer();
                await CheckIsPlaying("replay");
                
                var trackLength = Player.CurrentTrack.Length;
                int totalSeconds = (int)trackLength.TotalSeconds;
                if (seconds == -1)
                {
                    await ReplyAsync(embed: EmbedHandler.CreateBasicEmbed(ModuleName, $"{Hyperlink(Player.CurrentTrack)} is `{totalSeconds}` seconds long.", Color.Blue));
                    return;
                }
                if (seconds < 0 || seconds > totalSeconds)
                    throw new InvalidOperationException($"Track length must be between `0` seconds and `{totalSeconds}`.");

                await Player.SeekAsync(TimeSpan.FromSeconds(seconds));
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"Position is now at {GetPosition(Player.CurrentTrack)}.", Color.Blue));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Pause")]
        [Summary("Pause playback, if playing")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Pause()
        {            
            try
            {
                await CheckPlayer();

                if (Player.IsPaused)
                    throw new InvalidOperationException($"Track already paused.");

                await Player.PauseAsync();
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"**Paused:** {Hyperlink(Player.CurrentTrack)}", Color.Blue));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }
        
        [Command("Resume")]
        [Summary("Resume playback, if playing")]
        [RequireUserPermission(GuildPermission.Speak)]
        public async Task Resume()
        {            
            try
            {
                await CheckPlayer();

                if (!Player.IsPaused)
                    throw new InvalidOperationException($"Track already resumed.");

                await Player.ResumeAsync();
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"**Resumed:** Now Playing {Hyperlink(Player.CurrentTrack)}.", Color.Blue));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }
        
        [Command("Remove"), Alias("RM")]
        [Summary("Remove a playlist track by its number")]
        public async Task Remove(int number = 2)
        {
            try
            {
                await CheckPlayer();

                if (number <= 1 || Player.Queue.Items.Count() < number - 1)
                    throw new InvalidOperationException("Track to remove must be less than playlist size and greater than 2.");
                else
                {
                    var oldTrack = Player.Queue.RemoveAt(number - 2) as LavaTrack;
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"Removed {Hyperlink(oldTrack)} from playlist.", Color.Blue));
                }
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Shuffle")]
        [Summary("Randomly shuffle a playlist")]
        public async Task Shuffle()
        {
            try
            {
                await CheckPlayer();

                Player.Queue.Shuffle();
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"Shuffled playlist.", Color.Blue));                
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("NowPlaying"), Alias("NP")]
        [Summary("Show info about the current playing track")]
        public async Task NowPlaying()
        {
            try
            {
                await CheckPlayer();
                await CheckIsPlaying("show track");
                
                await SendNowPlayingEmbed(Player);                
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        
        [Command("Replay")]
        [Summary("Readd the last track played to the queue")]
        public async Task Replay()
        {
            try
            {
                Player ??= await JoinAndGetPlayer();

                AudioService.Tracks.TryGetValue(Context.Guild.Id, out var trackOptions);
                string lastTrackID = trackOptions?.LastOrDefault().ID;
                if (trackOptions is null || lastTrackID is null)
                {
                    await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, "👀 No tracks found to replay."));
                    return;
                }

                var search = await lavaRestClient.SearchTracksAsync(lastTrackID);
                var track = search.Tracks.First();

                if (Player.IsPlaying)
                {
                    Player.Queue.Enqueue(track);
                    await SendEnqueuedEmbed(track);
                }
                else
                {
                    await Player.PlayAsync(track);
                    await SendNowPlayingEmbed(Player);
                }                
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Queue"), Alias("List", "Playlist", "Q")]
        [Summary("Show currently playing or listed tracks")]
        public async Task Queue()
        {
            try
            {
                await CheckPlayer();
                await CheckIsPlaying("show playlist");
                
                string description = null;
                int trackNum = 2;
                foreach (LavaTrack track in Player.Queue.Items)
                {
                    description += $"**[{trackNum}]**: {FullHyperlink(track)}\n";
                    trackNum++;
                }
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, 
                    $"**Now Playing**: {FullHyperlink(Player.CurrentTrack)}\n\n{(description ?? "Nothing else is queued")}", Color.Blue));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("YouTube"), Alias("YT")]
        [Summary("Search YouTube for top results")]
        public async Task SearchYouTube([Remainder] string query)
        {
            var results = await lavaRestClient.SearchYouTubeAsync(query);
            if (results.Tracks.Count() <= 0)
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"No results found for `{query}`", Color.Red));
                return;
            }
            
            string details = "";
            var embed = new EmbedBuilder();
            
            var tracks = results.Tracks.Take(10).ToList();
            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                details += $"**[{i + 1}]** {Hyperlink(track)} by `{track.Author}`\n";
            }
            await ReplyAsync(EmbedHandler.CreateBasicEmbed($"Results for `{query}`\n", details, Color.Blue));
        }

        private async Task<LavaPlayer> JoinAndGetPlayer()
        {
            var user = Context.User as SocketGuildUser;
            if (user.VoiceChannel is null) throw new InvalidOperationException($"You must be in a channel first, for me to play.");

            await lavaClient.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
            await lavaClient.MoveChannelsAsync(user.VoiceChannel);
            return lavaClient.GetPlayer(Context.Guild.Id);
        }

        private async Task CheckPlayer()
        {
            CurrentGuild ??= await Guilds.GetAsync(Context.Guild);
            var prefix = CurrentGuild.General.CommandPrefix;

            Player = lavaClient.GetPlayer(Context.Guild.Id);
            if (Player is null) 
                throw new InvalidOperationException($"Could not get music player.\nAre you using me right now? Type `{prefix}help` for more info.");
        }

        private void AddEnqueuedTrack(LavaTrack track, bool repeat = false)
        {
            int maxPlaylistSize = 8;
            if (Player.Queue.Count >= maxPlaylistSize) 
                throw new InvalidOperationException($"Cannot add more than `{maxPlaylistSize}` tracks to playlist.");

            AudioService.Tracks.TryGetValue(Context.Guild.Id, out var queue);
            queue ??= new Queue<TrackOptions>();
            queue.Enqueue(new TrackOptions(track.Id, Context.User.Id, repeat));
            AudioService.Tracks[Context.Guild.Id] = queue;
        }

        private async Task CheckIsPlaying(string action)
        {
            CurrentGuild ??= await Guilds.GetAsync(Context.Guild);
            string prefix = CurrentGuild.General.CommandPrefix;

            if (!Player.IsPlaying) 
                throw new InvalidOperationException($"Unable to {action} as there are one or no songs playing.\nDid you mean `{prefix}stop`?");
        }

        public static string GetPosition(LavaTrack track) => $"`{GetDuration(track.Position)}`/`{GetDuration(track.Length)}`";
        public static string GetDuration(TimeSpan timeSpan) => timeSpan.ToString(timeSpan > TimeSpan.FromHours(1) ? @"hh\:mm\:ss" : @"mm\:ss");
        public string Hyperlink(LavaTrack track) => AudioService.Hyperlink(track);
        public string FullHyperlink(LavaTrack track) => $"{AudioService.Hyperlink(track)} - {GetRequestor(track)}";
        public string GetRequestor(LavaTrack track)
        {
            string hyperlink = AudioService.Hyperlink(track);
            var requestorID = AudioService.Tracks[Context.Guild.Id].First(t => t.ID == track.Id).RequestorID;
            return Context.Guild.GetUser(requestorID).Mention;
        }

        private async Task SendNowPlayingEmbed(LavaPlayer Player)
        {
            var track = Player.CurrentTrack;
            string thumbnail = await track.FetchThumbnailAsync();

            var embed = new EmbedBuilder();
            embed.WithTitle("Now Playing 🎶");
            embed.WithThumbnailUrl(thumbnail);
            embed.WithDescription($"[{track.Title}]({track.Uri})\n{GetPosition(track)}\n**Requested by:** {GetRequestor(Player.CurrentTrack)}");
            embed.WithColor(Color.Blue);

            await ReplyAsync(embed);
        }

        private async Task SendEnqueuedEmbed(LavaTrack track) => await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"{Hyperlink(track)} has been added to queue by {GetRequestor(track)}.", Color.Blue));
    }
}
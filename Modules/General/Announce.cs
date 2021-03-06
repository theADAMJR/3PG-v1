﻿using Bot3PG.Data;
using Bot3PG.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Bot3PG.Modules.General
{
    public static class Announce
    {
        public static async Task AnnounceUserJoin(SocketGuildUser guildUser)
        {
            if (guildUser.IsBot) return;

            var guild = await Guilds.GetAsync(guildUser.Guild);
            var announce = guild.General.Announce;

            var embed = new EmbedBuilder();

            var socketGuild = guildUser.Guild;
            var channel = socketGuild.GetTextChannel(announce.Welcomes.Channel) ?? socketGuild.SystemChannel ?? socketGuild.DefaultChannel;

            string imageURL = $"{Global.Config.DashboardURL}/api/servers/{guildUser.Guild.Id}/users/{guildUser.Id}/welcome";
            var stream = await CommandUtils.DownloadData(imageURL);
            
            if (!announce.DMNewUsers)
                await (channel as ISocketMessageChannel)?.SendFileAsync(stream, "welcome.png");
            else if (announce.DMNewUsers)
                await guildUser.SendFileAsync(stream, "welcome.png");
        }

        public static async Task AnnounceUserLeft(SocketGuildUser guildUser)
        {
            var user = await GuildUsers.GetAsync(guildUser);
            if (guildUser as SocketUser == Global.Client.CurrentUser || user.Status.IsBanned) return;

            var guild = await Guilds.GetAsync(guildUser.Guild);

            string imageURL = $"{Global.Config.DashboardURL}/api/servers/{guildUser.Guild.Id}/users/{guildUser.Id}/goodbye";
            var stream = await CommandUtils.DownloadData(imageURL);

            var channel = guildUser.Guild.GetTextChannel(guild.General.Announce.Goodbyes.Channel)
                ?? guildUser.Guild.SystemChannel
                ?? guildUser.Guild.DefaultChannel;

            await (channel as ISocketMessageChannel)?.SendFileAsync(stream, "goodbye.png");
        }
    }
}
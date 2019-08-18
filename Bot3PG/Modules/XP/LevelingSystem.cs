using Discord;
using Discord.WebSocket;
using System;
using Bot3PG.DataStructs;
using Bot3PG.Core.Data;
using System.Threading.Tasks;
using System.Collections;
using Discord.Rest;

namespace Bot3PG.Modules.XP
{
    public class LevelingSystem
    {
        private static int r = 255;
        private static int g = 0;
        private static int b = 0;

        public static async void ValidateMessageForXP(SocketUserMessage message)
        {
            if (message is null) return;

            var socketGuildUser = message.Author as SocketGuildUser;
            var user = new GuildUser(socketGuildUser);
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            if (user is null || guild is null || user.XP.InXPCooldown || message.Content.Length <= guild.Config.XPMessageLengthThreshold) return;

            else
            {
                user.XP.LastXPMsg = DateTime.Now;

                uint oldLevel = user.XP.LevelNumber;
                user.XP.EXP += guild.Config.XPPerMessage;
                uint newLevel = user.XP.LevelNumber;

                if (oldLevel != newLevel)
                {
                    var embed = new EmbedBuilder();
                    embed.WithColor(Color.Green);
                    embed.WithTitle("✨ **LEVEL UP!**");
                    embed.WithDescription(socketGuildUser.Mention + " just leveled up!");
                    embed.AddField("LEVEL", newLevel, true);
                    embed.AddField("XP", user.XP.EXP, true);

                    var levelUpMessage = await message.Channel.SendMessageAsync("", embed: embed.Build());                                      

                    /*for (int i = 0; i <= 255; i++)
                    {
                        await Task.Delay(1000);
                        await CycleRGB(levelUpMessage, socketGuildUser, newLevel);
                    }*/
                }
            }
        }

        private static async Task CycleRGB(RestUserMessage levelUpMessage, SocketGuildUser socketGuildUser, uint newLevel)
        {
            var cycleFactor = 15;
            if (r > 0 && b == 0)
            {
                r -= cycleFactor;
                g += cycleFactor;
            }
            if (g > 0 && r == 0)
            {
                g -= cycleFactor;
                b += cycleFactor;
            }
            if (b > 0 && g == 0)
            {
                r += cycleFactor;
                b -= cycleFactor;
            }
            Console.WriteLine(new Color(r, g, b));
            await levelUpMessage.ModifyAsync(m => m.Embed = new EmbedBuilder()
            {
                Color = new Color(r, g, b),
                Title = ("✨ **LEVEL UP!**"),
                Description = (socketGuildUser.Mention + " just leveled up!")
            }.Build());
        }
    }
}
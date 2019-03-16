using Bot3PG.DataStructs;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot3PG.Core.Users
{
    public static class Accounts
    {
        private static List<Account> accounts;

        private static string accountsFile = "Resources/accounts.json";

        static Accounts()
        {
            if(DataStorage.SaveExists(accountsFile))
            {
                accounts = DataStorage.LoadUserAccounts(accountsFile).ToList();
            }
            else
            {
                accounts = new List<Account>();
                SaveAccounts();
            }
        }

        public static void SaveAccounts()
        {
            DataStorage.SaveUserAccounts(accounts, accountsFile);
        }

        public static Account GetAccount(SocketGuildUser user)
        {
            return GetOrCreateAccount(user);
        }

        private static Account GetOrCreateAccount(SocketGuildUser user)
        {
            var result = from a in accounts
                         where a.ID == user.Id
                         where a.GuildID == user.Guild.Id
                         select a;

            var account = result.FirstOrDefault();
            if(account == null) account = CreateUserAccount(user);
            return account;
        }

        private static Account CreateUserAccount(SocketGuildUser user)
        {
            if (user.IsBot) return null;
            var newAccount = new Account()
            {
                ID = user.Id,
                GuildID = user.Guild.Id,
                Points = 0,
                XP = 0
            };

            accounts.Add(newAccount);
            SaveAccounts();
            return newAccount;
        }

        internal static List<Account> GetAccounts()
        {
            return accounts.ToList();
        }

        internal static List<Account> GetAccountsInGuild(SocketGuild guild)
        {
            var result = accounts.Where(acc => acc.GuildID == guild.Id);
            return result.ToList();
        }

        internal static Account GetAccountInGuild(SocketGuildUser user)
        {
            var result = from a in accounts
                         where a.GuildID == user.Guild.Id
                         where a.ID == user.Id
                         select a;

            var account = result.FirstOrDefault();
            if (account == null) account = CreateUserAccount(user as SocketGuildUser);
            return account;
        }

        internal static ulong GetUserGuild(SocketUser user)
        {
            var result = from a in accounts
                         where a.ID == user.Id
                         select a;
            
            var account = result.FirstOrDefault();
            if (account == null) account = CreateUserAccount(user as SocketGuildUser);
            return account.GuildID;
        }

        internal static void ResetUserAccount(SocketGuildUser user)
        {
            GetOrCreateAccount(user);
            GetOrCreateAccount(user).ID = user.Id;
            GetOrCreateAccount(user).GuildID = user.Guild.Id;
            GetOrCreateAccount(user).Points = 0;
            GetOrCreateAccount(user).XP = 0;
        }
    }
}
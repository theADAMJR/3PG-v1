using System;
using System.Threading.Tasks;
using Bot3PG.Handlers;

namespace Bot3PG
{
    public class Program
    {
        private static Task Main(string[] args) => new DiscordHandler().InitializeAsync();
    }
}
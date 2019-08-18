using Bot3PG.Services;
using System.Threading.Tasks;

namespace Bot3PG
{
    public class Program
    {       
        private static Task Main(string[] args) => new DiscordService().InitializeAsync();
    }
}
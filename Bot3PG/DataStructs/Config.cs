namespace Bot3PG.DataStructs
{
    public  class Config
    {
        public string Token { get; set; }
        public string GameStatus { get; set; }
        public string WelcomeLink { get; set; }
        public string WebappLink { get; set; }

        public DatabaseConfig DB;

        public struct DatabaseConfig
        {
            public string Server { get; set; }
            public string Database { get; set; }
            public string UserID { get; set; }
            public string Password { get; set; }
        }
    }
}
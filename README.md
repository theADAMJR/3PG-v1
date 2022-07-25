# Bot 3PG
The Creator

---

## Setup

### Create a config.json
```
{
    "Bot": {
        "Token": "your discord bot token",
        "Status": "threepg.xyz"
    },
    "DashboardURL": "http://localhost:3000",
    "LogSeverity": 4,
    "MongoURI": "mongodb://localhost/3PG-v1"
}
```

### Prerequisites
- .NET Core v2.1
- MongoDB Community Server
- Java 11

### Run
Make sure `Lavalink.jar` is running -> `cd dist/Lavalink && java -jar Lavalink.jar`.
Also make sure `mongod` is running.
Then type `dotnet run` or `F5` with VSCode.

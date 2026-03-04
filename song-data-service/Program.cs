namespace SongDataService
{
    public class Program {
        static async Task Main(params string[] args)
        {
            DatabaseHandler.Initialize();
            var server = new ServerHandler();
            await server.StartAsync();
        }

    }
}
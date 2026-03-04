using Microsoft.Data.Sqlite;

namespace SongDataService
{
    public class DatabaseHandler : IDisposable
    {
        private SqliteConnection _connection = new();
        private readonly string _source = @$"Data Source=data{Path.DirectorySeparatorChar}songs.db;Mode=ReadOnly";

        public static void Initialize()
        {
            SQLitePCL.Batteries.Init();
        }

        public DatabaseHandler()
        {
            _connection = new(_source);
        }

        public bool TryQuery(string query, out Dictionary<long, Dictionary<string, object?>> output, out Exception? ex)
        {
            try
            {
                output = Query(query);
                ex = null;
                return true;
            }
            catch (Exception e)
            {
                output = [];
                ex = e;
                Console.Error.WriteLine(e);
                return false;
            }
        }
        public Dictionary<long, Dictionary<string, object?>> Query(string query)
        {
            Dictionary<long, Dictionary<string, object?>> output = [];
            _connection.Open();

            var command = _connection.CreateCommand();
            command.CommandText = query;
            var reader = command.ExecuteReader();

            long i = 1;
            while (reader.Read())
            {
                Dictionary<string, object?> dict = [];
                for (int j = 0; j < reader.FieldCount; j++)
                    dict[reader.GetName(j)] = reader.IsDBNull(j) ? null : reader.GetValue(j);
                if (reader.FieldCount > 0) output[i++] = dict;
            }

            return output;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
using System.Data;
using MySql.Data.MySqlClient;

namespace Vbank.model
{
    public class DbConnection
    {
        private DbConnection()
        {
        }

        private const string DatabaseName = "Vbank";
        private const string ServerName = "localhost";
        private const string ServerPort = "3306";
        private const string Uid = "root";
        private const string Password = "";
        private const string SslMode = "none";
        private const string PersistSecurityInfo = "True";
        private const string Charset = "utf8";

        private MySqlConnection _connection;

        public MySqlConnection Connection => _connection;

        private static DbConnection _instance;

        public static DbConnection Instance()
        {
            var dbConnection = _instance != null ? _instance : (_instance = new DbConnection());
            return dbConnection;
        }

        public void OpenConnection()
        {
            if (_connection == null)
            {
                var connString =
                    string.Format(
                        "Server={0}; database={1}; UID={2}; password={3}; persistsecurityinfo={4};port={5};SslMode={6}; charset = {7};",
                        ServerName, DatabaseName, Uid, Password, PersistSecurityInfo, ServerPort, SslMode, Charset);
                _connection = new MySqlConnection(connString);
                _connection.Open();
            }
            else if (Connection.State == ConnectionState.Closed)
            {
                Connection.Open();
            }
        }

        public void CloseConnection()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
                _connection.Close();
        }
    }
}
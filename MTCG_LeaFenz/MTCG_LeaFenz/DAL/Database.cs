using MTCG_LeaFenz.BusinessLayer.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;


namespace MTCG_LeaFenz.DAL
{
    internal class Database
    {
        //private static List<User> users = new List<User>();

        //public List<User> GetUsers() => users;
        //public void AddUser(User user) => users.Add(user);

        private readonly string _connectionString = "Host=localhost;Username=postgres;Password=lea1234;Database=postgres";

        public User GetUser(string username)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);
            return db.QueryFirstOrDefault<User>("SELECT * FROM users WHERE username = @Username", new { Username = username });
        }

        public bool InsertUser(User user)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);
            int rowsAffected = db.Execute("INSERT INTO users (username, password, coins, elo) VALUES (@Username, @Password, @Coins, @ELO)", user);
            return rowsAffected > 0;
        }

}
}

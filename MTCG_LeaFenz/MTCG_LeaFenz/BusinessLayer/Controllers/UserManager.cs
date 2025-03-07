using MTCG_LeaFenz.BusinessLayer.Models;
using MTCG_LeaFenz.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;
using Npgsql;
using System.Data;
using Dapper;

namespace MTCG_LeaFenz.BusinessLayer.Controllers
{
    internal class UserManager
    {
        private readonly Database _db;
        private readonly string _connectionString;

        public UserManager(Database db, string connectionString)
        {
            _db = db;
            _connectionString = connectionString;
        }

        public bool RegisterUser(string username, string password)
        {
            if (_db.GetUser(username) != null) return false; // User existiert bereits
            string passwordHash = ComputeHash(password);
            return _db.InsertUser(new User { Username = username, Password = passwordHash });
        }

        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        public string? LoginUser(string username, string password)
        {
            User? user = _db.GetUser(username);

            if (user == null || user.Password != ComputeHash(password)) // Prüft Passwort
            {
                Console.WriteLine($"❌ Falsche Anmeldedaten für {username}");
                return null;
            }

            // Generiere ein Token (Hier einfach: "username-mtcgToken")
            string token = $"{username}-mtcgToken";
            Console.WriteLine($"✅ Login erfolgreich für {username}, Token: {token}");
            return token;
        }

        public User? GetUser(string username)
        {
            return _db.GetUser(username);
        }

        public List<User> GetScoreboard()
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);
            return db.Query<User>("SELECT username, elo FROM users ORDER BY elo DESC").ToList();
        }


    }
}

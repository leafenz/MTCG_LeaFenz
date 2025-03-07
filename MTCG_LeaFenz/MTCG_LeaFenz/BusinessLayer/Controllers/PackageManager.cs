using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace MTCG_LeaFenz.BusinessLayer.Controllers
{
    internal class PackageManager
    {
        private readonly string _connectionString;

        public PackageManager(string connectionString)
        {
            _connectionString = connectionString;
        }
        public bool BuyPackage(string username)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);
            db.Open();

            using var transaction = db.BeginTransaction();

            // 1️⃣ Prüfen, ob der User genug Coins hat
            int userCoins = db.QueryFirstOrDefault<int>("SELECT coins FROM users WHERE username = @Username", new { Username = username });
            if (userCoins < 5)
            {
                return false; // ❌ Nicht genug Coins
            }

            // 2️⃣ Ein zufälliges Paket auswählen
            var package = db.QueryFirstOrDefault<Guid>("SELECT id FROM packages LIMIT 1");
            Console.WriteLine(package);
            if (package == null)
            {
                return false; // ❌ Kein Paket verfügbar
            }

            // 3️⃣ Karten in `user_cards` übertragen
            db.Execute(@"
        INSERT INTO user_cards (username, card_id, is_in_deck)
        SELECT @Username, card_id, false FROM package_cards WHERE package_id = @PackageId",
                new { Username = username, PackageId = package });

            // 4️⃣ Dem User 5 Coins abziehen
            db.Execute("UPDATE users SET coins = coins - 5 WHERE username = @Username", new { Username = username });

            // 5️⃣ Das Paket entfernen
            db.Execute("DELETE FROM packages WHERE id = @PackageId", new { PackageId = package });

            transaction.Commit();
            return true; // ✅ Kauf erfolgreich
        }

    }
}

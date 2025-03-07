using MTCG_LeaFenz.BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using System.Diagnostics;

namespace MTCG_LeaFenz.BusinessLayer.Controllers
{
    internal class TradeManager
    {
        private readonly string _connectionString;

        public TradeManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Trading> GetAllTrades()
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);
            return db.Query<Trading>(
                "SELECT id, owner, offered_card AS OfferedCardId, required_type AS RequiredType, required_min_damage AS RequiredMinDamage, is_active AS IsActive FROM trades"
            ).ToList();
        }
        public bool CreateTrade(Trading trade)
        {
            using IDbConnection db = new Npgsql.NpgsqlConnection(_connectionString);

            string query = @"
                INSERT INTO trades (id, owner, offered_card, required_type, required_min_damage, is_active)
                VALUES (@Id, @Owner, @OfferedCardId, @RequiredType, @RequiredMinDamage, @IsActive)";

            int rowsAffected = db.Execute(query, trade);
            return rowsAffected > 0;
        }
        public bool ExecuteTrade(Guid tradeId, string trader, Guid traderCardId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            // 1️⃣ Trade suchen
            var trade = connection.QueryFirstOrDefault<Trading>(
                "SELECT * FROM trades WHERE id = @TradeId AND is_active = TRUE",
                new { TradeId = tradeId });

            if (trade == null)
            {
                return false; // ❌ Trade nicht gefunden oder nicht mehr aktiv
            }

            // 2️⃣ Überprüfen, ob der Trader die angebotene Karte besitzt
            bool ownsCard = connection.ExecuteScalar<bool>(
                "SELECT COUNT(*) FROM user_cards WHERE username = @Trader AND card_id = @TraderCardId",
                new { Trader = trader, TraderCardId = traderCardId });

            if (!ownsCard)
            {
                return false; // ❌ Spieler besitzt die Karte nicht
            }

            // 3️⃣ Überprüfen, ob die Karte die Trade-Bedingungen erfüllt
            var traderCard = connection.QueryFirstOrDefault<Card>(
                "SELECT * FROM cards WHERE id = @TraderCardId",
                new { TraderCardId = traderCardId });

            if (traderCard == null || traderCard.CardType != trade.RequiredType || traderCard.Damage < trade.RequiredMinDamage)
            {
                return false; // ❌ Karte erfüllt die Bedingungen nicht
            }

            // 4️⃣ Karten zwischen den Spielern tauschen
            connection.Execute(
                "UPDATE user_cards SET username = @Trader WHERE card_id = @OfferedCardId",
                new { Trader = trader, OfferedCardId = trade.OfferedCardId },
                transaction
            );

            connection.Execute(
                "UPDATE user_cards SET username = @Owner WHERE card_id = @TraderCardId",
                new { Owner = trade.Owner, TraderCardId = traderCardId },
                transaction
            );

            // 5️⃣ Trade als abgeschlossen markieren
            connection.Execute(
                "UPDATE trades SET is_active = FALSE WHERE id = @TradeId",
                new { TradeId = tradeId },
                transaction
            );

            transaction.Commit(); // ✅ Trade erfolgreich durchgeführt
            return true;
        }

        public bool DeleteTrade(Guid tradeId, string username)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            // Prüfen, ob der Nutzer wirklich der Besitzer des Trades ist
            var trade = db.QueryFirstOrDefault<Trading>(
                "SELECT * FROM trades WHERE id = @TradeId AND owner = @Username",
                new { TradeId = tradeId, Username = username });

            if (trade == null)
            {
                return false; 
            }

            int rowsAffected = db.Execute("DELETE FROM trades WHERE id = @TradeId", new { TradeId = tradeId });

            return rowsAffected > 0; // ✅ Erfolg, falls ein Eintrag gelöscht wurde
        }


    }
}

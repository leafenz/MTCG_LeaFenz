using MTCG_LeaFenz.BusinessLayer.Models;
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
    internal class CardManager
    {
        private readonly string _connectionString;

        public CardManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Card> GetUserCards(string username)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            // Abrufen aller Karten eines Spielers
            var cards = db.Query<Card>(
                "SELECT c.id, c.name, c.damage, c.element_type, c.card_type " +
                "FROM user_cards uc " +
                "JOIN cards c ON uc.card_id = c.id " +
                "WHERE uc.username = @Username",
                new { Username = username }
            ).ToList();
            Console.WriteLine($"DEBUG: {cards.Count} Karten für {username} gefunden.");
            return cards;
        }

        public List<Card> GetUserDeck(string username)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            var deck = db.Query<Card>(
                "SELECT c.id, c.name, c.damage, c.element_type, c.card_type " +
                "FROM user_cards uc " +
                "JOIN cards c ON uc.card_id = c.id " +
                "WHERE uc.username = @Username AND uc.is_in_deck = TRUE",
                new { Username = username }
            ).ToList();

            Console.WriteLine($"DEBUG: {deck.Count} Karten im Deck für {username} gefunden.");
            return deck;
        }

    }
}

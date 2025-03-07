using System;
using System.Collections.Generic;
using System.Linq;
using MTCG_LeaFenz.BusinessLayer.Models;
using Dapper;
using Npgsql;

namespace MTCG_LeaFenz.BusinessLayer.Controllers
{
    internal class BattleManager
    {
        private readonly string _connectionString;

        public BattleManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Battle StartBattle(string player1, string player2)
        {
            using var db = new NpgsqlConnection(_connectionString);

            // 1️⃣ Decks abrufen
            var deck1 = GetDeck(db, player1);
            var deck2 = GetDeck(db, player2);

            if (deck1.Count == 0 || deck2.Count == 0)
            {
                throw new Exception("Mindestens ein Spieler hat kein vollständiges Deck!");
            }

            // 2️⃣ Battle starten
            Battle battle = new Battle(player1, player2);
            SimulateBattle(deck1, deck2, battle);

            // 3️⃣ Gewinner-Überprüfung
            if (battle.Winner == null)
            {
                battle.Winner = deck1.Count > 0 ? player1 : (deck2.Count > 0 ? player2 : null);
            }

            // 4️⃣ Gewinner in DB speichern
            db.Execute("INSERT INTO battles (player1, player2, winner, log) VALUES (@Player1, @Player2, @Winner, @Log)",
                new { Player1 = player1, Player2 = player2, Winner = battle.Winner, Log = battle.Log });

            // 5️⃣ ELO-Update
            if (battle.Winner != null)
            {
                db.Execute("UPDATE users SET elo = elo + 10 WHERE username = @Winner", new { battle.Winner });
                string loser = battle.Winner == player1 ? player2 : player1;
                db.Execute("UPDATE users SET elo = elo - 5 WHERE username = @Loser", new { Loser = loser });
            }

            return battle;
        }

        private List<Card> GetDeck(NpgsqlConnection db, string username)
        {
            return db.Query<Card>(
                "SELECT c.* FROM user_cards uc JOIN cards c ON uc.card_id = c.id WHERE uc.username = @Username AND uc.is_in_deck = TRUE",
                new { Username = username }).ToList();
        }

        private void SimulateBattle(List<Card> deck1, List<Card> deck2, Battle battle)
        {
            Random rand = new Random();
            int rounds = 0;
            bool isTie;

            do
            {
                rounds++;
                Card card1 = deck1[rand.Next(deck1.Count)];
                Card card2 = deck2[rand.Next(deck2.Count)];

                battle.AddLog($"🃏 Runde {rounds}: {card1.Name} ({card1.Damage} DMG) vs {card2.Name} ({card2.Damage} DMG)");

                int damage1 = ApplyElementEffect(card1, card2);
                int damage2 = ApplyElementEffect(card2, card1);

                if (CheckSpecialCases(card1, card2))
                {
                    battle.AddLog("⚠️ Spezialregel aktiv! Kampf wurde beeinflusst.");
                    isTie = true;
                    continue; // Spezialfälle führen zu keinem Kartenwechsel
                }

                using var db = new NpgsqlConnection(_connectionString);

                if (damage1 > damage2)
                {
                    deck1.Add(card2);
                    deck2.Remove(card2);
                    battle.AddLog($"✅ {card1.Name} gewinnt die Runde und erhält {card2.Name}!");

                    db.Execute("UPDATE user_cards SET username = @Winner, is_in_deck = FALSE WHERE card_id = @CardId",
                        new { Winner = battle.Player1, CardId = card2.Id });

                    battle.Winner = battle.Player1; // Gewinner wird direkt gesetzt
                    isTie = false;
                }
                else if (damage1 < damage2)
                {
                    deck2.Add(card1);
                    deck1.Remove(card1);
                    battle.AddLog($"✅ {card2.Name} gewinnt die Runde und erhält {card1.Name}!");

                    db.Execute("UPDATE user_cards SET username = @Winner, is_in_deck = FALSE WHERE card_id = @CardId",
                        new { Winner = battle.Player2, CardId = card1.Id });

                    battle.Winner = battle.Player2; // Gewinner wird direkt gesetzt
                    isTie = false;
                }
                else
                {
                    battle.AddLog("🤝 Unentschieden! Beide behalten ihre Karten.");
                    isTie = true;
                }
            } while (isTie && rounds < 2); // Maximal eine extra Runde bei Unentschieden
        }



        private int ApplyElementEffect(Card card1, Card card2)
        {
            if (card1.CardType == "Spell" || card2.CardType == "Spell")
            {
                if (card1.ElementType == "Water" && card2.ElementType == "Fire") return card1.Damage * 2;
                if (card1.ElementType == "Fire" && card2.ElementType == "Normal") return card1.Damage * 2;
                if (card1.ElementType == "Normal" && card2.ElementType == "Water") return card1.Damage * 2;
                if (card1.ElementType == "Fire" && card2.ElementType == "Water") return card1.Damage / 2;
                if (card1.ElementType == "Normal" && card2.ElementType == "Fire") return card1.Damage / 2;
                if (card1.ElementType == "Water" && card2.ElementType == "Normal") return card1.Damage / 2;
            }
            return card1.Damage;
        }

        private bool CheckSpecialCases(Card card1, Card card2)
        {
            if (card1.Name.Contains("Goblin") && card2.Name.Contains("Dragon")) return true;
            if (card1.Name.Contains("Wizzard") && card2.Name.Contains("Ork")) return true;
            if (card1.Name.Contains("Knight") && card2.Name.Contains("WaterSpell")) return true;
            if (card1.Name.Contains("Kraken") && card2.CardType == "Spell") return true;
            if (card1.Name.Contains("FireElf") && card2.Name.Contains("Dragon")) return true;
            return false;
        }

        public string? FindOpponent(string player)
        {
            using var db = new NpgsqlConnection(_connectionString);

            // 1️⃣ Prüfen, ob ein Gegner bereits wartet (ältester Spieler zuerst)
            var opponent = db.QueryFirstOrDefault<string>(
                "SELECT username FROM battles_waiting WHERE username != @Player ORDER BY joined_at ASC LIMIT 1",
                new { Player = player });

            if (opponent != null)
            {
                // Falls ein wartender Spieler gefunden wurde, diesen aus der Warteliste entfernen
                db.Execute("DELETE FROM battles_waiting WHERE username = @Opponent", new { Opponent = opponent });
                return opponent;
            }

            // 2️⃣ Prüfen, ob der Spieler bereits wartet
            bool alreadyWaiting = db.QueryFirstOrDefault<int>(
                "SELECT COUNT(*) FROM battles_waiting WHERE username = @Player",
                new { Player = player }) > 0;

            if (!alreadyWaiting)
            {
                db.Execute("INSERT INTO battles_waiting (username, joined_at) VALUES (@Player, NOW())", new { Player = player });
            }

            // Kein Gegner gefunden, Spieler muss warten
            return null;
        }


    }
}

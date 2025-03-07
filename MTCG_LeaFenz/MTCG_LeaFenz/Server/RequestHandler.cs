using MTCG_LeaFenz.BusinessLayer.Controllers;
using MTCG_LeaFenz.BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MTCG_LeaFenz.Server
{
    internal class RequestHandler
    {
        private readonly UserManager _userManager;
        private readonly CardManager _cardManager;
        private readonly PackageManager _packageManager;
        private readonly TradeManager _tradeManager;
        private readonly BattleManager _battleManager;



        public RequestHandler(UserManager userManager, string connectionString)
        {
            _userManager = userManager;
            _cardManager = new CardManager(connectionString);
            _packageManager = new PackageManager(connectionString);
            _tradeManager = new TradeManager(connectionString);
            _battleManager= new BattleManager(connectionString);
        }



        public void HandleRequest(HttpListenerContext context)
        {
            string path = context.Request.RawUrl;
            string method = context.Request.HttpMethod;

            if (method == "POST" && path == "/users")
            {
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                string requestBody = reader.ReadToEnd();
                var userData = JsonSerializer.Deserialize<User>(requestBody);

                bool success = _userManager.RegisterUser(userData.Username, userData.Password);
                context.Response.StatusCode = success ? 201 : 409;
            }
            else
            {
                context.Response.StatusCode = 404;
            }
            if (method == "GET" && path == "/")
            {
                string responseText = "MTCG Server läuft!";
                context.Response.ContentType = "text/plain";
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            if (method == "POST" && path == "/sessions")
            {
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                string requestBody = reader.ReadToEnd();
                var loginData = JsonSerializer.Deserialize<User>(requestBody);

                string? token = _userManager.LoginUser(loginData.Username, loginData.Password);

                if (token != null)
                {
                    context.Response.StatusCode = 200;
                    byte[] buffer = Encoding.UTF8.GetBytes($"{{\"token\": \"{token}\"}}");
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    context.Response.StatusCode = 401; // Unauthorized
                }
            }
            if (method == "GET" && path == "/cards")
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                Console.WriteLine($"{username} ruft seine Karten ab...");

                var cards = _cardManager.GetUserCards(username);
                foreach (var card in cards)
                {
                    Console.WriteLine($" - {card.Name} ({card.ElementType}, {card.Damage} Damage)");
                }

                if (cards.Count == 0)
                {
                    Console.WriteLine("Keine Karten gefunden!");
                    context.Response.StatusCode = 404;
                }
                else
                {
                    Console.WriteLine("Karten gefunden! Senden...");
                    string responseText = JsonSerializer.Serialize(cards);
                    byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                    context.Response.StatusCode = 200;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }

            if (method == "GET" && path == "/deck")
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                Console.WriteLine($"{username} ruft sein Deck ab...");

                var deck = _cardManager.GetUserDeck(username);

                if (deck.Count == 0)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                string responseText = JsonSerializer.Serialize(deck);
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            if (method == "POST" && path == "/transactions/packages")
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                Console.WriteLine($"{username} möchte ein Kartenpaket kaufen!");

                bool success = _packageManager.BuyPackage(username);

                if (success)
                {
                    context.Response.StatusCode = 200; 
                    byte[] buffer = Encoding.UTF8.GetBytes("Package successfully purchased!");
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    byte[] buffer = Encoding.UTF8.GetBytes("No packages available or not enough coins.");
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }

            if (method == "GET" && path == "/scoreboard")
            {
                var scoreboard = _userManager.GetScoreboard();

                string responseText = JsonSerializer.Serialize(scoreboard);
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);

                Console.WriteLine("Scoreboard abgerufen!");
            }

            if (method == "GET" && path == "/tradings")
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403; 
                    return;
                }

                Console.WriteLine($"{username} ruft alle Trade-Angebote ab...");

                var trades = _tradeManager.GetAllTrades();

                if (trades.Count == 0)
                {
                    context.Response.StatusCode = 404; 
                    return;
                }

                string responseText = JsonSerializer.Serialize(trades);
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);                
            }
            if (method == "POST" && path == "/tradings")
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403; 
                    return;
                }

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                string requestBody = reader.ReadToEnd();
                var tradeData = JsonSerializer.Deserialize<Trading>(requestBody);

                if (tradeData == null || tradeData.OfferedCardId == Guid.Empty || string.IsNullOrEmpty(tradeData.RequiredType))
                {
                    context.Response.StatusCode = 400; 
                    return;
                }

                // Setze den Owner des Trades auf den authentifizierten Nutzer
                tradeData.Owner = username;
                tradeData.Id = Guid.NewGuid(); // Generiere eine neue ID
                tradeData.IsActive = true;

                bool success = _tradeManager.CreateTrade(tradeData);

                context.Response.StatusCode = success ? 201 : 500;
            }

            if (method == "POST" && path.StartsWith("/tradings/"))
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403; 
                    return;
                }

                string tradeIdString = path.Replace("/tradings/", "");
                if (!Guid.TryParse(tradeIdString, out Guid tradeId))
                {
                    context.Response.StatusCode = 400; 
                    return;
                }

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                string requestBody = reader.ReadToEnd();
                var tradeRequest = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);

                if (tradeRequest == null || !tradeRequest.ContainsKey("TraderCardId") || !Guid.TryParse(tradeRequest["TraderCardId"], out Guid traderCardId))
                {
                    context.Response.StatusCode = 400; 
                    return;
                }

                bool success = _tradeManager.ExecuteTrade(tradeId, username, traderCardId);

                context.Response.StatusCode = success ? 200 : 409;
            }

            if (method == "DELETE" && path.StartsWith("/tradings/"))
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                string tradeId = path.Replace("/tradings/", "").Trim();
                bool success = _tradeManager.DeleteTrade(Guid.Parse(tradeId), username);

                context.Response.StatusCode = success ? 200 : 404; 
            }



            if (method == "PUT" && path == "/deck")
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403; 
                    return;
                }

                Console.WriteLine($"{username} setzt sein Deck!");

                // 🔹 Hier kommt später die Deck-Logik rein
                context.Response.StatusCode = 200;
            }

            if (method == "POST" && path == "/battles")
            {
                if (!IsAuthenticated(context, out string username))
                {
                    context.Response.StatusCode = 403; // ❌ Zugriff verweigert
                    return;
                }

                Console.WriteLine($"✅ {username} tritt einem Kampf bei!");

                string opponent = _battleManager.FindOpponent(username);
                if (opponent == null)
                {
                    context.Response.StatusCode = 409; // ❌ Kein Gegner gefunden
                    return;
                }

                var battle = _battleManager.StartBattle(username, opponent);

                context.Response.StatusCode = 200;
                byte[] buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(battle));
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            context.Response.OutputStream.Close();
        }

        private bool IsAuthenticated(HttpListenerContext context, out string username)
        {
            username = "";

            // 1️⃣ `Authorization`-Header auslesen
            if (!context.Request.Headers.AllKeys.Contains("Authorization"))
            {
                Console.WriteLine("Kein Token in der Anfrage gefunden.");
                return false;
            }

            string token = context.Request.Headers["Authorization"];

            // 2️⃣ Token-Format prüfen
            if (!token.EndsWith("-mtcgToken"))
            {
                Console.WriteLine($"Ungültiges Token-Format: {token}");
                return false;
            }

            // 3️⃣ Username aus Token extrahieren
            username = token.Replace("-mtcgToken", "");

            // 4️⃣ Prüfen, ob der Nutzer existiert
            User? user = _userManager.GetUser(username);
            if (user == null)
            {
                Console.WriteLine($"Nutzer mit Token {token} existiert nicht.");
                return false;
            }

            Console.WriteLine($"Nutzer {username} authentifiziert.");
            return true;
        }


    }
}

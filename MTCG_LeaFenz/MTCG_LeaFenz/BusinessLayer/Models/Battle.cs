using System;
using System.Collections.Generic;
using System.Text;

namespace MTCG_LeaFenz.BusinessLayer.Models
{
    internal class Battle
    {
        public int Id { get; set; }
        public string Player1 { get; set; } = string.Empty;
        public string Player2 { get; set; } = string.Empty;
        public string? Winner { get; set; }  // null = Unentschieden
        public string Log { get; set; } = string.Empty;
        public DateTime BattleTime { get; set; } = DateTime.Now;

        public Battle() { }

        public Battle(string player1, string player2)
        {
            Player1 = player1;
            Player2 = player2;
            Log = "";
        }

        public void SetWinner(string winner)
        {
            Winner = winner;
        }

        public void AddLog(string logEntry)
        {
            Log += logEntry + "\n";
        }
    }
}

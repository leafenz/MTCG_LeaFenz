using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_LeaFenz.BusinessLayer.Models
{
    internal class User
    {
            public int Id { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public int Coins { get; set; } = 20;
            public int Elo { get; set; } = 100;
            public int GamesPlayed { get; set; } = 0;
    }
}

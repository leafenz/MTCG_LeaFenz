using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_LeaFenz.BusinessLayer.Models
{
    internal class Package
    {
        public Guid Id { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>(); //5 Karten
        public bool IsAvailable { get; set; } = true;  // Ob es noch gekauft werden kann

        public Package() { }

        public Package(List<Card> cards)
        {
            if (cards.Count != 5)
                throw new ArgumentException("Ein Kartenpaket muss genau 5 Karten enthalten!");

            Id = Guid.NewGuid();
            Cards = cards;
        }
    }
}

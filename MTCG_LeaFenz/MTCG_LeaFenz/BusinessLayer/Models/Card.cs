using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_LeaFenz.BusinessLayer.Models
{
    internal class Card
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Damage { get; set; }
        public string ElementType { get; set; } = "Normal";  // Fire, Water or Normal
        public string CardType { get; set; } = "Monster";  //Monster or Spell

        public Card() { }

        public Card(string name, int damage, string elementType, string cardType)
        {
            Id = Guid.NewGuid();  // Automatische UUID
            Name = name;
            Damage = damage;
            ElementType = elementType;
            CardType = cardType;
        }

        public override string ToString()
        {
            return $"{Name} [{CardType} - {ElementType}] mit {Damage} Schaden";
        }
    }
}

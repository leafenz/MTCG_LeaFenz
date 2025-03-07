using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_LeaFenz.BusinessLayer.Models
{
    internal class Trading
    {
            public Guid Id { get; set; }  // Eindeutige ID für das Tauschgeschäft
            public string Owner { get; set; } = string.Empty;  // Besitzer der angebotenen Karte
            public Guid OfferedCardId { get; set; }  // Die angebotene Karte
            public string RequiredType { get; set; } = "Monster";  // Erwarteter Kartentyp (Monster oder Spell)
            public int RequiredMinDamage { get; set; } = 0;  // Mindest-Schaden der gewünschten Karte
            public bool IsActive { get; set; } = true;  // Ob das Tauschgeschäft noch verfügbar ist

            public Trading() { }

            public Trading(string owner, Guid offeredCardId, string requiredType, int requiredMinDamage)
            {
                Id = Guid.NewGuid();
                Owner = owner;
                OfferedCardId = offeredCardId;
                RequiredType = requiredType;
                RequiredMinDamage = requiredMinDamage;
                IsActive = true;
            }

            public void CompleteTrade()
            {
                IsActive = false;
            }
}
}

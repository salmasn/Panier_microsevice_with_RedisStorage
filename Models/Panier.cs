//using System.Diagnostics;
using System.Xml;

namespace PanierService.Models
{
    public class Panier
    {

        //Identifiant unique du panier Créé automatiquement grâce à Guid(un identifiant unique global)
        //C’est une valeur générée automatiquement par.NET pour garantir que chaque ID est unique dans le monde entier.
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = "default-user"; // Pour l'instant
        public List<PanierItem> Items { get; set; } = new();
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime DerniereModification { get; set; } = DateTime.UtcNow;

        // Propriétés calculées (Ce ne sont PAS des valeurs stockées → elles sont calculées à la volée.)
        //La valeur n'est pas enregistrée dans la base de données
        public int NombreArticles => Items.Sum(i => i.Quantite);
        public decimal Total => Items.Sum(i => i.Total);
    }
}
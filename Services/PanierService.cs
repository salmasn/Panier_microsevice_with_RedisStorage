using PanierService.Models;
using PanierService.Models.DTOs;
using StackExchange.Redis;

namespace PanierService.Services
{
    public class PanierServiceImpl : IPanierService
    {
        private readonly IRedisService _redis;
        private readonly ILogger<PanierServiceImpl> _logger;
        private const int EXPIRATION_JOURS = 7;

        public PanierServiceImpl(IRedisService redis, ILogger<PanierServiceImpl> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<string> CreerNouveauPanierAsync()
        {
            var panier = new Panier();
            var key = $"panier:{panier.Id}";
            //Sauvegarder dans Redis avec expiration de 7 jours
            await _redis.SetAsync(key, panier, TimeSpan.FromDays(EXPIRATION_JOURS));
            _logger.LogInformation("Nouveau panier créé: {PanierId}", panier.Id);
            return panier.Id;
        }

        /// <summary>
        /// ✅ NOUVEAU: Garantit qu'un panier existe ou le crée automatiquement
        /// </summary>
        private async Task<Panier> ObtenirOuCreerPanierAsync(string panierId)
        {
            var key = $"panier:{panierId}";
            var panier = await _redis.GetAsync<Panier>(key);

            if (panier == null)
            {
                _logger.LogInformation("Panier {PanierId} inexistant, création automatique", panierId);

                // Créer un nouveau panier avec l'ID spécifié
                panier = new Panier
                {
                    Id = panierId, // ✅ Utiliser le panierId fourni au lieu d'un nouveau GUID
                    Items = new List<PanierItem>(),
                    DateCreation = DateTime.UtcNow,
                    DerniereModification = DateTime.UtcNow
                };

                await _redis.SetAsync(key, panier, TimeSpan.FromDays(EXPIRATION_JOURS));
                _logger.LogInformation("Panier {PanierId} créé automatiquement", panierId);
            }

            return panier;
        }

        public async Task<PanierResponseDto> ObtenirPanierAsync(string panierId)
        {
            var key = $"panier:{panierId}";
            var panier = await _redis.GetAsync<Panier>(key);

            if (panier == null)
            {
                _logger.LogWarning("Panier non trouvé: {PanierId}", panierId);
                throw new KeyNotFoundException($"Panier {panierId} introuvable");
            }

            return MapToDto(panier);
        }

        /// <summary>
        /// ✅ MODIFIÉ: Crée automatiquement le panier si nécessaire
        /// </summary>
        public async Task<PanierResponseDto> AjouterArticleAsync(string panierId, AjouterArticleDto dto)
        {
            try
            {
                // ✅ Utiliser ObtenirOuCreerPanierAsync pour recuperer ou creer panier s'il n'existe pas 
                var panier = await ObtenirOuCreerPanierAsync(panierId);
                
                //cherche le premier élément qui correspond a l'article cherché
                //Pour chaque item i de la liste, on vérifie si i.ArticleId correspond à dto.ArticleId
                var itemExistant = panier.Items.FirstOrDefault(i => i.ArticleId == dto.ArticleId);

                if (itemExistant != null)
                {
                    //incrementation
                    itemExistant.Quantite += dto.Quantite;
                }
                else
                {
                    panier.Items.Add(new PanierItem
                    {
                        ArticleId = dto.ArticleId,
                        Nom = dto.Nom,
                        Prix = dto.Prix,
                        Quantite = dto.Quantite
                    });
                }

                panier.DerniereModification = DateTime.UtcNow;
                var key = $"panier:{panierId}";
                await _redis.SetAsync(key, panier, TimeSpan.FromDays(EXPIRATION_JOURS));

                _logger.LogInformation("Article {ArticleId} ajouté au panier {PanierId}", dto.ArticleId, panierId);

                return MapToDto(panier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'article au panier {PanierId}", panierId);
                throw;
            }
        }

        /// <summary>
        /// ✅ MODIFIÉ: Crée automatiquement le panier si nécessaire
        /// </summary>
        public async Task<PanierResponseDto> ModifierQuantiteAsync(string panierId, ModifierQuantiteDto dto)
        {
            try
            {
                var panier = await ObtenirOuCreerPanierAsync(panierId);

                var item = panier.Items.FirstOrDefault(i => i.ArticleId == dto.ArticleId);
                //modif de quantité
                if (item != null)
                {
                    if (dto.NouvelleQuantite <= 0)
                    {
                        panier.Items.Remove(item);
                    }
                    else
                    {
                        item.Quantite = dto.NouvelleQuantite;
                    }

                    panier.DerniereModification = DateTime.UtcNow;
                    var key = $"panier:{panierId}";
                    await _redis.SetAsync(key, panier, TimeSpan.FromDays(EXPIRATION_JOURS));
                }

                return MapToDto(panier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de quantité dans le panier {PanierId}", panierId);
                throw;
            }
        }

        public async Task<bool> SupprimerArticleAsync(string panierId, int articleId)
        {
            var key = $"panier:{panierId}";
            var panier = await _redis.GetAsync<Panier>(key);

            if (panier == null)
            {
                return false;
            }

            var item = panier.Items.FirstOrDefault(i => i.ArticleId == articleId);

            if (item != null)
            {
                panier.Items.Remove(item);
                panier.DerniereModification = DateTime.UtcNow;
                await _redis.SetAsync(key, panier, TimeSpan.FromDays(EXPIRATION_JOURS));
                return true;
            }

            return false;
        }


        public async Task<bool> ViderPanierAsync(string panierId)
        {
            var key = $"panier:{panierId}";

            var panier = await _redis.GetAsync<Panier>(key);
            if (panier == null)
                return false; // Panier inexistant

            panier.Items.Clear(); // Vide la liste d'articles
            panier.DerniereModification = DateTime.UtcNow;

            await _redis.SetAsync(key, panier, TimeSpan.FromDays(7));
            return true;
        }

        //public async Task<bool> ViderPanierAsync(string panierId)
        //{
        //    var key = $"panier:{panierId}";
        //    return await _redis.DeleteAsync(key);
        //}


        private PanierResponseDto MapToDto(Panier panier)
        {
            return new PanierResponseDto
            {
                PanierId = panier.Id,
                Items = panier.Items,
                NombreArticles = panier.NombreArticles,
                Total = panier.Total,
                //DateCreation = panier.DateCreation,
                DerniereModification = panier.DerniereModification
            };
        }
    }
}
using PanierService.Models;
using PanierService.Models.DTOs;

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
            await _redis.SetAsync(key, panier, TimeSpan.FromDays(EXPIRATION_JOURS));
            _logger.LogInformation("Nouveau panier créé: {PanierId}", panier.Id);
            return panier.Id;
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

        public async Task<PanierResponseDto> AjouterArticleAsync(string panierId, AjouterArticleDto dto)
        {
            var key = $"panier:{panierId}";
            var panier = await _redis.GetAsync<Panier>(key);

            if (panier == null)
            {
                throw new KeyNotFoundException($"Panier {panierId} introuvable");
            }

            var itemExistant = panier.Items.FirstOrDefault(i => i.ArticleId == dto.ArticleId);

            if (itemExistant != null)
            {
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
            await _redis.SetAsync(key, panier, TimeSpan.FromDays(EXPIRATION_JOURS));

            _logger.LogInformation("Article {ArticleId} ajouté au panier {PanierId}", dto.ArticleId, panierId);

            return MapToDto(panier);
        }

        public async Task<PanierResponseDto> ModifierQuantiteAsync(string panierId, ModifierQuantiteDto dto)
        {
            var key = $"panier:{panierId}";
            var panier = await _redis.GetAsync<Panier>(key);

            if (panier == null)
            {
                throw new KeyNotFoundException($"Panier {panierId} introuvable");
            }

            var item = panier.Items.FirstOrDefault(i => i.ArticleId == dto.ArticleId);

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
                await _redis.SetAsync(key, panier, TimeSpan.FromDays(EXPIRATION_JOURS));
            }

            return MapToDto(panier);
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
            return await _redis.DeleteAsync(key);
        }

        private PanierResponseDto MapToDto(Panier panier)
        {
            return new PanierResponseDto
            {
                PanierId = panier.Id,
                Items = panier.Items,
                NombreArticles = panier.NombreArticles,
                Total = panier.Total,
                DerniereModification = panier.DerniereModification
            };
        }
    }
}
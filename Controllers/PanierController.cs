using Microsoft.AspNetCore.Mvc;
using PanierService.Models.DTOs;
using PanierService.Services;

namespace PanierService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PanierController : ControllerBase
    {
        private readonly IPanierService _panierService;
        private readonly ILogger<PanierController> _logger;

        public PanierController(IPanierService panierService, ILogger<PanierController> logger)
        {
            _panierService = panierService;
            _logger = logger;
        }

        // POST: api/panier/creer
        [HttpPost("creer")]
        public async Task<ActionResult<string>> CreerPanier()
        {
            try
            {
                var panierId = await _panierService.CreerNouveauPanierAsync();
                return Ok(new { panierId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur création panier");
                return StatusCode(500, "Erreur lors de la création du panier");
            }
        }

        // GET: api/panier/{panierId}
        [HttpGet("{panierId}")]
        public async Task<ActionResult<PanierResponseDto>> ObtenirPanier(string panierId)
        {
            try
            {
                var panier = await _panierService.ObtenirPanierAsync(panierId);
                return Ok(panier);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Panier {panierId} introuvable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur récupération panier {PanierId}", panierId);
                return StatusCode(500, "Erreur serveur");
            }
        }
        //Appelle le service pour ajouter un article ou augmenter la quantité s’il existe déjà
        // POST: api/panier/{panierId}/ajouter
        [HttpPost("{panierId}/ajouter")]
        public async Task<ActionResult<PanierResponseDto>> AjouterArticle(
            string panierId,
            [FromBody] AjouterArticleDto dto)
        {
            try
            {
                var panier = await _panierService.AjouterArticleAsync(panierId, dto);
                return Ok(panier);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Panier {panierId} introuvable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur ajout article");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // PUT: api/panier/{panierId}/quantite
        [HttpPut("{panierId}/quantite")]
        public async Task<ActionResult<PanierResponseDto>> ModifierQuantite(
            string panierId,
            [FromBody] ModifierQuantiteDto dto)
        {
            try
            {
                var panier = await _panierService.ModifierQuantiteAsync(panierId, dto);
                return Ok(panier);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Panier {panierId} introuvable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur modification quantité");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // DELETE: api/panier/{panierId}/article/{articleId}
        [HttpDelete("{panierId}/article/{articleId}")]
        public async Task<ActionResult> SupprimerArticle(string panierId, int articleId)
        {
            try
            {
                var resultat = await _panierService.SupprimerArticleAsync(panierId, articleId);

                if (resultat)
                    return Ok(new { message = "Article supprimé" });

                return NotFound("Article non trouvé dans le panier");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur suppression article");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // DELETE: api/panier/{panierId}
        [HttpDelete("{panierId}")]
        public async Task<ActionResult> ViderPanier(string panierId)
        {
            try
            {
                var resultat = await _panierService.ViderPanierAsync(panierId);

                if (resultat)
                    return Ok(new { message = "Panier vidé" });

                return NotFound($"Panier {panierId} introuvable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur vidage panier");
                return StatusCode(500, "Erreur serveur");
            }
        }
    }
}
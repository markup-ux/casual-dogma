using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class CraftRecipeGetCraftRecipeHandler : GameRequestPacketHandler<C2SCraftRecipeGetCraftRecipeReq, S2CCraftRecipeGetCraftRecipeRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(CraftRecipeGetCraftRecipeHandler));

        public CraftRecipeGetCraftRecipeHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CCraftRecipeGetCraftRecipeRes Handle(GameClient client, C2SCraftRecipeGetCraftRecipeReq request)
        {
            var allRecipesInCategory = Server.AssetRepository.CraftingRecipesAsset
                .Where(recipes => recipes.Category == request.Category)
                .Select(recipes => recipes.RecipeList)
                .SingleOrDefault(new List<CraftingRecipe>())
                .Where(recipe => recipe.UnlockID == 0 || client.Character.UnlockableItems.Contains((UnlockableItemCategory.CraftingRecipe, recipe.UnlockID)));

            bool goldFree = Server.GameSettings.GameServerSettings.MakeGoldFree;

            return new S2CCraftRecipeGetCraftRecipeRes
            {
                Category = request.Category,
                RecipeList = allRecipesInCategory
                    .SkipWhile(recipe => recipe.IsHide)
                    .Skip((int)request.Offset)
                    .Take(request.Num)
                    .Select(x =>
                    {
                        var recipe = x.AsCData();
                        if (goldFree)
                        {
                            recipe.Cost = 0;
                        }
                        return recipe;
                    })
                    .ToList(),
                IsEnd = (request.Offset + request.Num) >= allRecipesInCategory.Count()
            };
        }
    }
}

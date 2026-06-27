using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class CraftRecipeGetGradeupRecipeHandler : GameRequestPacketHandler<C2SCraftRecipeGetCraftGradeupRecipeReq, S2CCraftRecipeGetCraftGradeupRecipeRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(CraftRecipeGetGradeupRecipeHandler));

        public CraftRecipeGetGradeupRecipeHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CCraftRecipeGetCraftGradeupRecipeRes Handle(GameClient client, C2SCraftRecipeGetCraftGradeupRecipeReq request)
        {
            List<CDataMDataCraftGradeupRecipe> categoryRecipes = Server.AssetRepository.CraftingGradeUpRecipesAsset
                .Where(recipes => recipes.Category == request.Category)
                .SelectMany(recipes => recipes.RecipeList)
                .ToList();

            List<CDataCommonU32> itemList = request.ItemList;

            var pagedRecipes = categoryRecipes.Skip((int)request.Offset).Take(request.Num).ToList();

            // Gold has been made useless: show equipment grade-up as free.
            if (Server.GameSettings.GameServerSettings.MakeGoldFree)
            {
                pagedRecipes = pagedRecipes.Select(r => new CDataMDataCraftGradeupRecipe()
                {
                    RecipeID = r.RecipeID,
                    ItemID = r.ItemID,
                    Upgradable = r.Upgradable,
                    GradeupItemID = r.GradeupItemID,
                    Cost = 0,
                    Exp = r.Exp,
                    AllowMultiGrade = r.AllowMultiGrade,
                    CraftMaterialList = r.CraftMaterialList
                }).ToList();
            }

            var response = new S2CCraftRecipeGetCraftGradeupRecipeRes()
            {
                Category = request.Category, 
                RecipeList = pagedRecipes,
                UpgradableItemList = itemList,  
                IsEnd = (request.Offset + request.Num) >= categoryRecipes.Count
            };
            return response;
        }
    }
}

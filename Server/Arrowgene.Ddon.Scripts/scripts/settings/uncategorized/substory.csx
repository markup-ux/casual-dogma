/**
 * Settings file for the Substory feature in substory.csx
 * 
 * There are 3 SubstoryGroupIds 1, 2, 3
 * 1 - Carrie (NpcId 580)
 *   Sequence Nums: 0, 3, 4, 5, 7, 8, 9, 10
 * 2 - Sonel (NpcId 582)
 *   Sequence Nums: 0, 11, 12, 13, 14, 15, 16, 17
 * 3 - Mephis (NpcId 680)
 *   Sequence Nums: 0, 18, 19, 20, 21, 22, 23, 24
 *   
 * How to unlock Substory
 * 1. Complete: The Prince's Whereabouts,
 * 2. Complete: Save the Royal Family Cook
 *   - Unlock Contents Release for Royal Family,
 *   - Unlock Carrie for substory via Substory menu and quests.
 */

/// <summary>
/// The helper class in the server uses this variable to detect hot reload and recompute some lookup tables.
/// </summary>
var RecomputeSubstoryLookups = true;

/// <summary>
/// Determines what unlocks the Substory quest lines.
/// </summary>
var SubstoryGroupIdUnlockReqs = new Dictionary<QuestSubstoryGroupId, QuestId>
{
    [QuestSubstoryGroupId.Carrie] = QuestId.SaveTheRoyalFamilyCook,
    [QuestSubstoryGroupId.Sonel] = QuestId.RescueTheGeniusCommander,
    [QuestSubstoryGroupId.Mephis] = QuestId.ExposeTheDarkSideToTheOneOperatingBehindTheScenes,
};

/// <summary>
/// Describes the order of the sequence numbers.
/// </summary>
var SubstorySequence = new Dictionary<QuestSubstoryGroupId, List<uint>>
{
    [QuestSubstoryGroupId.Carrie] = [7, 8, 3, 4, 5, 9, 10],
    [QuestSubstoryGroupId.Sonel] = [],
    [QuestSubstoryGroupId.Mephis] = [],
};

/// <summary>
/// Describes the Substory Groups and Dependencies
/// </summary>
var SubstoryMissionMap = new Dictionary<QuestSubstoryGroupId, Dictionary<uint, List<QuestId>>>
{
    [QuestSubstoryGroupId.Carrie] = new() {
        [3] = [ // Piremoth Food Transport
            QuestId.SweetFoodForPiremoth,
            QuestId.HungryGoblin,
            QuestId.FoodForPiremoth,
            QuestId.ToTheEndOfThePursuit,
        ],
        [4] = [ // Bandit Group Hideout Food Transport
            QuestId.SweetFoodForTheBanditGroupHideout,
            QuestId.BewareTheWaterside,
            QuestId.FoodForTheBanditGroupHideout,
            QuestId.ChasingAfterThePhantomIngredient,
        ],
        [5] = [ // Chasing the "Phantom" Foodstuff
            QuestId.FarewellHungry
        ],
        [7] = [ //  Fort Thines Food Procurement
            QuestId.CarrieTheCookDespairs,
            QuestId.TasteOfBitterMemories,
            QuestId.FortThinesFoodProcurement,
            QuestId.PathToFoodProcurement,
            QuestId.CarriesWorry,
        ],
        [8] = [ // Large Dining Hall Restoration
            QuestId.FirstStepToRestoringTheGreatDiningHallKitchen,
            QuestId.SecondStepToRestoringTheGreatDiningHallCookStove,
            QuestId.ThirdStepToRestoringTheGreatDiningHallWaterWell,
            QuestId.FourthStepToRestoringTheGreatDiningHallWineBarrel,
            QuestId.SignsOfRestoringTheGreatDiningHall,
        ],
        [9] = [ // Complete Restoration! Fort Thines Great Dining Hall
            QuestId.FortThinesGreatDiningHall
        ],
        [10] = [ //  Deepen Friendship with Cooperators
            QuestId.ProcurementCooperationBrad,
            QuestId.ProcurementCooperationLorna,
            QuestId.ProcurementCooperationHeather,
            QuestId.ProcurementCooperationSneaker,
            QuestId.ProcurementCooperationAidan,
            QuestId.RequestForGoblinFamilyCooperationCarrie,
        ]
    },
    [QuestSubstoryGroupId.Sonel] = new() { },
    [QuestSubstoryGroupId.Mephis] = new() { }
};

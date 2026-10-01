using HarmonyLib;
using UnityEngine;

namespace BetterRaft 
{
    [HarmonyPatch(typeof(Bobber))]
    public static class Bobber_Patches
    {
        private const float InitialSlowdownFactor = 2.0f; 

        [HarmonyPatch("FishOnHook", typeof(float))]
        [HarmonyPrefix]
        public static void FishOnHook_Prefix(Bobber __instance, ref float timeDelay)
        {
            if (__instance == null) return;
            float skillSpeedModifier = FishingRodProgression.GetCurrentFishingSpeedModifier();
            float newDelay = (timeDelay * InitialSlowdownFactor) / skillSpeedModifier;
            if (newDelay < 0.1f) newDelay = 0.1f;
            
            timeDelay = newDelay;
        }
    }

    [HarmonyPatch(typeof(FishingRod))]
    public static class FishingRod_Patches
    {
       
        [HarmonyPatch("PullItemsFromSea")]
        [HarmonyPrefix]
        public static void PullItemsFromSea_Prefix(FishingRod __instance, out object __state) 
        {
           
            int extraFishCaughtByMod = 0;
            __state = extraFishCaughtByMod; 

            Network_Player playerNetwork = Traverse.Create(__instance).Field("playerNetwork").GetValue<Network_Player>();
            FishingBaitHandler fishingBaitHandler = Traverse.Create(__instance).Field("fishingBaitHandler").GetValue<FishingBaitHandler>();
            
            if (__instance == null || playerNetwork == null || fishingBaitHandler == null || fishingBaitHandler.CurrentBait == null)
            {
                return; 
            }

            bool isMetalRod = false; 
            Traverse isMetalRodTraverse = Traverse.Create(__instance).Field("isMetalRod");
            if (isMetalRodTraverse.FieldExists())
            {
                isMetalRod = isMetalRodTraverse.GetValue<bool?>() ?? false;
            }

            float extraCatchChance = FishingRodProgression.GetCurrentExtraCatchChance();
            if (Random.Range(0f, 100f) < extraCatchChance)
            {
                int numberOfExtraFishToAdd = 1; 

                for (int i = 0; i < numberOfExtraFishToAdd; i++)
                {
                    if (fishingBaitHandler.CurrentBait == null) break; 

                    Item_Base extraFishItem = fishingBaitHandler.GetRandomItemFromCurrentBaitPool(isMetalRod); 
                    if (extraFishItem != null)
                    {
                        playerNetwork.Inventory.AddItem(extraFishItem.UniqueName, 1);
                        fishingBaitHandler.ConsumeBait(); 
                        extraFishCaughtByMod++; 
                      
                    }
                    else 
                    {
                        break; 
                    }
                }
            }
            __state = extraFishCaughtByMod; 
        }

        [HarmonyPatch("PullItemsFromSea")]
        [HarmonyPostfix]
        public static void PullItemsFromSea_Postfix(FishingRod __instance, object __state) 
        {
            int extraFishCaughtByMod = 0;
            if (__state != null && __state is int) 
            {
                extraFishCaughtByMod = (int)__state;
            }

            Bobber bobber = Traverse.Create(__instance).Field("bobber").GetValue<Bobber>();
            if (__instance == null || bobber == null) return;

            int totalFishThisCast = extraFishCaughtByMod;

            if (!bobber.FishIsOnHook) 
            {
                totalFishThisCast++; 
            }

            if (totalFishThisCast > 0)
            {
                FishingRodProgression.RecordFishCaught(totalFishThisCast);
               
            }
        }
    }
}

using UnityEngine;

namespace BetterRaft
{
    public static class FishingRodProgression
    {
        public static int FishingLevel { get; private set; } = 0;
        public static int FishingExperience { get; private set; } = 0;


        public const int MaxFishingLevel = 20;
        public const int BaseExperienceToLevelUp = 5;
        public const int ExperienceIncPerLevel = 3;


        public const float BaseFishingSpeedBonusPerLevel = 0.1f;
        public const float BaseExtraCatchChancePerLevel = 1.5f;
        public const float MaxExtraCatchChance = 50.0f;

        public static void Initialize()
        {
            
        }

        public static void ApplyData(FishingRodProgressionData data)
        {
            if (data != null)
            {
                FishingLevel = Mathf.Clamp(data.FishingLevel, 0, MaxFishingLevel);
                FishingExperience = data.FishingExperience;
                if (FishingLevel < MaxFishingLevel) {
                    int expForNext = GetExperienceNeededForLevel(FishingLevel);
                    if (FishingExperience >= expForNext) {
                        RecordFishCaught(0);
                    }
                } else {
                    FishingExperience = 0;
                }
               
            }
            else
            {
                FishingLevel = 0;
                FishingExperience = 0;
               
            }
        }

        public static FishingRodProgressionData GetData()
        {
            return new FishingRodProgressionData
            {
                FishingLevel = FishingLevel,
                FishingExperience = FishingExperience
            };
        }

        public static int GetExperienceNeededForLevel(int currentLevel)
        {
            if (currentLevel >= MaxFishingLevel) return int.MaxValue;
            return BaseExperienceToLevelUp + (currentLevel * ExperienceIncPerLevel);
        }

        public static void RecordFishCaught(int fishCount = 1)
        {
            if (FishingLevel >= MaxFishingLevel)
            {
                FishingExperience = 0;
                return;
            }

            FishingExperience += fishCount;


            while (FishingLevel < MaxFishingLevel && FishingExperience >= GetExperienceNeededForLevel(FishingLevel))
            {
                LevelUp();
            }
        }

        private static void LevelUp()
        {
            int expNeeded = GetExperienceNeededForLevel(FishingLevel);
            FishingLevel++;
            FishingExperience -= expNeeded;
            if (FishingExperience < 0) FishingExperience = 0;

           

            HNotify notifier = Object.FindObjectOfType<HNotify>();
            if (notifier != null)
            {
                notifier.AddNotification(HNotify.NotificationType.normal, $"Fishing skill increased! Level: {FishingLevel}", 4, HNotify.CheckSprite);
            }

            BetterRaft_Main.TrySaveCombinedModProgression(); 

            if (FishingLevel >= MaxFishingLevel)
            {
                FishingExperience = 0;
                if (notifier != null) {
                    notifier.AddNotification(HNotify.NotificationType.normal, "Fishing skill reached maximum!", 5, HNotify.CheckSprite);
                }
            }
        }

        public static float GetCurrentFishingSpeedModifier()
        {
            float bonus = FishingLevel * BaseFishingSpeedBonusPerLevel;
            return 1.0f + bonus;
        }

        public static float GetCurrentExtraCatchChance()
        {
            float chance = FishingLevel * BaseExtraCatchChancePerLevel;
            return Mathf.Clamp(chance, 0f, MaxExtraCatchChance);
        }

        public static void ResetToDefaults() {
            FishingLevel = 0;
            FishingExperience = 0;
           
            BetterRaft_Main.TrySaveCombinedModProgression(); 
        }
    }
}
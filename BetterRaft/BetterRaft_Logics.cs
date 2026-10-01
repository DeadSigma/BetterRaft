using UnityEngine;
using UltimateWater;

namespace BetterRaft
{
    public class Stat_Fatigue : Stat
    {
        private PersonController personController;
        private PlayerStats playerStats;
        private bool LowFatigueNotified = false;
        public bool IsExhaustedAndSinking { get; private set; }

        private float regenCooldownTimer = 0f;

        public void Initialize(PersonController pc, PlayerStats pStats)
        {
            this.personController = pc;
            this.playerStats = pStats;
            this.SetMaxValue(FatigueProgression.GetCurrentMaxFatigue());
            this.Value = this.Max;
            this.regenCooldownTimer = 0f;
        }

        public void ManagedUpdate(float deltaTime)
        {
            if (personController == null ||
                playerStats == null ||
                !BetterRaft_Main.IsLocalPlayerStats(playerStats) ||
                playerStats.IsDead)
            {
                IsExhaustedAndSinking = false;
                return;
            }

            ControllerType currentCtrlType = personController.controllerType;
            float currentLossRate = FatigueProgression.GetCurrentFatigueLossRate();
            float currentRegenRate = FatigueProgression.GetCurrentFatigueRegenOnLandRate();
            bool isPlayerActuallyInWater = (currentCtrlType == ControllerType.Water);

            if (isPlayerActuallyInWater)
            {
                if (this.Value > 0)
                {
                    this.Value -= currentLossRate * deltaTime;
                    if (this.Value < 0) this.Value = 0;
                }
                regenCooldownTimer = FatigueProgression.FatigueRegenCooldownDuration;
            }
            else
            {
                if (regenCooldownTimer > 0)
                {
                    regenCooldownTimer -= deltaTime;
                }
                if (regenCooldownTimer <= 0 && this.Value < this.Max)
                {
                    this.Value += currentRegenRate * deltaTime;
                    if (this.Value > this.Max) this.Value = this.Max;
                }
            }

            if (this.Value <= 0 && isPlayerActuallyInWater)
            {
                IsExhaustedAndSinking = true;
                HNotify notifier = Object.FindObjectOfType<HNotify>();
                if (notifier != null && !LowFatigueNotified)
                {
                    notifier.AddNotification(HNotify.NotificationType.normal, "Complete exhaustion! You are sinking!", 4, HNotify.ErrorSprite);
                    LowFatigueNotified = true;
                }
            }
            else
            {
                if (this.Value > 0 || !isPlayerActuallyInWater) IsExhaustedAndSinking = false;
                if (this.Value > 0) LowFatigueNotified = false;
            }
        }
    }

    public static class LungProgression
    {
        public static int DivesPerLevelUp { get; set; } = 10;
        public static float OxygenRegenerationModifier { get; set; } = 1f;
        public static int LungLevel { get; private set; } = 0;
        private static int lungProgressExp = 0;
        private static float baseMaxOxygen = 200;
        private static float oxygenIncreasePerLevel = 15f;
        private static int maxLungLevel = 20;
        private static float initialOxygenLossRate = 30f;
        private static float oxygenLossRateReductionPerLevel = 0.5f;
        private static float minOxygenLossRate = 10f;

        private static bool awaitingRegenCompletion = false;
        private static float diveMinOxygenRatio = 1f;

        public static void ApplyData(LungProgressionData data)
        {
            if (data != null)
            {
                LungLevel = data.LungLevel;
                lungProgressExp = data.SignificantDivesCounter % DivesPerLevelUp;
            }
            else
            {
                LungLevel = 0;
                lungProgressExp = 0;
            }
            awaitingRegenCompletion = false;
            diveMinOxygenRatio = 1f;
        }
        public static LungProgressionData GetData() { return new LungProgressionData { LungLevel = LungLevel, SignificantDivesCounter = (LungLevel * DivesPerLevelUp) + lungProgressExp }; }
        public static float GetCurrentMaxOxygen() { return baseMaxOxygen + (LungLevel * oxygenIncreasePerLevel); }
        public static float GetCurrentOxygenLossRate()
        {
            float rate = initialOxygenLossRate - (LungLevel * oxygenLossRateReductionPerLevel);
            return Mathf.Max(rate, minOxygenLossRate);
        }
        public static void RecordDiveActivity(Stat_Oxygen oxygenStat, PersonController personController)
        {
            if (personController == null ||
                oxygenStat == null ||
                !BetterRaft_Main.IsLocalPersonController(personController) ||
                !BetterRaft_Main.IsLocalOxygenStat(oxygenStat)) return;

            UltimateWater.SubmersionState currentSubmersion = personController.SubmersionState;
            bool isUnderwater = (currentSubmersion == UltimateWater.SubmersionState.Full);
            float currentRatio = oxygenStat.Value / oxygenStat.Max;

            if (isUnderwater && oxygenStat.Value < oxygenStat.Max)
            {  // Кислород тратится под водой
                if (!awaitingRegenCompletion)
                {
                    awaitingRegenCompletion = true;
                    diveMinOxygenRatio = currentRatio;
                }
                else
                {
                    diveMinOxygenRatio = Mathf.Min(diveMinOxygenRatio, currentRatio);
                }
            }
            else
            {  // Кислород больше не тратится
                if (awaitingRegenCompletion)
                {
                    if (currentRatio >= 0.8f)
                    {  // Опыт выдаётся после восстановления до 80%
                        int expGain = 0;
                        if (diveMinOxygenRatio <= 0.1f) expGain = 2;  // Потрачено не меньше 90%
                        else if (diveMinOxygenRatio <= 0.5f) expGain = 1;  // Потрачено не меньше 50%

                        if (expGain > 0)
                        {
                            lungProgressExp += expGain;
                            int levelsGained = 0;
                            int expPerLevel = DivesPerLevelUp;
                            while (lungProgressExp >= expPerLevel)
                            {
                                lungProgressExp -= expPerLevel;
                                if (LungLevel < maxLungLevel)
                                {
                                    LungLevel++;
                                    levelsGained++;
                                }
                            }
                            if (levelsGained > 0)
                            {
                                Debug.Log($"Oxygen level increased by {levelsGained}! New level: {LungLevel}.");
                                HNotify notifier = Object.FindObjectOfType<HNotify>();
                                if (notifier != null)
                                {
                                    notifier.AddNotification(HNotify.NotificationType.normal, $"Oxygen level +{levelsGained}! New level: {LungLevel}", 3, HNotify.CheckSprite);
                                }
                                BetterRaft_Main.TrySaveCombinedModProgression();
                            }
                        }
                        awaitingRegenCompletion = false;
                        diveMinOxygenRatio = 1f;
                    }
                }
            }
        }
        public static void ResetToDefaults()
        {
            LungLevel = 0; lungProgressExp = 0;
            awaitingRegenCompletion = false; diveMinOxygenRatio = 1f;
        }

        public static int GetCurrentDivesProgress()
        {
            return lungProgressExp;
        }
    }

    public static class FatigueProgression
    {
        public static int TimeInWaterPerLevelUp { get; set; } = 150;
        public static float FatigueRegenOnLandModifier { get; set; } = 1f;
        public static float FatigueRegenCooldownDuration { get; set; } = 2f;
        public static int FatigueLevel { get; private set; } = 0;
        private static float timeSpentInWaterActiveCounter = 0f;
        private static float baseMaxFatigue = 120f;
        private static float fatigueIncreasePerLevel = 20f;
        private static int maxFatigueLevel = 20;
        private static float initialFatigueLossRate = 5f;
        private static float fatigueLossRateReductionPerLevel = 0.2f;
        private static float minFatigueLossRate = 1f;
        private static float baseFatigueRegenOnLand = 10f;
        private static float fatigueRegenIncreasePerLevel = 1f;

        public static void UpdateAllModSettingsFromAPI()
        {
            if (!BetterRaft_Main.ExtraSettingsAPI_Loaded)
            {
                TimeInWaterPerLevelUp = 150;
                FatigueRegenOnLandModifier = 1.0f;
                FatigueRegenCooldownDuration = 2f;
            }
        }
        public static void ApplyData(FatigueProgressionData data)
        {
            if (data != null)
            {
                FatigueLevel = data.FatigueLevel;
                timeSpentInWaterActiveCounter = data.FatigueActivityCounter;

            }
            else
            {
                FatigueLevel = 0;
                timeSpentInWaterActiveCounter = 0f;

            }
        }
        public static FatigueProgressionData GetData()
        {
            return new FatigueProgressionData { FatigueLevel = FatigueLevel, FatigueActivityCounter = Mathf.FloorToInt(timeSpentInWaterActiveCounter) };
        }
        public static float GetCurrentMaxFatigue() { return baseMaxFatigue + (FatigueLevel * fatigueIncreasePerLevel); }
        public static float GetCurrentFatigueLossRate()
        {
            float rate = initialFatigueLossRate - (FatigueLevel * fatigueLossRateReductionPerLevel);
            return Mathf.Max(rate, minFatigueLossRate);
        }
        public static float GetCurrentFatigueRegenOnLandRate()
        {
            return (baseFatigueRegenOnLand + (FatigueLevel * fatigueRegenIncreasePerLevel)) * FatigueRegenOnLandModifier;
        }
        public static void RecordFatigueActivity(float deltaTime, Stat_Fatigue fatigueStat, PersonController pc)
        {
            if (pc == null ||
                fatigueStat == null ||
                !BetterRaft_Main.IsLocalPersonController(pc)) return;
            ControllerType currentCtrlType = pc.controllerType;
            bool isInWaterForTraining = (currentCtrlType == ControllerType.Water);

            if (isInWaterForTraining && fatigueStat.Value < fatigueStat.Max)
            {
                timeSpentInWaterActiveCounter += deltaTime;
                if (timeSpentInWaterActiveCounter >= TimeInWaterPerLevelUp)
                {
                    if (FatigueLevel < maxFatigueLevel)
                    {
                        FatigueLevel++;
                        timeSpentInWaterActiveCounter = 0f;

                        Debug.Log($"Stamina level increased! New level: {FatigueLevel}.");
                        Stat_Fatigue currentFatigueStat = pc.GetComponent<Stat_Fatigue>();
                        if (currentFatigueStat != null) currentFatigueStat.SetMaxValue(GetCurrentMaxFatigue());
                        HNotify notifier = Object.FindObjectOfType<HNotify>();

                        if (notifier != null) notifier.AddNotification(HNotify.NotificationType.normal, $"Stamina level increased! New: {FatigueLevel}", 4, HNotify.CheckSprite);
                        BetterRaft_Main.TrySaveCombinedModProgression();
                    }
                }
            }
        }
        public static void ResetToDefaults()
        {
            FatigueLevel = 0; timeSpentInWaterActiveCounter = 0f;
        }

        public static int GetCurrentTimeProgressSeconds()
        {
            return Mathf.FloorToInt(timeSpentInWaterActiveCounter);
        }

        private static string FormatSeconds(int seconds)
        {
            int mins = seconds / 60;
            int secs = seconds % 60;
            return $"{mins}:{secs:00}";
        }

        public static string GetFormattedProgressTime()
        {
            int current = GetCurrentTimeProgressSeconds();
            int next = TimeInWaterPerLevelUp;
            return $"{FormatSeconds(current)} / {FormatSeconds(next)}";
        }
    }
}
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UltimateWater;
using System.IO;
using HMLLibrary;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterRaft
{
    public class BetterRaft_Main : Mod
    {
        Harmony harmonyInstance;
        public const string HARMONY_ID = "com.DeadSigma.raft.BetterRaft";
        public static string CurrentWorldIdentifier { get; private set; }
        public static AssetBundle fatigueDisplayBundle;
        public static GameObject fatigueDisplayCanvasInstance;
        public static Image fatigueBarImage;
        public static BetterRaft_Main Instance;
        public static bool ExtraSettingsAPI_Loaded = false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float ExtraSettingsAPI_GetSliderValue(string SettingName) => 0f;

        public void ExtraSettingsAPI_Load()
        {
            ExtraSettingsAPI_Loaded = true;
            UpdateAllModSettingsFromAPI();
        }

        public void ExtraSettingsAPI_SettingsClose()
        {
            UpdateAllModSettingsFromAPI();
        }

        public static void UpdateAllModSettingsFromAPI()
        {
            if (!ExtraSettingsAPI_Loaded)
            {
                LungProgression.DivesPerLevelUp = 10;
                LungProgression.OxygenRegenerationModifier = 0.5f;
                FatigueProgression.TimeInWaterPerLevelUp = 300;
                FatigueProgression.FatigueRegenOnLandModifier = 1.0f;
                FatigueProgression.FatigueRegenCooldownDuration = 3f;
                return;
            }
            LungProgression.DivesPerLevelUp = Mathf.RoundToInt(ExtraSettingsAPI_GetSliderValue("divesPerLevelSetting"));
            if (LungProgression.DivesPerLevelUp < 1) LungProgression.DivesPerLevelUp = 1;
            LungProgression.OxygenRegenerationModifier = ExtraSettingsAPI_GetSliderValue("oxygenRegenRateModifier");
            if (LungProgression.OxygenRegenerationModifier < 0.01f) LungProgression.OxygenRegenerationModifier = 0.01f;
            if (LungProgression.OxygenRegenerationModifier > 1.0f) LungProgression.OxygenRegenerationModifier = 1.0f;
            FatigueProgression.TimeInWaterPerLevelUp = Mathf.RoundToInt(ExtraSettingsAPI_GetSliderValue("fatigueTimePerLevelSetting"));
            if (FatigueProgression.TimeInWaterPerLevelUp < 60) FatigueProgression.TimeInWaterPerLevelUp = 60;
            FatigueProgression.FatigueRegenOnLandModifier = ExtraSettingsAPI_GetSliderValue("fatigueRegenRateModifier");
            if (FatigueProgression.FatigueRegenOnLandModifier < 0.1f) FatigueProgression.FatigueRegenOnLandModifier = 0.1f;
            if (FatigueProgression.FatigueRegenOnLandModifier > 2.0f) FatigueProgression.FatigueRegenOnLandModifier = 2.0f;
            FatigueProgression.FatigueRegenCooldownDuration = ExtraSettingsAPI_GetSliderValue("fatigueRegenCooldownSetting");
            if (FatigueProgression.FatigueRegenCooldownDuration < 0f) FatigueProgression.FatigueRegenCooldownDuration = 0f;
            if (FatigueProgression.FatigueRegenCooldownDuration > 10f) FatigueProgression.FatigueRegenCooldownDuration = 10f;
        }


        public static Network_Player GetLocalPlayerSafe()
        {
            try
            {
                return RAPI.GetLocalPlayer();
            }
            catch
            {
                return null;
            }
        }

        public static bool IsLocalPlayerStats(PlayerStats stats)
        {
            if (stats == null) return false;

            Network_Player localPlayer = GetLocalPlayerSafe();
            return localPlayer != null && localPlayer.Stats != null && localPlayer.Stats == stats;
        }

        public static bool IsLocalPersonController(PersonController personController)
        {
            if (personController == null) return false;

            Network_Player localPlayer = GetLocalPlayerSafe();
            if (localPlayer == null || localPlayer.Stats == null) return false;

            PersonController localController = localPlayer.Stats.GetComponent<PersonController>();
            return localController != null && localController == personController;
        }

        public static bool IsLocalOxygenStat(Stat_Oxygen oxygenStat)
        {
            if (oxygenStat == null) return false;

            Network_Player localPlayer = GetLocalPlayerSafe();
            return localPlayer != null &&
                   localPlayer.Stats != null &&
                   localPlayer.Stats.stat_oxygen != null &&
                   localPlayer.Stats.stat_oxygen == oxygenStat;
        }

        public static Stat_Fatigue EnsureLocalFatigueStat(PlayerStats stats)
        {
            if (!IsLocalPlayerStats(stats)) return null;

            Stat_Fatigue fatigueStat = stats.gameObject.GetComponent<Stat_Fatigue>();
            if (fatigueStat != null) return fatigueStat;

            try
            {
                fatigueStat = stats.gameObject.AddComponent<Stat_Fatigue>();
                PersonController personController = stats.GetComponent<PersonController>();

                if (fatigueStat != null && personController != null)
                {
                    fatigueStat.Initialize(personController, stats);
                }
            }
            catch
            {
                return null;
            }

            return fatigueStat;
        }

        public void Awake() { Instance = this; }

        public void Start()
        {
            harmonyInstance = new Harmony(HARMONY_ID);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

        }

        public override void WorldEvent_WorldLoaded()
        {
            base.WorldEvent_WorldLoaded();

            UpdateAllModSettingsFromAPI();
            UpdateCurrentWorldIdentifierInternal();

            if (!string.IsNullOrEmpty(CurrentWorldIdentifier))
            {

                PlayerModSaveData loadedData = ModDataManager.LoadCombinedProgression(CurrentWorldIdentifier);

                LungProgression.ApplyData(loadedData.LungData);
                FatigueProgression.ApplyData(loadedData.FatigueData);
                FishingRodProgression.ApplyData(loadedData.FishingRodData);

                Network_Player localPlayer = GetLocalPlayerSafe();
                if (localPlayer != null && localPlayer.Stats != null)
                {

                    if (localPlayer.Stats.stat_oxygen != null)
                    {
                        localPlayer.Stats.stat_oxygen.SetMaxValue(LungProgression.GetCurrentMaxOxygen());
                    }
                    Stat_Fatigue fatigueStat = EnsureLocalFatigueStat(localPlayer.Stats);
                    if (fatigueStat != null)
                    {
                        fatigueStat.SetMaxValue(FatigueProgression.GetCurrentMaxFatigue());
                    }
                }
                else
                {

                }
                BetterRaft.StatsDisplayManager.TryInitialize();
            }
            else
            {

                LungProgression.ApplyData(null);
                FatigueProgression.ApplyData(null);
                FishingRodProgression.ApplyData(null);
                BetterRaft.StatsDisplayManager.TryInitialize();
            }
            if (Instance != null) Instance.StartCoroutine(Instance.Co_LoadAndSetupFatigueUI());
        }


        public System.Collections.IEnumerator Co_LoadAndSetupFatigueUI()
        {
            EnsureBundleUnload("fatiguegui.assets");
            byte[] bundleBytes = GetEmbeddedFileBytes("fatiguegui.assets");
            if (bundleBytes == null)
            {

                yield break;
            }
            var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(bundleBytes);
            yield return bundleLoadRequest;
            fatigueDisplayBundle = bundleLoadRequest.assetBundle;
            if (fatigueDisplayBundle == null)
            {

                yield break;
            }
            GameObject canvasPrefab = fatigueDisplayBundle.LoadAsset<GameObject>("OxygenCanvas");
            if (canvasPrefab == null)
            {

                yield break;
            }
            if (fatigueDisplayCanvasInstance != null)
            {
                Destroy(fatigueDisplayCanvasInstance);
            }
            fatigueDisplayCanvasInstance = Instantiate(canvasPrefab);
            if (fatigueDisplayCanvasInstance == null)
            {

                yield break;
            }
            fatigueDisplayCanvasInstance.name = "FatigueDisplayCanvas_Modded";
            Transform foregroundTransform = fatigueDisplayCanvasInstance.transform.Find("OxygenMeter/Foreground");
            if (foregroundTransform != null) fatigueBarImage = foregroundTransform.GetComponent<Image>();
            if (fatigueBarImage == null)
            {

                Destroy(fatigueDisplayCanvasInstance); fatigueDisplayCanvasInstance = null;
                yield break;
            }
            if (fatigueDisplayCanvasInstance != null)
            {
                Transform textTransform = fatigueDisplayCanvasInstance.transform.Find("OxygenMeter/Label");
                if (textTransform != null)
                {
                    Text uiText = textTransform.GetComponent<Text>();
                    if (uiText != null) { uiText.text = "Stamina"; }
                    else
                    {
                        TMPro.TextMeshProUGUI uiTextMeshPro = textTransform.GetComponent<TMPro.TextMeshProUGUI>();
                        if (uiTextMeshPro != null) { uiTextMeshPro.text = "Stamina"; }
                    }
                }
            }
            CanvasHelper canvasHelper = ComponentManager<CanvasHelper>.Value;
            Transform parentTransform = null;
            if (canvasHelper != null)
            {
                if (canvasHelper.GetComponent<Canvas>() != null) parentTransform = canvasHelper.transform;
                else if (canvasHelper.transform.parent != null && canvasHelper.transform.parent.GetComponent<Canvas>() != null) parentTransform = canvasHelper.transform.parent;
                else parentTransform = canvasHelper.transform;
            }
            if (parentTransform != null) fatigueDisplayCanvasInstance.transform.SetParent(parentTransform, false);


            PositionFatigueUI(canvasHelper);

            fatigueDisplayCanvasInstance.SetActive(false);
        }

        void PositionFatigueUI(CanvasHelper gameCanvasHelper)
        {
            if (fatigueDisplayCanvasInstance == null) return;
            RectTransform fatigueCanvasRect = fatigueDisplayCanvasInstance.GetComponent<RectTransform>();
            if (fatigueCanvasRect == null) return;

            fatigueCanvasRect.anchorMin = new Vector2(1, 0);
            fatigueCanvasRect.anchorMax = new Vector2(1, 0);
            fatigueCanvasRect.pivot = new Vector2(1, 0);
            fatigueCanvasRect.anchoredPosition = new Vector2(-10, 70);

            if (gameCanvasHelper != null && gameCanvasHelper.fatigueParent != null)
            {
                RectTransform oxygenMeterRect = gameCanvasHelper.fatigueParent.GetComponent<RectTransform>();
                if (oxygenMeterRect != null)
                {
                    fatigueCanvasRect.anchorMin = oxygenMeterRect.anchorMin;
                    fatigueCanvasRect.anchorMax = oxygenMeterRect.anchorMax;
                    fatigueCanvasRect.pivot = oxygenMeterRect.pivot;
                    float Y_OFFSET = -(oxygenMeterRect.rect.height * oxygenMeterRect.localScale.y) - 15f;
                    fatigueCanvasRect.anchoredPosition = oxygenMeterRect.anchoredPosition + new Vector2(0, Y_OFFSET);
                }
            }
        }

        public static void EnsureBundleUnload(string name)
        {
            if (fatigueDisplayBundle != null && fatigueDisplayBundle.name == name)
            {
                fatigueDisplayBundle.Unload(true); fatigueDisplayBundle = null; return;
            }
            AssetBundle.GetAllLoadedAssetBundles().ToList().ForEach(assetbundle => {
                if (assetbundle.name == name) assetbundle.Unload(true);
            });
        }

        public void OnModUnload()
        {
            string gameFileNameOnExit = SaveAndLoad.CurrentGameFileName;


            TrySaveCombinedModProgression(gameFileNameOnExit);

            harmonyInstance?.UnpatchAll(HARMONY_ID);
            if (fatigueDisplayCanvasInstance != null) Destroy(fatigueDisplayCanvasInstance);
            if (BetterRaft_Main.fatigueDisplayBundle != null) EnsureBundleUnload("fatiguegui.assets");
            BetterRaft.StatsDisplayManager.Reset();

        }

        public static void UpdateCurrentWorldIdentifierInternal()
        {
            string previousIdentifier = CurrentWorldIdentifier;
            if (!string.IsNullOrEmpty(SaveAndLoad.CurrentGameFileName))
            {
                CurrentWorldIdentifier = SaveAndLoad.CurrentGameFileName;
            }
            else
            {
                CurrentWorldIdentifier = null;
            }
            if (previousIdentifier != CurrentWorldIdentifier)
            {

            }
        }

        public static void TrySaveCombinedModProgression(string specificWorldId = null)
        {
            string worldIdToUse = specificWorldId;

            if (string.IsNullOrEmpty(worldIdToUse))
            {
                UpdateCurrentWorldIdentifierInternal();
                worldIdToUse = CurrentWorldIdentifier;

            }
            else
            {

            }

            if (!string.IsNullOrEmpty(worldIdToUse))
            {
                PlayerModSaveData dataToSave = new PlayerModSaveData
                {
                    LungData = LungProgression.GetData(),
                    FatigueData = FatigueProgression.GetData(),
                    FishingRodData = FishingRodProgression.GetData()
                };
                ModDataManager.SaveCombinedProgression(worldIdToUse, dataToSave);
            }
            else
            {

            }
        }
    }

    [HarmonyPatch(typeof(PlayerStats))]
    [HarmonyPatch("Start")]
    class Patch_PlayerStats_Start_InitializeData
    {
        static void Postfix(PlayerStats __instance)
        {
            if (!BetterRaft_Main.IsLocalPlayerStats(__instance)) return;

            BetterRaft_Main.UpdateAllModSettingsFromAPI();

            Stat_Fatigue fatigueStat = BetterRaft_Main.EnsureLocalFatigueStat(__instance);
            if (fatigueStat != null)
            {
                fatigueStat.SetMaxValue(FatigueProgression.GetCurrentMaxFatigue());
            }

            if (__instance.stat_oxygen != null)
            {
                __instance.stat_oxygen.SetMaxValue(LungProgression.GetCurrentMaxOxygen());
            }
        }
    }

    [HarmonyPatch(typeof(PlayerStats))]
    [HarmonyPatch("Update")]
    class Patch_PlayerStats_Update_HandleProgression
    {
        static void Postfix(PlayerStats __instance)
        {

            if (__instance == null || !BetterRaft_Main.IsLocalPlayerStats(__instance)) return;

            Stat_Fatigue fatigueStat = BetterRaft_Main.EnsureLocalFatigueStat(__instance);
            PersonController pc = __instance.GetComponent<PersonController>();

            if (__instance.IsDead || fatigueStat == null || pc == null)
            {
                if (BetterRaft_Main.fatigueDisplayCanvasInstance != null)
                    BetterRaft_Main.fatigueDisplayCanvasInstance.SetActive(false);
                return;
            }

            if (fatigueStat != null && pc != null && !__instance.IsDead)
            {
                ControllerType currentCtrlType = pc.controllerType;

                bool isFatigueUiRelevant = (currentCtrlType == ControllerType.Water) ||
                                           (fatigueStat.Value < fatigueStat.Max);

                if (BetterRaft_Main.fatigueDisplayCanvasInstance != null)
                {
                    if (isFatigueUiRelevant)
                    {
                        if (!BetterRaft_Main.fatigueDisplayCanvasInstance.activeSelf)
                            BetterRaft_Main.fatigueDisplayCanvasInstance.SetActive(true);

                        if (BetterRaft_Main.fatigueBarImage == null && BetterRaft_Main.fatigueDisplayCanvasInstance.activeInHierarchy)
                        {
                            Transform foregroundTransform = BetterRaft_Main.fatigueDisplayCanvasInstance.transform.Find("OxygenMeter/Foreground");
                            if (foregroundTransform != null) BetterRaft_Main.fatigueBarImage = foregroundTransform.GetComponent<Image>();
                        }

                        if (BetterRaft_Main.fatigueBarImage != null)
                        {
                            if (fatigueStat.Max > 0)
                            {
                                float fatigueRatio = fatigueStat.Value / fatigueStat.Max;
                                BetterRaft_Main.fatigueBarImage.fillAmount = fatigueRatio;
                                BetterRaft_Main.fatigueBarImage.color = Color.Lerp(Color.red, Color.cyan, fatigueRatio);
                            }
                            else
                            {
                                BetterRaft_Main.fatigueBarImage.fillAmount = 0;
                                BetterRaft_Main.fatigueBarImage.color = Color.red;
                            }
                        }
                    }
                    else
                    {
                        if (BetterRaft_Main.fatigueDisplayCanvasInstance.activeSelf)
                            BetterRaft_Main.fatigueDisplayCanvasInstance.SetActive(false);
                    }
                }
                fatigueStat.ManagedUpdate(Time.deltaTime);
                FatigueProgression.RecordFatigueActivity(Time.deltaTime, fatigueStat, pc);
            }
        }
    }

    [HarmonyPatch(typeof(PersonController))]
    [HarmonyPatch("WaterControll")]
    class Patch_PersonController_WaterControll_HandleSinking
    {
        private static FieldInfo _moveDirectionField;
        private const float INITIAL_SINKING_FORCE_AT_SURFACE = 14f;
        private const float SUBMERGED_SINKING_SPEED = 3.0f;

        static void Prepare()
        {
            _moveDirectionField = typeof(PersonController).GetField("moveDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_moveDirectionField == null)
            {

            }
        }
        static void Postfix(PersonController __instance)
        {
            if (__instance == null ||
                !__instance.enabled ||
                _moveDirectionField == null ||
                !BetterRaft_Main.IsLocalPersonController(__instance))
            {
                return;
            }
            Stat_Fatigue fatigueStat = __instance.gameObject.GetComponent<Stat_Fatigue>();
            if (fatigueStat != null && fatigueStat.IsExhaustedAndSinking)
            {
                Vector3 currentMoveDirection = (Vector3)_moveDirectionField.GetValue(__instance);
                float targetSinkingSpeed;
                UltimateWater.SubmersionState submersionState = __instance.SubmersionState;
                if (submersionState != UltimateWater.SubmersionState.Full)
                {
                    targetSinkingSpeed = INITIAL_SINKING_FORCE_AT_SURFACE;
                }
                else
                {
                    targetSinkingSpeed = SUBMERGED_SINKING_SPEED;
                }
                currentMoveDirection.y = -targetSinkingSpeed;
                _moveDirectionField.SetValue(__instance, currentMoveDirection);
            }
        }
    }

    [HarmonyPatch(typeof(SaveAndLoad))]
    [HarmonyPatch("SaveGame")]
    class Patch_OnGameSave_SaveAllModData
    {
        static void Postfix()
        {

            BetterRaft_Main.TrySaveCombinedModProgression();
        }
    }



    [HarmonyPatch(typeof(Stat_Oxygen), "Update")]
    class Patch_StatOxygen_Update_ApplyProgression
    {
        private static Dictionary<Stat_Oxygen, float> valueBeforeUpdateLookup_Oxygen = new Dictionary<Stat_Oxygen, float>();



        static void Prefix(Stat_Oxygen __instance)
        {
            if (__instance == null || !BetterRaft_Main.IsLocalOxygenStat(__instance)) return;


            float oldMax = __instance.Max;
            float currentValue = __instance.Value;


            float bonusOxygen = OxygenTankManager.GetBonusMaxOxygen();
            float newMax = LungProgression.GetCurrentMaxOxygen() + bonusOxygen;


            __instance.SetMaxValue(newMax);


            if (newMax > oldMax && oldMax > 0)
            {

                __instance.Value = newMax * (currentValue / oldMax);
            }
            else if (newMax < oldMax && oldMax > 0)
            {

                float percentage = currentValue / oldMax;
                __instance.Value = newMax * percentage;
            }


            __instance.SetOxygenLostPerSecond(LungProgression.GetCurrentOxygenLossRate());


            if (!valueBeforeUpdateLookup_Oxygen.ContainsKey(__instance)) valueBeforeUpdateLookup_Oxygen.Add(__instance, __instance.Value);
            else valueBeforeUpdateLookup_Oxygen[__instance] = __instance.Value;
        }


        static void Postfix(Stat_Oxygen __instance, PersonController ___personController)
        {
            if (__instance == null || !BetterRaft_Main.IsLocalOxygenStat(__instance)) return;

            float tankRegenRate = OxygenTankManager.GetOxygenRegenRate();
            if (tankRegenRate > 0)
            {
                __instance.Value += tankRegenRate * Time.deltaTime;
            }

            float valueBeforeTick;
            if (valueBeforeUpdateLookup_Oxygen.TryGetValue(__instance, out valueBeforeTick))
            {
                valueBeforeUpdateLookup_Oxygen.Remove(__instance);
                if (tankRegenRate <= 0 && ___personController != null && ___personController.SubmersionState != UltimateWater.SubmersionState.Full)
                {
                    float rawGainByGameLogic = __instance.Value - valueBeforeTick;
                    if (rawGainByGameLogic > 0)
                    {
                        float baseModifiedGain = rawGainByGameLogic * LungProgression.OxygenRegenerationModifier;
                        float finalGainThisTick = (valueBeforeTick >= (__instance.Max * 0.90f) && valueBeforeTick < __instance.Max) ? baseModifiedGain * 5.5f : baseModifiedGain;
                        __instance.Value = valueBeforeTick + finalGainThisTick;
                    }
                }
            }

            if (__instance.Value > __instance.Max)
            {
                __instance.Value = __instance.Max;
            }

            if (___personController != null && BetterRaft_Main.IsLocalPersonController(___personController))
            {
                LungProgression.RecordDiveActivity(__instance, ___personController);
            }
        }
    }


    [HarmonyPatch(typeof(Slot_Equip), "SetItem", new[] { typeof(ItemInstance) })]
    class Patch_SlotEquip_SetItem_CheckForOxygenTank
    {
        static void Postfix(Slot_Equip __instance)
        {
            if (__instance == null) return;

            try
            {
                Traverse traverse = Traverse.Create(__instance);
                EquipSlotType currentSlotType = traverse.Field<EquipSlotType>("slotType").Value;

                const EquipSlotType oxygenSlotType = (EquipSlotType)2;

                if (currentSlotType == oxygenSlotType)
                {

                    if (!__instance.IsEmpty)
                    {
                        Item_Base equippedItem = __instance.itemInstance.baseItem;
                        OxygenTankManager.ActivateTank(equippedItem);
                    }
                    else
                    {
                        OxygenTankManager.DeactivateTank();
                    }
                }
            }
            catch (System.Exception)
            {

            }
        }
    }
}


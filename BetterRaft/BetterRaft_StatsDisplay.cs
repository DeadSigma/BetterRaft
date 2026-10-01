using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BetterRaft
{
    public static class PPS_UI
    {
        private static GameObject buttonPrefab;
        private static GameObject backgroundPrefab;
        private static GameObject textPrefab;
        private static Font fallbackFont;

        public static bool PrefabsInitialized { get; private set; } = false;

        public static void ResetInitializationFlag()
        {
            PrefabsInitialized = false;
            buttonPrefab = null;
            backgroundPrefab = null;
            textPrefab = null;
            fallbackFont = null;
        }

        public static bool TryInitPrefabs()
        {
            CanvasHelper canvasHelper = ComponentManager<CanvasHelper>.Value;
            if (canvasHelper == null || canvasHelper.transform == null)
            {
                PrefabsInitialized = false;
                return false;
            }

            buttonPrefab = null;
            backgroundPrefab = null;
            textPrefab = null;

            var researchTable = ComponentManager<Inventory_ResearchTable>.Value;
            if (researchTable != null && researchTable.gameObject != null)
            {
                Transform researchButtonTransform = researchTable.gameObject.transform.FindChildRecursively("ResearchButton");
                if (researchButtonTransform != null)
                {
                    buttonPrefab = researchButtonTransform.gameObject;
                }
            }

            var localPlayer = ComponentManager<Raft_Network>.Value?.GetLocalPlayer();
            if (localPlayer != null && localPlayer.Inventory != null && localPlayer.Inventory.gameObject != null)
            {
                Transform playerInventoryTransform = localPlayer.Inventory.gameObject.transform;

                Transform bgTransform = playerInventoryTransform.FindChildRecursively("BrownBackground");
                if (bgTransform != null)
                {
                    backgroundPrefab = bgTransform.gameObject;
                }

                Transform textUITransform = playerInventoryTransform.FindChildRecursively("InventoryText");
                if (textUITransform != null)
                {
                    textPrefab = textUITransform.gameObject;
                }
            }

            PrefabsInitialized = true;
            return true;
        }

        private static Font GetUIFont()
        {
            if (fallbackFont != null) return fallbackFont;

            if (textPrefab != null)
            {
                Text prefabText = textPrefab.GetComponent<Text>();
                if (prefabText != null && prefabText.font != null)
                {
                    fallbackFont = prefabText.font;
                    return fallbackFont;
                }
            }

            CanvasHelper canvasHelper = ComponentManager<CanvasHelper>.Value;
            if (canvasHelper != null)
            {
                Text anyText = canvasHelper.GetComponentInChildren<Text>(true);
                if (anyText != null && anyText.font != null)
                {
                    fallbackFont = anyText.font;
                    return fallbackFont;
                }
            }

            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            if (fonts != null && fonts.Length > 0)
            {
                fallbackFont = fonts[0];
                return fallbackFont;
            }

            try
            {
                fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return fallbackFont;
        }

        private static GameObject CreateFallbackButton(GameObject parent, string text, UnityAction onClick)
        {
            GameObject buttonGO = new GameObject(text.Replace(" ", "") + "Button_PPS", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent.transform, false);

            Image image = buttonGO.GetComponent<Image>();
            image.color = new Color(0.28f, 0.20f, 0.13f, 0.96f);

            Button button = buttonGO.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.38f, 0.29f, 0.20f, 1f);
            colors.pressedColor = new Color(0.20f, 0.14f, 0.09f, 1f);
            button.colors = colors;

            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(buttonGO.transform, false);

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text buttonText = textGO.GetComponent<Text>();
            buttonText.text = text;
            buttonText.font = GetUIFont();
            buttonText.fontSize = 16;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            buttonText.raycastTarget = false;

            return buttonGO;
        }

        public static GameObject AddButton(GameObject parent, string text, UnityAction onClick, Vector2 anchoredPosition, Vector2 size,
                                           Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            GameObject buttonGO;

            if (buttonPrefab != null)
            {
                buttonGO = Object.Instantiate(buttonPrefab, parent.transform, false);
                buttonGO.name = text.Replace(" ", "") + "Button_PPS";
                buttonGO.SetActive(true);

                Text buttonText = buttonGO.GetComponentInChildren<Text>(true);
                if (buttonText != null)
                {
                    buttonText.text = text;
                    var localizeComponent = buttonText.gameObject.GetComponent("Localize");
                    if (localizeComponent != null) Object.Destroy(localizeComponent);
                }

                Button buttonComponent = buttonGO.GetComponent<Button>();
                if (buttonComponent != null)
                {
                    buttonComponent.onClick = new Button.ButtonClickedEvent();
                    buttonComponent.onClick.AddListener(onClick);
                }
                else
                {
                    Object.Destroy(buttonGO);
                    buttonGO = CreateFallbackButton(parent, text, onClick);
                }
            }
            else
            {
                buttonGO = CreateFallbackButton(parent, text, onClick);
            }

            RectTransform rectTransform = buttonGO.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = anchorMin ?? new Vector2(0.5f, 1f);
                rectTransform.anchorMax = anchorMax ?? new Vector2(0.5f, 1f);
                rectTransform.pivot = pivot ?? new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = size;
                rectTransform.anchoredPosition = anchoredPosition;
            }

            buttonGO.transform.SetAsLastSibling();
            return buttonGO;
        }

        public static GameObject CreatePanel(GameObject parent, Vector2 anchoredPosition, Vector2 size, string panelName = "Panel_PPS")
        {
            GameObject panelGO;

            if (backgroundPrefab != null)
            {
                panelGO = Object.Instantiate(backgroundPrefab, parent.transform, false);
                panelGO.name = panelName;
                panelGO.SetActive(true);
            }
            else
            {
                panelGO = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelGO.transform.SetParent(parent.transform, false);
                Image image = panelGO.GetComponent<Image>();
                image.color = new Color(0.12f, 0.08f, 0.05f, 0.96f);
            }

            RectTransform rectTransform = panelGO.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = size;
                rectTransform.anchoredPosition = anchoredPosition;
            }

            panelGO.transform.SetAsLastSibling();
            return panelGO;
        }

        public static Text AddText(GameObject parent, string initialText, Vector2 anchoredPosition, Vector2 size, int fontSize = 18, TextAnchor alignment = TextAnchor.MiddleLeft, Color? color = null)
        {
            GameObject textGO;

            if (textPrefab != null)
            {
                textGO = Object.Instantiate(textPrefab, parent.transform, false);
                textGO.SetActive(true);
            }
            else
            {
                textGO = new GameObject("Text_PPS", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textGO.transform.SetParent(parent.transform, false);
            }

            string textPart = initialText.Length > 10 ? initialText.Substring(0, 10) : initialText;
            textGO.name = "Text_PPS_" + textPart.Replace(" ", "").Replace(":", "").Replace("(", "").Replace(")", "").Replace("+", "").Replace("%", "");

            Text textComponent = textGO.GetComponent<Text>();
            if (textComponent == null)
            {
                Object.Destroy(textGO);
                return null;
            }

            textComponent.text = initialText;
            textComponent.font = textComponent.font ?? GetUIFont();
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = color ?? Color.white;
            textComponent.raycastTarget = false;

            var localizeComponent = textGO.GetComponent("Localize");
            if (localizeComponent != null) Object.Destroy(localizeComponent);

            RectTransform rectTransform = textGO.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.sizeDelta = size;
                rectTransform.anchoredPosition = anchoredPosition;
            }

            return textComponent;
        }
    }

    public static class StatsDisplayManager
    {
        private static GameObject statsButtonHUD;
        private static GameObject myStatsButton;
        private static GameObject statsWindowPanel;
        private static bool isStatsWindowVisible = false;

        private static Text fishingLevelText;
        private static Text fishingExpText;
        private static Text fishingSpeedBonusText;
        private static Text fishingExtraCatchChanceText;
        private static Text staminaStatsText;
        private static Text staminaProgressText;
        private static Text oxygenStatsText;
        private static Text oxygenProgressText;

        public static bool IsButtonUIVisible { get; private set; } = false;

        public static void TryInitialize()
        {
            if (statsButtonHUD != null && statsWindowPanel != null)
            {
                if (!PPS_UI.PrefabsInitialized)
                {

                    PPS_UI.TryInitPrefabs();
                }
                return;
            }

            if (!PPS_UI.TryInitPrefabs())
            {

                return;
            }

            CanvasHelper canvasHelper = ComponentManager<CanvasHelper>.Value;
            if (canvasHelper == null || canvasHelper.transform == null)
            {

                return;
            }

            if (statsButtonHUD == null)
            {
                statsButtonHUD = new GameObject("StatsButtonHUD_PPS", typeof(RectTransform));
                statsButtonHUD.transform.SetParent(canvasHelper.transform, false);
                RectTransform hudRect = statsButtonHUD.GetComponent<RectTransform>();
                hudRect.anchorMin = new Vector2(0, 1);
                hudRect.anchorMax = new Vector2(1, 1);
                hudRect.pivot = new Vector2(0.5f, 1f);
                hudRect.sizeDelta = new Vector2(0, 50);
                hudRect.anchoredPosition = new Vector2(0, -30f);

                myStatsButton = PPS_UI.AddButton(statsButtonHUD, "My Stats", OnMyStatsButtonClicked, new Vector2(170, -170), new Vector2(120, 30));

                if (myStatsButton == null)
                {

                    if (statsButtonHUD != null) Object.Destroy(statsButtonHUD); statsButtonHUD = null; return;
                }

                statsButtonHUD.SetActive(false);
            }

            if (statsWindowPanel == null)
            {
                float panelWidth = 320f;
                float panelHeight = 280f;
                Vector2 panelAnchoredPosition = new Vector2(411f, 211f);

                statsWindowPanel = PPS_UI.CreatePanel(canvasHelper.transform.gameObject, panelAnchoredPosition, new Vector2(panelWidth, panelHeight), "StatsWindowPanel_PPS");



                float textWidth = panelWidth - 30;
                float currentY = -15f;
                int defaultFontSize = 16;
                float lineSpacing = 3f;
                float sectionSpacing = 8f;

                PPS_UI.AddText(statsWindowPanel, "Player Stats", new Vector2((panelWidth - textWidth) / 2f, currentY), new Vector2(textWidth, 30), 20, TextAnchor.MiddleCenter);
                currentY -= (30 + sectionSpacing);

                fishingLevelText = PPS_UI.AddText(statsWindowPanel, "Fishing Lvl:", new Vector2(15, currentY), new Vector2(textWidth, 25), defaultFontSize);
                currentY -= (20 + lineSpacing);
                fishingExpText = PPS_UI.AddText(statsWindowPanel, "  Exp:", new Vector2(15, currentY), new Vector2(textWidth, 25), defaultFontSize);
                currentY -= (20 + lineSpacing);
                fishingSpeedBonusText = PPS_UI.AddText(statsWindowPanel, "  Speed Bonus:", new Vector2(15, currentY), new Vector2(textWidth, 20), defaultFontSize);
                currentY -= (20 + lineSpacing);
                fishingExtraCatchChanceText = PPS_UI.AddText(statsWindowPanel, "  Extra Catch:", new Vector2(15, currentY), new Vector2(textWidth, 20), defaultFontSize);
                currentY -= (20 + sectionSpacing);

                staminaStatsText = PPS_UI.AddText(statsWindowPanel, "Stamina:", new Vector2(15, currentY), new Vector2(textWidth, 25), defaultFontSize);
                currentY -= (20 + lineSpacing);
                staminaProgressText = PPS_UI.AddText(statsWindowPanel, "  Progress:", new Vector2(15, currentY), new Vector2(textWidth, 25), defaultFontSize);
                currentY -= (20 + sectionSpacing);

                oxygenStatsText = PPS_UI.AddText(statsWindowPanel, "Oxygen:", new Vector2(15, currentY), new Vector2(textWidth, 25), defaultFontSize);
                currentY -= (20 + lineSpacing);
                oxygenProgressText = PPS_UI.AddText(statsWindowPanel, "  Dives:", new Vector2(15, currentY), new Vector2(textWidth, 25), defaultFontSize);

                statsWindowPanel.SetActive(false);
                isStatsWindowVisible = false;

            }
        }

        private static void OnMyStatsButtonClicked()
        {

            ToggleStatsWindow();
        }

        private static void ToggleStatsWindow()
        {
            if (statsWindowPanel == null) TryInitialize();
            if (statsWindowPanel == null) return;

            isStatsWindowVisible = !isStatsWindowVisible;
            statsWindowPanel.SetActive(isStatsWindowVisible);

            if (isStatsWindowVisible)
            {
                statsWindowPanel.transform.SetAsLastSibling();
                UpdateStatsTexts();

            }

        }

        private static void CloseStatsWindow()
        {
            if (statsWindowPanel != null)
            {
                statsWindowPanel.SetActive(false);
                isStatsWindowVisible = false;

            }
        }

        private static void UpdateStatsTexts()
        {
            if (!isStatsWindowVisible || statsWindowPanel == null) return;

            if (fishingLevelText != null)
            {
                fishingLevelText.text = $"Fishing Lvl: {FishingRodProgression.FishingLevel}";
            }
            if (fishingExpText != null)
            {
                int currentLvl = FishingRodProgression.FishingLevel;
                int currentExp = FishingRodProgression.FishingExperience;
                if (currentLvl < FishingRodProgression.MaxFishingLevel)
                {
                    int expToNextLvl = FishingRodProgression.GetExperienceNeededForLevel(currentLvl);
                    fishingExpText.text = $"  Exp: {currentExp}/{expToNextLvl}";
                }
                else
                {
                    fishingExpText.text = "  Exp: MAX";
                }
            }
            if (fishingSpeedBonusText != null)
            {
                float speedBonusPercentage = (FishingRodProgression.GetCurrentFishingSpeedModifier() - 1.0f) * 100f;
                fishingSpeedBonusText.text = $"  Speed Bonus: +{speedBonusPercentage:F0}%";
            }
            if (fishingExtraCatchChanceText != null)
            {
                fishingExtraCatchChanceText.text = $"  Extra Catch: {FishingRodProgression.GetCurrentExtraCatchChance():F1}%";
            }

            if (staminaStatsText != null)
            {
                staminaStatsText.text = $"Stamina: Lvl {FatigueProgression.FatigueLevel} (Capacity: {FatigueProgression.GetCurrentMaxFatigue():F0})";
            }
            if (staminaProgressText != null)
            {
                staminaProgressText.text = $"  Progress: {FatigueProgression.GetFormattedProgressTime()}";
            }

            if (oxygenStatsText != null)
            {
                oxygenStatsText.text = $"Oxygen: Lvl {LungProgression.LungLevel} (Capacity: {LungProgression.GetCurrentMaxOxygen():F0})";
            }
            if (oxygenProgressText != null)
            {
                oxygenProgressText.text = $"  Dives: {LungProgression.GetCurrentDivesProgress()} / {LungProgression.DivesPerLevelUp}";
            }
        }

        public static void ShowButtonUI()
        {
            if (statsButtonHUD == null || statsWindowPanel == null) TryInitialize();
            if (statsButtonHUD == null) return;

            statsButtonHUD.SetActive(true);
            statsButtonHUD.transform.SetAsLastSibling();
            IsButtonUIVisible = true;
        }

        public static void HideButtonUI()
        {
            if (statsButtonHUD != null) statsButtonHUD.SetActive(false);
            IsButtonUIVisible = false;

            if (isStatsWindowVisible) CloseStatsWindow();
        }

        public static void Reset()
        {
            if (statsButtonHUD != null) Object.Destroy(statsButtonHUD);
            if (statsWindowPanel != null) Object.Destroy(statsWindowPanel);

            statsButtonHUD = null;
            myStatsButton = null;
            statsWindowPanel = null;
            fishingLevelText = null;
            fishingExpText = null;
            fishingSpeedBonusText = null;
            fishingExtraCatchChanceText = null;
            staminaStatsText = null;
            staminaProgressText = null;
            oxygenStatsText = null;
            oxygenProgressText = null;

            IsButtonUIVisible = false;
            isStatsWindowVisible = false;
            PPS_UI.ResetInitializationFlag();

        }
    }

    [HarmonyPatch(typeof(CanvasHelper))]
    public static class StatsDisplay_HarmonyPatches
    {
        [HarmonyPatch("OpenMenu")]
        [HarmonyPostfix]
        static void OpenMenu_Postfix(CanvasHelper __instance, MenuType menuType)
        {
            if (menuType == MenuType.Inventory)
            {
                StatsDisplayManager.ShowButtonUI();
            }
        }

        [HarmonyPatch("CloseMenu")]
        [HarmonyPrefix]
        static void CloseMenu_Prefix(MenuType menuType)
        {
            if (menuType == MenuType.Inventory)
            {
                StatsDisplayManager.HideButtonUI();
            }
        }
    }
}
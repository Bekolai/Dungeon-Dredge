using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace DungeonDredge.Editor
{
    /// <summary>
    /// Editor utility that programmatically builds the entire UI Canvas hierarchy
    /// for Dungeon Dredge, using the Artsystack Fantasy RPG GUI art assets.
    /// Run from menu: DungeonDredge > Build UI
    /// </summary>
    public class UIBuilderEditor : UnityEditor.Editor
    {
        // Asset paths (relative to Assets/)
        private const string SpritesPath = "Assets/Assets/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/components/";
        private const string IconsWhitePath = "Assets/Assets/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/flaticon/white/";
        private const string IconsTexturedPath = "Assets/Assets/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/flaticon/textured/";
        private const string FontPath = "Assets/Assets/Artsystack - Fantasy RPG GUI/ResourcesData/Font/";
        private const string PrefabOutputPath = "Assets/Prefabs/UI/Generated/";

        // Cached assets
        private static TMP_FontAsset _font;
        private static Sprite _bgLayer01;
        private static Sprite _bgLayer02;
        private static Sprite _heartBg;
        private static Sprite _heartFill;
        private static Sprite _heartFrame;
        private static Sprite _progressBarBg;
        private static Sprite _progressBarTop;
        private static Sprite _statsSliderBg;
        private static Sprite _statsSliderTop;
        private static Sprite _expBarBg;
        private static Sprite _expBarTop;
        private static Sprite _itemSlot;
        private static Sprite _itemSelectedSlot;
        private static Sprite _popUp;
        private static Sprite _descBox01;
        private static Sprite _button01;
        private static Sprite _button02;
        private static Sprite _characterFrame;
        private static Sprite _ingameIconSlot;
        private static Sprite _headerBox;
        private static Sprite _coinsFrame;
        private static Sprite _placeholderSlot;
        private static Sprite _skillSlot;
        private static Sprite _splittDivider;
        private static Sprite _panelNameHeader;

        [MenuItem("DungeonDredge/Build UI/Build All", false, 100)]
        public static void BuildAll()
        {
            LoadAssets();
            EnsureOutputDirectory();

            BuildGameHUD();
            BuildInventoryPanel();
            BuildMenuCanvas();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UIBuilder] All UI prefabs built successfully in " + PrefabOutputPath);
        }

        [MenuItem("DungeonDredge/Build UI/Build Game HUD Only", false, 101)]
        public static void BuildGameHUDOnly()
        {
            LoadAssets();
            EnsureOutputDirectory();
            BuildGameHUD();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("DungeonDredge/Build UI/Build Inventory Panel Only", false, 102)]
        public static void BuildInventoryPanelOnly()
        {
            LoadAssets();
            EnsureOutputDirectory();
            BuildInventoryPanel();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("DungeonDredge/Build UI/Build Menu Canvas Only", false, 103)]
        public static void BuildMenuCanvasOnly()
        {
            LoadAssets();
            EnsureOutputDirectory();
            BuildMenuCanvas();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #region Asset Loading

        private static void LoadAssets()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath + "MedievalSharp-Regular SDF.asset");
            if (_font == null)
                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath + "Kurale-Regular SDF.asset");

            _bgLayer01 = LoadSprite("bg_layer_01");
            _bgLayer02 = LoadSprite("bg_layer_02");
            _heartBg = LoadSprite("heart_bg");
            _heartFill = LoadSprite("heart_fill");
            _heartFrame = LoadSprite("heart_frame");
            _progressBarBg = LoadSprite("progress_bar_bg");
            _progressBarTop = LoadSprite("progress_bar_top");
            _statsSliderBg = LoadSprite("stats_slider_bg");
            _statsSliderTop = LoadSprite("stats_slider_top");
            _expBarBg = LoadSprite("exp_bar_bg");
            _expBarTop = LoadSprite("exp_bar_top");
            _itemSlot = LoadSprite("item_slot");
            _itemSelectedSlot = LoadSprite("item_selected_slot");
            _popUp = LoadSprite("pop_up");
            _descBox01 = LoadSprite("description_box_01");
            _button01 = LoadSprite("button_01");
            _button02 = LoadSprite("button_02");
            _characterFrame = LoadSprite("character_frame");
            _ingameIconSlot = LoadSprite("ingame_icon_slot");
            _headerBox = LoadSprite("header_box");
            _coinsFrame = LoadSprite("coins_frame");
            _placeholderSlot = LoadSprite("placeholder_slot");
            _skillSlot = LoadSprite("skill_slot");
            _splittDivider = LoadSprite("splitt_divider");
            _panelNameHeader = LoadSprite("panel_name_header");

            if (_font == null)
                Debug.LogWarning("[UIBuilder] Could not find TMP font asset. Text will use default font.");
        }

        private static Sprite LoadSprite(string name)
        {
            string path = SpritesPath + name + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[UIBuilder] Could not load sprite: {path}");
            }
            return sprite;
        }

        private static Sprite LoadIcon(string name, bool textured = false)
        {
            string basePath = textured ? IconsTexturedPath : IconsWhitePath;
            string path = basePath + name + ".png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        #endregion

        #region Utilities

        private static void EnsureOutputDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI/Generated"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Prefabs");
                    }
                    AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
                }
                AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Generated");
            }
        }

        private static GameObject CreateCanvas(string name, int sortOrder)
        {
            GameObject canvasGO = new GameObject(name);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            return canvasGO;
        }

        private static GameObject CreatePanel(string name, Transform parent, Sprite bgSprite = null)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (bgSprite != null)
            {
                Image img = panel.AddComponent<Image>();
                img.sprite = bgSprite;
                img.type = Image.Type.Sliced;
            }

            return panel;
        }

        private static RectTransform CreateElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            return rt;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            RectTransform rt = CreateElement(name, parent);
            rt.sizeDelta = size;
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite != null)
                img.type = Image.Type.Sliced;
            return img;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text,
            float fontSize = 18f, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            RectTransform rt = CreateElement(name, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            if (_font != null) tmp.font = _font;
            tmp.color = new Color(0.9f, 0.85f, 0.7f);
            return tmp;
        }

        private static Slider CreateSlider(string name, Transform parent, Sprite bgSprite, Sprite fillSprite,
            Vector2 size, Color fillColor)
        {
            // Root
            RectTransform sliderRT = CreateElement(name, parent);
            sliderRT.sizeDelta = size;
            Slider slider = sliderRT.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Background
            Image bgImage = CreateImage("Background", sliderRT, bgSprite, Vector2.zero);
            RectTransform bgRT = bgImage.rectTransform;
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // Fill Area
            RectTransform fillArea = CreateElement("Fill Area", sliderRT);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = new Vector2(5, 0);
            fillArea.offsetMax = new Vector2(-5, 0);

            // Fill
            Image fillImage = CreateImage("Fill", fillArea, fillSprite, Vector2.zero);
            RectTransform fillRT = fillImage.rectTransform;
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1, 1);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillImage.color = fillColor;

            slider.fillRect = fillRT;

            return slider;
        }

        private static Button CreateButton(string name, Transform parent, Sprite sprite, string label,
            Vector2 size)
        {
            Image btnImg = CreateImage(name, parent, sprite, size);
            Button btn = btnImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            TextMeshProUGUI text = CreateText("Label", btnImg.rectTransform, label, 20f,
                TextAlignmentOptions.Center);
            RectTransform textRT = text.rectTransform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            return btn;
        }

        private static void SavePrefab(GameObject go, string prefabName)
        {
            string path = PrefabOutputPath + prefabName + ".prefab";
            // Delete existing
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[UIBuilder] Saved prefab: {path}");
        }

        /// <summary>
        /// Anchor a RectTransform to a specific corner/edge with a given offset and size.
        /// </summary>
        private static void AnchorTo(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        #endregion

        #region Build Game HUD

        private static void BuildGameHUD()
        {
            GameObject canvas = CreateCanvas("GameHUD", 0);

            // --- HP Bar (Top-Left) ---
            BuildHPBar(canvas.transform);

            // --- Stamina Bar (Below HP) ---
            BuildStaminaBar(canvas.transform);

            // --- Stealth Section (Below Stamina) ---
            BuildStealthSection(canvas.transform);

            // --- Weight Bar (Bottom-Right) ---
            BuildWeightBar(canvas.transform);

            // --- Tool Hotbar (Bottom-Center) ---
            BuildToolHotbar(canvas.transform);

            // --- Crosshair (Center) ---
            BuildCrosshair(canvas.transform);

            // --- Interaction Prompt (Center-Bottom) ---
            BuildInteractionPrompt(canvas.transform);

            // --- Target Info (Right-Center) ---
            BuildTargetInfo(canvas.transform);

            // --- Threat Vignette (Full screen overlay) ---
            BuildThreatVignette(canvas.transform);

            // Add HUDManager component
            var hudManager = canvas.AddComponent<UI.HUDManager>();

            // Wire references via SerializedObject
            SerializedObject so = new SerializedObject(hudManager);

            // HP
            so.FindProperty("hpSlider").objectReferenceValue = canvas.transform.Find("HPBar/HPSlider")?.GetComponent<Slider>();
            so.FindProperty("hpFill").objectReferenceValue = canvas.transform.Find("HPBar/HPSlider/Fill Area/Fill")?.GetComponent<Image>();
            so.FindProperty("hpText").objectReferenceValue = canvas.transform.Find("HPBar/HPText")?.GetComponent<TextMeshProUGUI>();

            // Stealth Eye
            so.FindProperty("stealthEyeImage").objectReferenceValue = canvas.transform.Find("StealthSection/EyeIcon")?.GetComponent<Image>();

            // Noise Bar - CanvasGroup is on the noise slider itself (not the parent)
            so.FindProperty("noiseSlider").objectReferenceValue = canvas.transform.Find("StealthSection/NoiseSlider")?.GetComponent<Slider>();
            so.FindProperty("noiseFill").objectReferenceValue = canvas.transform.Find("StealthSection/NoiseSlider/Fill Area/Fill")?.GetComponent<Image>();
            var noiseGroup = canvas.transform.Find("StealthSection/NoiseSlider")?.GetComponent<CanvasGroup>();
            so.FindProperty("noiseBarGroup").objectReferenceValue = noiseGroup;

            // Threat
            so.FindProperty("threatVignette").objectReferenceValue = canvas.transform.Find("ThreatVignette")?.GetComponent<Image>();

            // Weight
            so.FindProperty("weightSlider").objectReferenceValue = canvas.transform.Find("WeightBar/WeightSlider")?.GetComponent<Slider>();
            so.FindProperty("weightFill").objectReferenceValue = canvas.transform.Find("WeightBar/WeightSlider/Fill Area/Fill")?.GetComponent<Image>();
            so.FindProperty("weightText").objectReferenceValue = canvas.transform.Find("WeightBar/WeightText")?.GetComponent<TextMeshProUGUI>();

            // Stamina
            so.FindProperty("staminaSlider").objectReferenceValue = canvas.transform.Find("StaminaBar/StaminaSlider")?.GetComponent<Slider>();
            so.FindProperty("staminaFill").objectReferenceValue = canvas.transform.Find("StaminaBar/StaminaSlider/Fill Area/Fill")?.GetComponent<Image>();
            so.FindProperty("staminaGroup").objectReferenceValue = canvas.transform.Find("StaminaBar")?.GetComponent<CanvasGroup>();

            // Crosshair
            so.FindProperty("crosshairImage").objectReferenceValue = canvas.transform.Find("Crosshair")?.GetComponent<Image>();

            // Interaction
            so.FindProperty("interactionPromptGroup").objectReferenceValue = canvas.transform.Find("InteractionPrompt")?.GetComponent<CanvasGroup>();
            so.FindProperty("interactionText").objectReferenceValue = canvas.transform.Find("InteractionPrompt/PromptText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("interactionKeyIcon").objectReferenceValue = canvas.transform.Find("InteractionPrompt/KeyIcon")?.GetComponent<Image>();

            // Target Info
            so.FindProperty("targetInfoGroup").objectReferenceValue = canvas.transform.Find("TargetInfo")?.GetComponent<CanvasGroup>();
            so.FindProperty("targetNameText").objectReferenceValue = canvas.transform.Find("TargetInfo/TargetName")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("targetRankText").objectReferenceValue = canvas.transform.Find("TargetInfo/TargetRank")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("targetHealthSlider").objectReferenceValue = canvas.transform.Find("TargetInfo/TargetHealthSlider")?.GetComponent<Slider>();

            // Tool slots
            SerializedProperty toolSlotsProp = so.FindProperty("toolSlots");
            toolSlotsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var slot = canvas.transform.Find($"ToolHotbar/ToolSlot_{i}")?.GetComponent<UI.ToolSlotUI>();
                toolSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slot;
            }

            // Weight gradient
            SerializedProperty gradientProp = so.FindProperty("weightGradient");
            if (gradientProp != null)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.3f, 0.8f, 0.3f), 0f),
                        new GradientColorKey(new Color(0.9f, 0.9f, 0.2f), 0.5f),
                        new GradientColorKey(new Color(0.9f, 0.3f, 0.1f), 0.8f),
                        new GradientColorKey(new Color(0.8f, 0.1f, 0.1f), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
                gradientProp.gradientValue = gradient;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(canvas, "GameHUD");
        }

        private static void BuildHPBar(Transform parent)
        {
            RectTransform hpRoot = CreateElement("HPBar", parent);
            AnchorTo(hpRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(250, 50));

            // Heart frame icon
            Image heartIcon = CreateImage("HeartIcon", hpRoot, _heartFrame, new Vector2(40, 40));
            AnchorTo(heartIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(40, 40));

            // HP Slider
            Slider hpSlider = CreateSlider("HPSlider", hpRoot, _progressBarBg, _progressBarTop,
                new Vector2(170, 24), new Color(0.8f, 0.2f, 0.2f));
            AnchorTo(hpSlider.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(170, 24));

            // HP Text
            TextMeshProUGUI hpText = CreateText("HPText", hpRoot, "100/100", 14f, TextAlignmentOptions.Center);
            RectTransform hpTextRT = hpText.rectTransform;
            AnchorTo(hpTextRT, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(170, 24));
        }

        private static void BuildStaminaBar(Transform parent)
        {
            RectTransform staminaRoot = CreateElement("StaminaBar", parent);
            AnchorTo(staminaRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -78), new Vector2(250, 30));

            CanvasGroup cg = staminaRoot.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; // Starts hidden

            // Stamina Slider
            Slider staminaSlider = CreateSlider("StaminaSlider", staminaRoot, _progressBarBg, _progressBarTop,
                new Vector2(200, 18), Color.green);
            AnchorTo(staminaSlider.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(200, 18));

            // Stamina icon label
            TextMeshProUGUI label = CreateText("StaminaLabel", staminaRoot, "STA", 12f, TextAlignmentOptions.Center);
            AnchorTo(label.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(44, 20));
            label.color = new Color(0.3f, 0.9f, 0.3f);
        }

        private static void BuildStealthSection(Transform parent)
        {
            RectTransform stealthRoot = CreateElement("StealthSection", parent);
            AnchorTo(stealthRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -116), new Vector2(250, 40));

            // NOTE: CanvasGroup is on the noise slider, NOT the parent.
            // This way the eye icon stays visible while the noise bar auto-hides.

            // Eye icon (placeholder - user assigns eye sprites in inspector)
            Image eyeIcon = CreateImage("EyeIcon", stealthRoot, null, new Vector2(36, 36));
            AnchorTo(eyeIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(36, 36));
            eyeIcon.color = new Color(1f, 0.9f, 0.4f);

            // Noise bar
            Slider noiseSlider = CreateSlider("NoiseSlider", stealthRoot, _statsSliderBg, _statsSliderTop,
                new Vector2(170, 14), new Color(0.3f, 0.7f, 0.3f));
            AnchorTo(noiseSlider.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(170, 14));
            noiseSlider.value = 0f;

            // CanvasGroup on the noise slider itself for auto-hide
            noiseSlider.gameObject.AddComponent<CanvasGroup>();
        }

        private static void BuildWeightBar(Transform parent)
        {
            RectTransform weightRoot = CreateElement("WeightBar", parent);
            AnchorTo(weightRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-20, 20), new Vector2(260, 50));

            // Weight Slider
            Slider weightSlider = CreateSlider("WeightSlider", weightRoot, _progressBarBg, _progressBarTop,
                new Vector2(200, 20), new Color(0.3f, 0.8f, 0.3f));
            AnchorTo(weightSlider.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(200, 20));
            weightSlider.value = 0f;

            // Weight text
            TextMeshProUGUI weightText = CreateText("WeightText", weightRoot, "0.0/40.0kg (0%)",
                13f, TextAlignmentOptions.Right);
            AnchorTo(weightText.rectTransform, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(1, 0), new Vector2(0, 24), new Vector2(200, 20));

            // Backpack icon
            Image bagIcon = CreateImage("BagIcon", weightRoot, _ingameIconSlot, new Vector2(40, 40));
            AnchorTo(bagIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(40, 40));
        }

        private static void BuildToolHotbar(Transform parent)
        {
            RectTransform hotbar = CreateElement("ToolHotbar", parent);
            AnchorTo(hotbar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(320, 70));

            float slotSize = 60f;
            float spacing = 10f;
            float totalWidth = 4 * slotSize + 3 * spacing;
            float startX = -totalWidth / 2f + slotSize / 2f;

            for (int i = 0; i < 4; i++)
            {
                RectTransform slotRT = CreateElement($"ToolSlot_{i}", hotbar);
                AnchorTo(slotRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(startX + i * (slotSize + spacing), 0),
                    new Vector2(slotSize, slotSize));

                // Background
                Image bgImg = CreateImage("Background", slotRT, _ingameIconSlot, Vector2.zero);
                RectTransform bgRT = bgImg.rectTransform;
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.offsetMin = Vector2.zero;
                bgRT.offsetMax = Vector2.zero;

                // Icon
                Image iconImg = CreateImage("Icon", slotRT, null, new Vector2(40, 40));
                AnchorTo(iconImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(40, 40));
                iconImg.enabled = false;

                // Charges text
                TextMeshProUGUI chargesText = CreateText("Charges", slotRT, "", 11f, TextAlignmentOptions.BottomRight);
                AnchorTo(chargesText.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                chargesText.rectTransform.offsetMin = new Vector2(2, 2);
                chargesText.rectTransform.offsetMax = new Vector2(-2, -2);

                // Key number text
                TextMeshProUGUI keyText = CreateText("KeyText", slotRT, (i + 1).ToString(), 12f,
                    TextAlignmentOptions.TopLeft);
                AnchorTo(keyText.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                keyText.rectTransform.offsetMin = new Vector2(4, 2);
                keyText.rectTransform.offsetMax = new Vector2(-2, -4);
                keyText.color = new Color(0.7f, 0.7f, 0.7f);

                // Cooldown overlay
                Image cooldown = CreateImage("CooldownOverlay", slotRT, null, Vector2.zero);
                RectTransform coolRT = cooldown.rectTransform;
                coolRT.anchorMin = Vector2.zero;
                coolRT.anchorMax = Vector2.one;
                coolRT.offsetMin = Vector2.zero;
                coolRT.offsetMax = Vector2.zero;
                cooldown.color = new Color(0, 0, 0, 0.6f);
                cooldown.type = Image.Type.Filled;
                cooldown.fillMethod = Image.FillMethod.Radial360;
                cooldown.fillAmount = 0f;

                // Add ToolSlotUI component
                var toolSlotUI = slotRT.gameObject.AddComponent<UI.ToolSlotUI>();
                SerializedObject slotSO = new SerializedObject(toolSlotUI);
                slotSO.FindProperty("backgroundImage").objectReferenceValue = bgImg;
                slotSO.FindProperty("iconImage").objectReferenceValue = iconImg;
                slotSO.FindProperty("chargesText").objectReferenceValue = chargesText;
                slotSO.FindProperty("keyText").objectReferenceValue = keyText;
                slotSO.FindProperty("cooldownOverlay").objectReferenceValue = cooldown;
                slotSO.FindProperty("slotNumber").intValue = i + 1;
                slotSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildCrosshair(Transform parent)
        {
            Image crosshair = CreateImage("Crosshair", parent, null, new Vector2(4, 4));
            AnchorTo(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 4));
            crosshair.color = Color.white;
        }

        private static void BuildInteractionPrompt(Transform parent)
        {
            RectTransform promptRoot = CreateElement("InteractionPrompt", parent);
            AnchorTo(promptRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 120), new Vector2(300, 40));

            CanvasGroup cg = promptRoot.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Key icon
            Image keyIcon = CreateImage("KeyIcon", promptRoot, _ingameIconSlot, new Vector2(32, 32));
            AnchorTo(keyIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(80, 0), new Vector2(32, 32));

            // "E" label inside key icon
            TextMeshProUGUI keyLabel = CreateText("KeyLabel", keyIcon.rectTransform, "E", 16f,
                TextAlignmentOptions.Center);
            AnchorTo(keyLabel.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            keyLabel.color = Color.white;

            // Prompt text
            TextMeshProUGUI promptText = CreateText("PromptText", promptRoot, "Interact", 16f,
                TextAlignmentOptions.Left);
            AnchorTo(promptText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(-120, 30));
        }

        private static void BuildTargetInfo(Transform parent)
        {
            RectTransform targetRoot = CreateElement("TargetInfo", parent);
            AnchorTo(targetRoot, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-20, 50), new Vector2(220, 80));

            CanvasGroup cg = targetRoot.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Background
            Image bg = CreateImage("Background", targetRoot, _descBox01, Vector2.zero);
            RectTransform bgRT = bg.rectTransform;
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            // Name
            TextMeshProUGUI nameText = CreateText("TargetName", targetRoot, "Enemy Name", 16f,
                TextAlignmentOptions.Center);
            AnchorTo(nameText.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(-10, 22));
            nameText.color = new Color(1f, 0.4f, 0.4f);

            // Rank
            TextMeshProUGUI rankText = CreateText("TargetRank", targetRoot, "Rank F", 13f,
                TextAlignmentOptions.Center);
            AnchorTo(rankText.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(-10, 18));
            rankText.color = new Color(0.8f, 0.7f, 0.5f);

            // Health bar
            Slider healthSlider = CreateSlider("TargetHealthSlider", targetRoot, _progressBarBg, _progressBarTop,
                new Vector2(180, 14), new Color(0.8f, 0.2f, 0.2f));
            AnchorTo(healthSlider.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(180, 14));
        }

        private static void BuildThreatVignette(Transform parent)
        {
            Image vignette = CreateImage("ThreatVignette", parent, null, Vector2.zero);
            RectTransform rt = vignette.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            vignette.color = new Color(0.6f, 0f, 0f, 0f);
            vignette.raycastTarget = false;
            vignette.gameObject.SetActive(false);
        }

        #endregion

        #region Build Inventory Panel

        private static void BuildInventoryPanel()
        {
            GameObject canvas = CreateCanvas("InventoryPanel", 10);

            // Dark overlay background
            Image overlay = CreateImage("Overlay", canvas.transform, null, Vector2.zero);
            RectTransform overlayRT = overlay.rectTransform;
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;
            overlay.color = new Color(0f, 0f, 0f, 0.6f);

            // Main content panel
            RectTransform contentPanel = CreateElement("ContentPanel", canvas.transform);
            AnchorTo(contentPanel, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.95f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Panel background
            Image contentBg = contentPanel.gameObject.AddComponent<Image>();
            contentBg.sprite = _bgLayer01;
            contentBg.type = Image.Type.Sliced;
            contentBg.color = new Color(0.15f, 0.12f, 0.18f, 0.95f);

            // Header
            RectTransform header = CreateElement("Header", contentPanel);
            AnchorTo(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                Vector2.zero, new Vector2(0, 50));

            Image headerBg = header.gameObject.AddComponent<Image>();
            headerBg.sprite = _panelNameHeader;
            if (headerBg.sprite != null) headerBg.type = Image.Type.Sliced;
            headerBg.color = new Color(0.2f, 0.15f, 0.25f, 0.9f);

            TextMeshProUGUI titleText = CreateText("Title", header, "INVENTORY", 24f,
                TextAlignmentOptions.Center);
            AnchorTo(titleText.rectTransform, new Vector2(0, 0), new Vector2(0.5f, 1),
                new Vector2(0.25f, 0.5f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI goldText = CreateText("GoldTotal", header, "0g", 18f,
                TextAlignmentOptions.Right);
            AnchorTo(goldText.rectTransform, new Vector2(0.7f, 0), new Vector2(1, 1),
                new Vector2(1, 0.5f), new Vector2(-15, 0), Vector2.zero);
            goldText.color = new Color(1f, 0.85f, 0.3f);

            TextMeshProUGUI itemCountText = CreateText("ItemCount", header, "0 items", 14f,
                TextAlignmentOptions.Right);
            AnchorTo(itemCountText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.7f, 1),
                new Vector2(1, 0.5f), new Vector2(0, 0), Vector2.zero);
            itemCountText.color = new Color(0.7f, 0.7f, 0.7f);

            // --- Left Panel: Character Info (40%) ---
            BuildCharacterPanel(contentPanel);

            // --- Right Panel: Inventory Grid (60%) ---
            BuildInventoryGrid(contentPanel);

            // --- Floating Tooltip ---
            BuildItemTooltip(canvas.transform);

            // --- Item Detail Panel (right side, shown when item is selected) ---
            BuildItemDetailPanel(contentPanel);

            // Add InventoryPanelUI component
            var invPanelUI = canvas.AddComponent<UI.InventoryPanelUI>();
            SerializedObject so = new SerializedObject(invPanelUI);
            so.FindProperty("panelRoot").objectReferenceValue = canvas;
            so.FindProperty("panelCanvasGroup").objectReferenceValue = canvas.GetComponent<CanvasGroup>() ?? canvas.AddComponent<CanvasGroup>();
            so.FindProperty("overlayBackground").objectReferenceValue = overlay;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("goldTotalText").objectReferenceValue = goldText;
            so.FindProperty("itemCountText").objectReferenceValue = itemCountText;
            so.FindProperty("gridBoundary").objectReferenceValue = contentPanel.Find("InventoryGridPanel/GridContainer")?.GetComponent<RectTransform>();
            so.ApplyModifiedPropertiesWithoutUndo();

            // Wire CharacterPanelUI
            var charPanelUI = contentPanel.Find("CharacterPanel")?.GetComponent<UI.CharacterPanelUI>();
            if (charPanelUI != null)
            {
                so.FindProperty("characterPanel").objectReferenceValue = charPanelUI;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire InventoryUI
            var invUI = contentPanel.Find("InventoryGridPanel")?.GetComponent<UI.InventoryUI>();
            if (invUI != null)
            {
                so.FindProperty("inventoryUI").objectReferenceValue = invUI;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire ItemDetailPanelUI
            var detailPanel = contentPanel.Find("ItemDetailPanel")?.GetComponent<UI.ItemDetailPanelUI>();
            if (detailPanel != null)
            {
                so.FindProperty("itemDetailPanel").objectReferenceValue = detailPanel;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SavePrefab(canvas, "InventoryPanel");
        }

        private static void BuildCharacterPanel(Transform contentPanel)
        {
            RectTransform charPanel = CreateElement("CharacterPanel", contentPanel);
            AnchorTo(charPanel, new Vector2(0, 0), new Vector2(0.4f, 1),
                new Vector2(0, 0.5f), new Vector2(10, -55), new Vector2(-10, -65));

            // Background
            Image charBg = charPanel.gameObject.AddComponent<Image>();
            charBg.sprite = _bgLayer02;
            if (charBg.sprite != null) charBg.type = Image.Type.Sliced;
            charBg.color = new Color(0.12f, 0.1f, 0.15f, 0.8f);

            // Character portrait area
            Image portrait = CreateImage("CharacterFrame", charPanel, _characterFrame,
                new Vector2(120, 120));
            AnchorTo(portrait.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(120, 120));

            Image portraitImage = CreateImage("PortraitImage", portrait.rectTransform, null,
                new Vector2(100, 100));
            AnchorTo(portraitImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
            portraitImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            // --- Stats Section ---
            RectTransform statsSection = CreateElement("StatsSection", charPanel);
            AnchorTo(statsSection, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(-20, 120));

            // Strength Row
            BuildStatRow("Strength", statsSection, 0, "STR", new Color(0.9f, 0.3f, 0.2f));
            // Endurance Row
            BuildStatRow("Endurance", statsSection, 1, "END", new Color(0.2f, 0.8f, 0.3f));
            // Perception Row
            BuildStatRow("Perception", statsSection, 2, "PER", new Color(0.3f, 0.5f, 1f));

            // Divider
            Image divider = CreateImage("Divider", charPanel, _splittDivider, new Vector2(0, 3));
            AnchorTo(divider.rectTransform, new Vector2(0.05f, 1), new Vector2(0.95f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -282), new Vector2(0, 3));

            // --- Weight Section ---
            RectTransform weightSection = CreateElement("WeightSection", charPanel);
            AnchorTo(weightSection, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, -295), new Vector2(-20, 80));

            TextMeshProUGUI weightLabel = CreateText("WeightLabel", weightSection, "WEIGHT", 14f,
                TextAlignmentOptions.Left);
            AnchorTo(weightLabel.rectTransform, new Vector2(0, 1), new Vector2(0.4f, 1),
                new Vector2(0, 1), new Vector2(10, 0), new Vector2(0, 20));
            weightLabel.color = new Color(0.7f, 0.65f, 0.55f);

            TextMeshProUGUI weightValue = CreateText("WeightValue", weightSection, "0.0 / 40.0 kg", 14f,
                TextAlignmentOptions.Right);
            AnchorTo(weightValue.rectTransform, new Vector2(0.4f, 1), new Vector2(1, 1),
                new Vector2(1, 1), new Vector2(-10, 0), new Vector2(0, 20));

            Slider weightBar = CreateSlider("WeightBar", weightSection, _statsSliderBg, _statsSliderTop,
                new Vector2(0, 16), new Color(0.3f, 0.8f, 0.3f));
            AnchorTo(weightBar.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(-20, 16));
            weightBar.value = 0f;

            TextMeshProUGUI tierText = CreateText("EncumbranceTier", weightSection, "Light", 16f,
                TextAlignmentOptions.Center);
            AnchorTo(tierText.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(0, 22));
            tierText.color = new Color(0.3f, 0.8f, 0.3f);

            // --- Backpack Section ---
            RectTransform bpSection = CreateElement("BackpackSection", charPanel);
            AnchorTo(bpSection, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(-20, 50));

            TextMeshProUGUI bpLabel = CreateText("BackpackLabel", bpSection, "BACKPACK", 12f,
                TextAlignmentOptions.Left);
            AnchorTo(bpLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 1),
                new Vector2(0, 1), new Vector2(10, 0), new Vector2(0, 18));
            bpLabel.color = new Color(0.6f, 0.55f, 0.5f);

            TextMeshProUGUI bpName = CreateText("BackpackName", bpSection, "Starter Pack", 14f,
                TextAlignmentOptions.Left);
            AnchorTo(bpName.rectTransform, new Vector2(0, 0), new Vector2(0.6f, 0.5f),
                new Vector2(0, 0), new Vector2(10, 0), new Vector2(0, 20));

            TextMeshProUGUI bpSize = CreateText("BackpackSize", bpSection, "6x4", 18f,
                TextAlignmentOptions.Right);
            AnchorTo(bpSize.rectTransform, new Vector2(0.6f, 0), new Vector2(1, 1),
                new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(0, 0));
            bpSize.color = new Color(0.9f, 0.8f, 0.5f);

            // Add CharacterPanelUI component
            var charPanelUI = charPanel.gameObject.AddComponent<UI.CharacterPanelUI>();
            SerializedObject so = new SerializedObject(charPanelUI);

            so.FindProperty("characterFrame").objectReferenceValue = portrait;
            so.FindProperty("characterPortrait").objectReferenceValue = portraitImage;

            // Wire stat rows
            WireStatRow(so, "strength", statsSection.Find("StrengthRow"));
            WireStatRow(so, "endurance", statsSection.Find("EnduranceRow"));
            WireStatRow(so, "perception", statsSection.Find("PerceptionRow"));

            // Weight
            so.FindProperty("weightValueText").objectReferenceValue = weightValue;
            so.FindProperty("weightBar").objectReferenceValue = weightBar;
            so.FindProperty("weightBarFill").objectReferenceValue = weightBar.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            so.FindProperty("encumbranceTierText").objectReferenceValue = tierText;

            // Backpack
            so.FindProperty("backpackSizeText").objectReferenceValue = bpSize;
            so.FindProperty("backpackNameText").objectReferenceValue = bpName;

            // Weight gradient
            SerializedProperty gradientProp = so.FindProperty("weightGradient");
            if (gradientProp != null)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.3f, 0.8f, 0.3f), 0f),
                        new GradientColorKey(new Color(0.9f, 0.9f, 0.2f), 0.5f),
                        new GradientColorKey(new Color(0.9f, 0.3f, 0.1f), 0.8f),
                        new GradientColorKey(new Color(0.8f, 0.1f, 0.1f), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
                gradientProp.gradientValue = gradient;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildStatRow(string statName, Transform parent, int index,
            string prefix, Color color)
        {
            float yOffset = -index * 36f;

            RectTransform row = CreateElement(statName + "Row", parent);
            AnchorTo(row, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, yOffset), new Vector2(0, 32));

            // Icon placeholder
            Image icon = CreateImage("Icon", row, _skillSlot, new Vector2(28, 28));
            AnchorTo(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(28, 28));
            icon.color = color;

            // Level text
            TextMeshProUGUI levelText = CreateText("LevelText", row, $"{prefix} 1", 15f,
                TextAlignmentOptions.Left);
            AnchorTo(levelText.rectTransform, new Vector2(0, 0), new Vector2(0.35f, 1),
                new Vector2(0, 0.5f), new Vector2(44, 0), new Vector2(0, 0));
            levelText.color = color;

            // XP bar
            Slider xpBar = CreateSlider("XPBar", row, _expBarBg, _expBarTop,
                new Vector2(0, 12), color);
            AnchorTo(xpBar.GetComponent<RectTransform>(), new Vector2(0.38f, 0.5f), new Vector2(1, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(-10, 12));
            xpBar.value = 0f;
        }

        private static void WireStatRow(SerializedObject so, string statPrefix, Transform row)
        {
            if (row == null) return;

            so.FindProperty(statPrefix + "Icon").objectReferenceValue = row.Find("Icon")?.GetComponent<Image>();
            so.FindProperty(statPrefix + "LevelText").objectReferenceValue = row.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty(statPrefix + "XPBar").objectReferenceValue = row.Find("XPBar")?.GetComponent<Slider>();
            so.FindProperty(statPrefix + "XPFill").objectReferenceValue = row.Find("XPBar/Fill Area/Fill")?.GetComponent<Image>();
        }

        private static void BuildInventoryGrid(Transform contentPanel)
        {
            RectTransform gridPanel = CreateElement("InventoryGridPanel", contentPanel);
            AnchorTo(gridPanel, new Vector2(0.4f, 0), new Vector2(1, 1),
                new Vector2(1, 0.5f), new Vector2(-10, -55), new Vector2(-10, -65));

            // Background
            Image gridBg = gridPanel.gameObject.AddComponent<Image>();
            gridBg.sprite = _bgLayer02;
            if (gridBg.sprite != null) gridBg.type = Image.Type.Sliced;
            gridBg.color = new Color(0.1f, 0.08f, 0.12f, 0.85f);

            // Grid container sizing: 6x4 grid * 50px cells + 2px spacing + 6px padding on each side
            // totalCells = 6*(50+2)-2 = 310 width, 4*(50+2)-2 = 206 height
            // with padding: 310+12 = 322, 206+12 = 218
            float cellSize = 50f;
            float cellSpacing = 2f;
            float padding = 6f;
            int defaultGridW = 6;
            int defaultGridH = 4;
            float totalCellsW = defaultGridW * (cellSize + cellSpacing) - cellSpacing;
            float totalCellsH = defaultGridH * (cellSize + cellSpacing) - cellSpacing;
            float containerW = totalCellsW + padding * 2f;
            float containerH = totalCellsH + padding * 2f;

            // Grid Container (cells go here) - includes padding, gets a dark background
            RectTransform gridContainer = CreateElement("GridContainer", gridPanel);
            AnchorTo(gridContainer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(containerW, containerH));

            // Grid container background (dark panel behind the cells)
            Image gridContainerBg = gridContainer.gameObject.AddComponent<Image>();
            gridContainerBg.color = new Color(0.06f, 0.07f, 0.10f, 0.95f);
            gridContainerBg.raycastTarget = true;

            // Item Container (items rendered on top of grid, same size as grid container)
            RectTransform itemContainer = CreateElement("ItemContainer", gridPanel);
            AnchorTo(itemContainer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(containerW, containerH));

            // Grid Cell Prefab (template, disabled) - uses item_slot sprite
            GameObject cellPrefab = new GameObject("GridCellPrefab");
            cellPrefab.transform.SetParent(gridPanel, false);
            RectTransform cellPrefabRT = cellPrefab.AddComponent<RectTransform>();
            cellPrefabRT.sizeDelta = new Vector2(cellSize, cellSize);
            Image cellImage = cellPrefab.AddComponent<Image>();
            cellImage.sprite = _itemSlot;
            if (cellImage.sprite != null) cellImage.type = Image.Type.Sliced;
            cellImage.color = new Color(0.12f, 0.14f, 0.18f, 0.95f); // emptyCellColor
            cellImage.raycastTarget = true;
            // Add Outline for border (InventoryGridCell.Initialize also adds one, but prefab is cleaner)
            Outline cellOutline = cellPrefab.AddComponent<Outline>();
            cellOutline.effectColor = new Color(0.3f, 0.35f, 0.42f, 0.6f);
            cellOutline.effectDistance = new Vector2(1f, -1f);
            cellPrefab.SetActive(false);

            // Inventory Item Prefab (template, disabled)
            GameObject itemPrefab = new GameObject("InventoryItemPrefab");
            itemPrefab.transform.SetParent(gridPanel, false);
            RectTransform itemPrefabRT = itemPrefab.AddComponent<RectTransform>();
            itemPrefabRT.sizeDelta = new Vector2(cellSize, cellSize);
            itemPrefabRT.anchorMin = new Vector2(0, 1);
            itemPrefabRT.anchorMax = new Vector2(0, 1);
            itemPrefabRT.pivot = new Vector2(0, 1);

            // Item background (rarity-tinted)
            Image itemBg = CreateImage("Background", itemPrefab.transform, _itemSelectedSlot, Vector2.zero);
            RectTransform itemBgRT = itemBg.rectTransform;
            itemBgRT.anchorMin = Vector2.zero;
            itemBgRT.anchorMax = Vector2.one;
            itemBgRT.offsetMin = Vector2.zero;
            itemBgRT.offsetMax = Vector2.zero;
            itemBg.color = new Color(0.15f, 0.14f, 0.18f, 0.75f);

            // Item icon (centered with padding, aspect preserved)
            Image itemIcon = CreateImage("Icon", itemPrefab.transform, null, Vector2.zero);
            RectTransform itemIconRT = itemIcon.rectTransform;
            itemIconRT.anchorMin = Vector2.zero;
            itemIconRT.anchorMax = Vector2.one;
            itemIconRT.offsetMin = new Vector2(4, 4); // icon padding
            itemIconRT.offsetMax = new Vector2(-4, -4);
            itemIcon.preserveAspect = true;

            // Item border outline
            Outline itemOutline = itemPrefab.AddComponent<Outline>();
            itemOutline.effectColor = new Color(0.4f, 0.42f, 0.48f, 0.7f);
            itemOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Wire InventoryItemUI component
            var itemUI = itemPrefab.AddComponent<UI.InventoryItemUI>();
            SerializedObject itemUISO = new SerializedObject(itemUI);
            itemUISO.FindProperty("iconImage").objectReferenceValue = itemIcon;
            itemUISO.FindProperty("backgroundImage").objectReferenceValue = itemBg;
            itemUISO.FindProperty("rectTransform").objectReferenceValue = itemPrefabRT;
            itemUISO.FindProperty("borderOutline").objectReferenceValue = itemOutline;
            itemUISO.ApplyModifiedPropertiesWithoutUndo();
            itemPrefab.SetActive(false);

            // Add InventoryUI component
            var invUI = gridPanel.gameObject.AddComponent<UI.InventoryUI>();
            SerializedObject invUISO = new SerializedObject(invUI);
            invUISO.FindProperty("gridContainer").objectReferenceValue = gridContainer;
            invUISO.FindProperty("itemContainer").objectReferenceValue = itemContainer;
            invUISO.FindProperty("gridCellPrefab").objectReferenceValue = cellPrefab;
            invUISO.FindProperty("inventoryItemPrefab").objectReferenceValue = itemPrefab;
            invUISO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildItemTooltip(Transform parent)
        {
            RectTransform tooltipRoot = CreateElement("ItemTooltip", parent);
            AnchorTo(tooltipRoot, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(100, -100), new Vector2(240, 200));

            CanvasGroup cg = tooltipRoot.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            // Background
            Image bg = tooltipRoot.gameObject.AddComponent<Image>();
            bg.sprite = _descBox01;
            if (bg.sprite != null) bg.type = Image.Type.Sliced;
            bg.color = new Color(0.1f, 0.08f, 0.12f, 0.95f);
            bg.raycastTarget = false;

            // Rarity bar (thin colored bar at top)
            Image rarityBar = CreateImage("RarityBar", tooltipRoot, null, new Vector2(0, 3));
            AnchorTo(rarityBar.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(-16, 3));
            rarityBar.color = Color.white;
            rarityBar.raycastTarget = false;

            // Content layout
            float yPos = -14f;

            // Item name
            TextMeshProUGUI nameText = CreateText("ItemName", tooltipRoot, "Item Name", 18f,
                TextAlignmentOptions.Left);
            AnchorTo(nameText.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 1), new Vector2(10, yPos), new Vector2(-20, 24));
            nameText.raycastTarget = false;

            yPos -= 28f;

            // Category
            TextMeshProUGUI categoryText = CreateText("ItemCategory", tooltipRoot, "Scrap", 12f,
                TextAlignmentOptions.Left);
            AnchorTo(categoryText.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 1), new Vector2(10, yPos), new Vector2(-20, 16));
            categoryText.color = new Color(0.6f, 0.6f, 0.6f);
            categoryText.raycastTarget = false;

            yPos -= 22f;

            // Description
            TextMeshProUGUI descText = CreateText("ItemDescription", tooltipRoot, "Item description goes here.",
                13f, TextAlignmentOptions.TopLeft);
            AnchorTo(descText.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 1), new Vector2(10, yPos), new Vector2(-20, 50));
            descText.color = new Color(0.75f, 0.72f, 0.65f);
            descText.enableWordWrapping = true;
            descText.overflowMode = TextOverflowModes.Truncate;
            descText.raycastTarget = false;

            yPos -= 58f;

            // Weight and Value row
            TextMeshProUGUI weightText = CreateText("ItemWeight", tooltipRoot, "1.0 kg", 14f,
                TextAlignmentOptions.Left);
            AnchorTo(weightText.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1),
                new Vector2(0, 1), new Vector2(10, yPos), new Vector2(0, 20));
            weightText.color = new Color(0.7f, 0.7f, 0.7f);
            weightText.raycastTarget = false;

            TextMeshProUGUI valueText = CreateText("ItemValue", tooltipRoot, "10g", 14f,
                TextAlignmentOptions.Right);
            AnchorTo(valueText.rectTransform, new Vector2(0.5f, 1), new Vector2(1, 1),
                new Vector2(1, 1), new Vector2(-10, yPos), new Vector2(0, 20));
            valueText.color = new Color(1f, 0.85f, 0.3f);
            valueText.raycastTarget = false;

            yPos -= 26f;

            // Shape preview container
            RectTransform shapeContainer = CreateElement("ShapeContainer", tooltipRoot);
            AnchorTo(shapeContainer, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(10, yPos), new Vector2(60, 60));

            // Shape cell prefab (tiny square)
            GameObject shapeCellPrefab = new GameObject("ShapeCellPrefab");
            shapeCellPrefab.transform.SetParent(tooltipRoot, false);
            Image shapeCellImg = shapeCellPrefab.AddComponent<Image>();
            shapeCellImg.color = Color.white;
            shapeCellImg.raycastTarget = false;
            shapeCellPrefab.SetActive(false);

            // Add ItemTooltipUI
            var tooltip = tooltipRoot.gameObject.AddComponent<UI.ItemTooltipUI>();
            SerializedObject so = new SerializedObject(tooltip);
            so.FindProperty("tooltipRect").objectReferenceValue = tooltipRoot;
            so.FindProperty("canvasGroup").objectReferenceValue = cg;
            so.FindProperty("itemNameText").objectReferenceValue = nameText;
            so.FindProperty("itemDescriptionText").objectReferenceValue = descText;
            so.FindProperty("itemWeightText").objectReferenceValue = weightText;
            so.FindProperty("itemValueText").objectReferenceValue = valueText;
            so.FindProperty("itemCategoryText").objectReferenceValue = categoryText;
            so.FindProperty("rarityBar").objectReferenceValue = rarityBar;
            so.FindProperty("shapeContainer").objectReferenceValue = shapeContainer;
            so.FindProperty("shapeCellPrefab").objectReferenceValue = shapeCellPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Start disabled
            tooltipRoot.gameObject.SetActive(true); // Active but alpha=0
        }

        private static void BuildItemDetailPanel(Transform contentPanel)
        {
            // The detail panel sits to the right of the inventory grid panel.
            // It's hidden by default and shown when an item is selected.
            RectTransform detailRoot = CreateElement("ItemDetailPanel", contentPanel);
            // Anchor to right edge, vertically centered
            AnchorTo(detailRoot, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(220, 400));

            // Background
            Image detailBg = detailRoot.gameObject.AddComponent<Image>();
            detailBg.sprite = _bgLayer02;
            if (detailBg.sprite != null) detailBg.type = Image.Type.Sliced;
            detailBg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
            detailBg.raycastTarget = true;

            // Outline border
            Outline detailOutline = detailRoot.gameObject.AddComponent<Outline>();
            detailOutline.effectColor = new Color(0.3f, 0.35f, 0.42f, 0.6f);
            detailOutline.effectDistance = new Vector2(1f, -1f);

            // CanvasGroup for show/hide
            CanvasGroup detailCG = detailRoot.gameObject.AddComponent<CanvasGroup>();
            detailCG.alpha = 0f;
            detailCG.interactable = false;
            detailCG.blocksRaycasts = false;

            // Add ItemDetailPanelUI component
            // The component builds its own child UI programmatically in Start(),
            // so we just need to attach it and wire the InventoryUI reference.
            var detailPanelUI = detailRoot.gameObject.AddComponent<UI.ItemDetailPanelUI>();
            // InventoryUI reference will be wired by InventoryPanelUI.EnsureDetailPanel() at runtime,
            // or explicitly wired below via the parent InventoryPanelUI SerializedObject.

            detailRoot.gameObject.SetActive(false); // Start hidden
        }

        #endregion

        #region Build Menu Canvas

        private static void BuildMenuCanvas()
        {
            GameObject canvas = CreateCanvas("MenuCanvas", 100);

            // Main Menu Panel
            BuildMainMenuPanel(canvas.transform);

            // Pause Menu Panel
            BuildPauseMenuPanel(canvas.transform);

            // Settings Panel
            BuildSettingsPanel(canvas.transform);

            // Death Panel
            BuildDeathPanel(canvas.transform);

            // Extraction Panel
            BuildExtractionPanel(canvas.transform);

            // Loading Panel
            BuildLoadingPanel(canvas.transform);

            // Add MenuManager
            var menuManager = canvas.AddComponent<UI.MenuManager>();
            SerializedObject so = new SerializedObject(menuManager);

            // Panels
            so.FindProperty("mainMenuPanel").objectReferenceValue = canvas.transform.Find("MainMenuPanel")?.gameObject;
            so.FindProperty("pauseMenuPanel").objectReferenceValue = canvas.transform.Find("PauseMenuPanel")?.gameObject;
            so.FindProperty("settingsPanel").objectReferenceValue = canvas.transform.Find("SettingsPanel")?.gameObject;
            so.FindProperty("deathPanel").objectReferenceValue = canvas.transform.Find("DeathPanel")?.gameObject;
            so.FindProperty("extractionPanel").objectReferenceValue = canvas.transform.Find("ExtractionPanel")?.gameObject;
            so.FindProperty("loadingPanel").objectReferenceValue = canvas.transform.Find("LoadingPanel")?.gameObject;

            // Main Menu buttons
            so.FindProperty("newGameButton").objectReferenceValue = canvas.transform.Find("MainMenuPanel/NewGameButton")?.GetComponent<Button>();
            so.FindProperty("continueButton").objectReferenceValue = canvas.transform.Find("MainMenuPanel/ContinueButton")?.GetComponent<Button>();
            so.FindProperty("settingsButton").objectReferenceValue = canvas.transform.Find("MainMenuPanel/SettingsButton")?.GetComponent<Button>();
            so.FindProperty("quitButton").objectReferenceValue = canvas.transform.Find("MainMenuPanel/QuitButton")?.GetComponent<Button>();

            // Pause menu buttons
            so.FindProperty("resumeButton").objectReferenceValue = canvas.transform.Find("PauseMenuPanel/ResumeButton")?.GetComponent<Button>();
            so.FindProperty("pauseSettingsButton").objectReferenceValue = canvas.transform.Find("PauseMenuPanel/SettingsButton")?.GetComponent<Button>();
            so.FindProperty("saveQuitButton").objectReferenceValue = canvas.transform.Find("PauseMenuPanel/SaveQuitButton")?.GetComponent<Button>();

            // Death
            so.FindProperty("deathMessageText").objectReferenceValue = canvas.transform.Find("DeathPanel/DeathMessage")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("returnToVillageButton").objectReferenceValue = canvas.transform.Find("DeathPanel/ReturnButton")?.GetComponent<Button>();

            // Extraction
            so.FindProperty("goldEarnedText").objectReferenceValue = canvas.transform.Find("ExtractionPanel/GoldEarned")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("itemsCollectedText").objectReferenceValue = canvas.transform.Find("ExtractionPanel/ItemsCollected")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("xpGainedText").objectReferenceValue = canvas.transform.Find("ExtractionPanel/XPGained")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("extractionContinueButton").objectReferenceValue = canvas.transform.Find("ExtractionPanel/ContinueButton")?.GetComponent<Button>();

            // Settings
            so.FindProperty("mouseSensitivitySlider").objectReferenceValue = canvas.transform.Find("SettingsPanel/MouseSensitivity")?.GetComponent<Slider>();
            so.FindProperty("masterVolumeSlider").objectReferenceValue = canvas.transform.Find("SettingsPanel/MasterVolume")?.GetComponent<Slider>();
            so.FindProperty("settingsBackButton").objectReferenceValue = canvas.transform.Find("SettingsPanel/BackButton")?.GetComponent<Button>();

            // Loading
            so.FindProperty("loadingProgressBar").objectReferenceValue = canvas.transform.Find("LoadingPanel/ProgressBar")?.GetComponent<Slider>();
            so.FindProperty("loadingText").objectReferenceValue = canvas.transform.Find("LoadingPanel/LoadingText")?.GetComponent<TextMeshProUGUI>();

            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(canvas, "MenuCanvas");
        }

        private static void BuildMainMenuPanel(Transform parent)
        {
            GameObject panel = CreatePanel("MainMenuPanel", parent);
            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.03f, 0.08f, 0.95f);

            // Title
            TextMeshProUGUI title = CreateText("Title", panel.transform, "DUNGEON DREDGE", 48f,
                TextAlignmentOptions.Center);
            AnchorTo(title.rectTransform, new Vector2(0, 0.65f), new Vector2(1, 0.85f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            title.color = new Color(0.9f, 0.8f, 0.5f);

            // Subtitle
            TextMeshProUGUI subtitle = CreateText("Subtitle", panel.transform, "Scavenge. Survive. Extract.", 18f,
                TextAlignmentOptions.Center);
            AnchorTo(subtitle.rectTransform, new Vector2(0.2f, 0.58f), new Vector2(0.8f, 0.65f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            subtitle.color = new Color(0.6f, 0.55f, 0.5f);

            // Buttons
            float btnY = 0.48f;
            float btnSpacing = 0.08f;
            Vector2 btnSize = new Vector2(280, 50);

            CreateMenuButton("NewGameButton", panel.transform, "New Game", btnY, btnSize);
            CreateMenuButton("ContinueButton", panel.transform, "Continue", btnY - btnSpacing, btnSize);
            CreateMenuButton("SettingsButton", panel.transform, "Settings", btnY - btnSpacing * 2, btnSize);
            CreateMenuButton("QuitButton", panel.transform, "Quit", btnY - btnSpacing * 3, btnSize);

            panel.SetActive(false);
        }

        private static void BuildPauseMenuPanel(Transform parent)
        {
            GameObject panel = CreatePanel("PauseMenuPanel", parent);
            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.03f, 0.08f, 0.85f);

            // Title
            TextMeshProUGUI title = CreateText("Title", panel.transform, "PAUSED", 36f,
                TextAlignmentOptions.Center);
            AnchorTo(title.rectTransform, new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.8f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            title.color = new Color(0.9f, 0.85f, 0.7f);

            Vector2 btnSize = new Vector2(250, 45);
            CreateMenuButton("ResumeButton", panel.transform, "Resume", 0.55f, btnSize);
            CreateMenuButton("SettingsButton", panel.transform, "Settings", 0.45f, btnSize);
            CreateMenuButton("SaveQuitButton", panel.transform, "Save & Quit", 0.35f, btnSize);

            panel.SetActive(false);
        }

        private static void BuildSettingsPanel(Transform parent)
        {
            GameObject panel = CreatePanel("SettingsPanel", parent);
            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.AddComponent<Image>();
            panelBg.sprite = _popUp;
            if (panelBg.sprite != null) panelBg.type = Image.Type.Sliced;
            panelBg.color = new Color(0.12f, 0.1f, 0.15f, 0.95f);

            // Constrain to center
            RectTransform rt = panel.GetComponent<RectTransform>();
            AnchorTo(rt, new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.85f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Title
            TextMeshProUGUI title = CreateText("Title", panel.transform, "SETTINGS", 28f,
                TextAlignmentOptions.Center);
            AnchorTo(title.rectTransform, new Vector2(0, 0.88f), new Vector2(1, 0.98f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Mouse Sensitivity
            BuildSettingsSlider("MouseSensitivity", panel.transform, "Mouse Sensitivity", 0.72f);

            // Master Volume
            BuildSettingsSlider("MasterVolume", panel.transform, "Master Volume", 0.55f);

            // Back button
            Button backBtn = CreateButton("BackButton", panel.transform, _button02, "Back",
                new Vector2(160, 40));
            AnchorTo(backBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f),
                new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(160, 40));

            panel.SetActive(false);
        }

        private static void BuildSettingsSlider(string name, Transform parent, string label, float yAnchor)
        {
            // Label
            TextMeshProUGUI lbl = CreateText(name + "Label", parent, label, 16f, TextAlignmentOptions.Left);
            AnchorTo(lbl.rectTransform, new Vector2(0.1f, yAnchor), new Vector2(0.4f, yAnchor + 0.05f),
                new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);

            // Slider
            Slider slider = CreateSlider(name, parent, _statsSliderBg, _statsSliderTop,
                new Vector2(0, 20), new Color(0.5f, 0.7f, 0.9f));
            AnchorTo(slider.GetComponent<RectTransform>(), new Vector2(0.42f, yAnchor),
                new Vector2(0.88f, yAnchor + 0.05f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            slider.value = 0.5f;
        }

        private static void BuildDeathPanel(Transform parent)
        {
            GameObject panel = CreatePanel("DeathPanel", parent);
            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.15f, 0.02f, 0.02f, 0.9f);

            TextMeshProUGUI deathMsg = CreateText("DeathMessage", panel.transform,
                "You died. All items lost.", 28f, TextAlignmentOptions.Center);
            AnchorTo(deathMsg.rectTransform, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.65f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            deathMsg.color = new Color(0.9f, 0.3f, 0.3f);

            Button returnBtn = CreateButton("ReturnButton", panel.transform, _button01,
                "Return to Village", new Vector2(250, 50));
            AnchorTo(returnBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 50));

            panel.SetActive(false);
        }

        private static void BuildExtractionPanel(Transform parent)
        {
            GameObject panel = CreatePanel("ExtractionPanel", parent);
            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.02f, 0.08f, 0.02f, 0.9f);

            TextMeshProUGUI headerText = CreateText("Header", panel.transform,
                "EXTRACTION SUCCESSFUL", 32f, TextAlignmentOptions.Center);
            AnchorTo(headerText.rectTransform, new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.82f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            headerText.color = new Color(0.3f, 0.9f, 0.3f);

            TextMeshProUGUI goldText = CreateText("GoldEarned", panel.transform, "Gold: 0", 20f,
                TextAlignmentOptions.Center);
            AnchorTo(goldText.rectTransform, new Vector2(0.2f, 0.58f), new Vector2(0.8f, 0.66f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            goldText.color = new Color(1f, 0.85f, 0.3f);

            TextMeshProUGUI itemsText = CreateText("ItemsCollected", panel.transform, "Items: 0", 18f,
                TextAlignmentOptions.Center);
            AnchorTo(itemsText.rectTransform, new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.57f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI xpText = CreateText("XPGained", panel.transform, "XP: 0", 18f,
                TextAlignmentOptions.Center);
            AnchorTo(xpText.rectTransform, new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.49f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Button continueBtn = CreateButton("ContinueButton", panel.transform, _button01,
                "Continue", new Vector2(220, 50));
            AnchorTo(continueBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 50));

            panel.SetActive(false);
        }

        private static void BuildLoadingPanel(Transform parent)
        {
            GameObject panel = CreatePanel("LoadingPanel", parent);
            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.03f, 0.02f, 0.05f, 1f);

            TextMeshProUGUI loadingText = CreateText("LoadingText", panel.transform,
                "Loading...", 24f, TextAlignmentOptions.Center);
            AnchorTo(loadingText.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0.7f, 0.6f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Slider progressBar = CreateSlider("ProgressBar", panel.transform, _progressBarBg, _progressBarTop,
                new Vector2(400, 20), new Color(0.4f, 0.7f, 0.9f));
            AnchorTo(progressBar.GetComponent<RectTransform>(), new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 20));
            progressBar.value = 0f;

            panel.SetActive(false);
        }

        private static void CreateMenuButton(string name, Transform parent, string label, float yAnchor,
            Vector2 size)
        {
            Button btn = CreateButton(name, parent, _button01, label, size);
            AnchorTo(btn.GetComponent<RectTransform>(), new Vector2(0.5f, yAnchor), new Vector2(0.5f, yAnchor),
                new Vector2(0.5f, 0.5f), Vector2.zero, size);
        }

        #endregion
    }
}

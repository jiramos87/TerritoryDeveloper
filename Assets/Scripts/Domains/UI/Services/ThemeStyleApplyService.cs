using System;
using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI.Services
{
    /// <summary>
    /// Style application helpers — Apply*, Style*, Tint*, CreateDividerStripe, EnsureTaxPanel*.
    /// Pure instance service; no MonoBehaviour, no UiTheme reference.
    /// Split from ThemeService (Stage 7.4 Tier-E atomization).
    /// </summary>
    public class ThemeStyleApplyService
    {
        // ─── Text style apply ────────────────────────────────────────────────────
        public void ApplyHeroStatRow(Text valueText, int fontSizeCaption, Color textSecondary, int fontSizeDisplay, Color textPrimary)
        {
            if (valueText == null) return;
            StyleSiblingLabelTexts(valueText.transform, fontSizeCaption, textSecondary);
            valueText.fontSize = fontSizeDisplay;
            valueText.color = textPrimary;
            valueText.supportRichText = true;
        }

        public void ApplyToolbarMoneyRow(Text valueText, int menuButtonFontSize, Color textPrimary)
        {
            if (valueText == null) return;
            valueText.fontSize = menuButtonFontSize;
            valueText.color = textPrimary;
            valueText.supportRichText = true;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public void ApplyBodyStatRow(Text textField, int fontSizeBody, Color textPrimary, int fontSizeCaption, Color textSecondary, bool styleSiblingLabels = true)
        {
            if (textField == null) return;
            if (styleSiblingLabels) StyleSiblingLabelTexts(textField.transform, fontSizeCaption, textSecondary);
            textField.fontSize = fontSizeBody;
            textField.color = textPrimary;
            textField.supportRichText = true;
        }

        public void ApplyCaptionText(Text textField, int fontSizeCaption, Color textSecondary)
        {
            if (textField == null) return;
            textField.fontSize = fontSizeCaption;
            textField.color = textSecondary;
        }

        public void ApplyCellDataPanelTextStyle(Text gridCoordinatesText, int fontSizeCaption)
        {
            if (gridCoordinatesText == null) return;
            gridCoordinatesText.color = Color.white;
            gridCoordinatesText.fontSize = Mathf.Max(fontSizeCaption + 6, 17);
            gridCoordinatesText.horizontalOverflow = HorizontalWrapMode.Wrap;
            gridCoordinatesText.verticalOverflow = VerticalWrapMode.Overflow;
            gridCoordinatesText.alignment = TextAnchor.UpperLeft;
        }

        public static void StyleSiblingLabelTexts(Transform valueTransform, int captionSize, Color captionColor)
        {
            Transform parent = valueTransform.parent;
            if (parent == null) return;
            foreach (Transform child in parent)
            {
                if (child == valueTransform) continue;
                var t = child.GetComponent<Text>();
                if (t == null) continue;
                t.fontSize = captionSize; t.color = captionColor;
            }
        }

        // ─── Panel tint / divider ────────────────────────────────────────────────
        /// <summary>Walk parents from anchorText until panelName; tint its Image with color.</summary>
        public void TintPanelRootBehindReference(string panelName, Text anchorText, Color color)
        {
            if (anchorText == null) return;
            Transform t = anchorText.transform;
            for (int depth = 0; depth < 24 && t != null; depth++)
            {
                if (t.name == panelName) { var img = t.GetComponent<Image>(); if (img != null) img.color = color; return; }
                t = t.parent;
            }
        }

        /// <summary>Apply theme tokens to TaxPanel growth-budget row texts (caption + body, skip slider labels).</summary>
        public void ApplyTaxPanelBudgetRowTexts(Text residentialTaxText, int fontSizeBody, Color textPrimary, int fontSizeCaption, Color textSecondary)
        {
            if (residentialTaxText == null) return;
            Transform taxPanel = residentialTaxText.transform.parent;
            if (taxPanel == null) return;
            foreach (Transform child in taxPanel)
            {
                Text t = child.GetComponent<Text>();
                if (t == null) continue;
                string n = child.name;
                if (n == "ResidentialTaxText" || n.StartsWith("CommercialTaxText", StringComparison.Ordinal) || n == "IndustrialTaxText") continue;
                if (n == "TaxGrowthBudgetPercentLabel" || (n.Contains("GrowthLabel", StringComparison.Ordinal) && n.Contains("(1)", StringComparison.Ordinal)))
                { ApplyCaptionText(t, fontSizeCaption, textSecondary); continue; }
                if (n == "TotalGrowthLabel" || n == "RoadGrowthLabel" || n == "EnergyGrowthLabel" || n == "WaterGrowthLabel" || n == "ZoningGrowthLabel")
                    ApplyBodyStatRow(t, fontSizeBody, textPrimary, fontSizeCaption, textSecondary, styleSiblingLabels: false);
            }
        }

        /// <summary>Add thin horizontal dividers to TaxPanel if missing.</summary>
        public void EnsureTaxPanelDividerStripes(Text residentialTaxText, Color borderSubtle)
        {
            if (residentialTaxText == null) return;
            Transform taxPanel = residentialTaxText.transform.parent;
            if (taxPanel == null || taxPanel.Find("Fe50TaxDividerUpper") != null) return;
            var panelBg = taxPanel.GetComponent<Image>();
            Sprite stripeSprite = panelBg != null ? panelBg.sprite : null;
            CreateDividerStripe(taxPanel, "Fe50TaxDividerUpper", stripeSprite, borderSubtle, new Vector2(0f, 48f), new Vector2(200f, 1f));
            CreateDividerStripe(taxPanel, "Fe50TaxDividerLower", stripeSprite, borderSubtle, new Vector2(0f, -40f), new Vector2(200f, 1f));
        }

        public void ApplyLoadGameAndFundsPanels(GameObject loadGameMenu, GameObject insufficientFundsPanel, Color modalDimmerColor, Color surfaceCardHud)
        {
            if (loadGameMenu != null)
            {
                var rootImage = loadGameMenu.GetComponent<Image>();
                if (rootImage != null) rootImage.color = modalDimmerColor;
                foreach (Transform child in loadGameMenu.transform) { var img = child.GetComponent<Image>(); if (img != null) { img.color = surfaceCardHud; break; } }
            }
            if (insufficientFundsPanel != null)
            {
                var rootImage = insufficientFundsPanel.GetComponent<Image>();
                if (rootImage != null) rootImage.color = modalDimmerColor;
                foreach (Transform child in insufficientFundsPanel.transform) { var img = child.GetComponent<Image>(); if (img != null) { img.color = surfaceCardHud; break; } }
            }
        }

        // ─── Static divider builder ──────────────────────────────────────────────
        /// <summary>Create thin horizontal divider stripe Image under parent.</summary>
        public static void CreateDividerStripe(Transform parent, string objectName, Sprite sprite, Color lineColor, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta; rt.anchoredPosition = anchoredPosition;
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.type = Image.Type.Simple; img.raycastTarget = false; img.color = lineColor;
        }
    }
}

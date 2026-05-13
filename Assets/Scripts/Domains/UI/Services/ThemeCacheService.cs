using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI.Services
{
    /// <summary>
    /// CellDataPanel chrome build, scroll layout, demand gauge, alignment.
    /// Pure instance service; no MonoBehaviour, no UiTheme reference.
    /// Split from ThemeService (Stage 7.4 Tier-E atomization).
    /// </summary>
    public class ThemeCacheService
    {
        // ─── CellDataPanel chrome ────────────────────────────────────────────────
        /// <summary>CellDataPanel chrome setup (requires theme surfaceToolbar color; null = use fallback).</summary>
        public void EnsureCellDataPanelChrome(ref Text gridCoordinatesText, Color? surfaceToolbar)
        {
            EnsureCellDataPanelTextField(ref gridCoordinatesText);
            if (gridCoordinatesText == null) return;
            Transform t = gridCoordinatesText.transform;
            RectTransform textRt = gridCoordinatesText.GetComponent<RectTransform>();
            Transform chromeRoot = ThemeTokenResolveService.FindCellDataPanelRoot(t);
            if (chromeRoot != null)
            {
                RectTransform chromeRt = chromeRoot.GetComponent<RectTransform>();
                EnsureCellDataPanelTextUnderInset(chromeRt.transform, textRt);
                ApplyCellDataPanelTextStyleInternal(gridCoordinatesText, 11);
                EnsureCellDataPanelHudMount(chromeRt);
                AlignCellDataPanel(chromeRt);
                UpdateCellDataPanelScrollLayout(chromeRt, gridCoordinatesText);
                return;
            }
            Transform originalParent = t.parent;
            int siblingIndex = t.GetSiblingIndex();
            Transform chromeMount = ThemeTokenResolveService.FindHudLayoutRoot(t) ?? originalParent;
            GameObject chrome = new GameObject(ThemeTokenResolveService.CellDataPanelName, typeof(RectTransform));
            chrome.transform.SetParent(chromeMount, false);
            Transform cpForOrder = chromeMount.Find(ThemeTokenResolveService.ControlPanelObjectName);
            Transform mmForOrder = chromeMount.Find("MiniMapPanel");
            if (cpForOrder != null) chrome.transform.SetSiblingIndex(cpForOrder.GetSiblingIndex() + 1);
            else if (mmForOrder != null) chrome.transform.SetSiblingIndex(mmForOrder.GetSiblingIndex() + 1);
            else chrome.transform.SetSiblingIndex(siblingIndex);
            RectTransform chromeRtNew = chrome.GetComponent<RectTransform>();
            chromeRtNew.anchorMin = textRt.anchorMin; chromeRtNew.anchorMax = textRt.anchorMax;
            chromeRtNew.pivot = textRt.pivot; chromeRtNew.anchoredPosition = textRt.anchoredPosition;
            chromeRtNew.sizeDelta = textRt.sizeDelta + new Vector2(54f, 28f); chromeRtNew.localScale = textRt.localScale;
            GameObject bgGo = new GameObject("CellDataPanelBg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(chrome.transform, false); bgGo.transform.SetAsFirstSibling();
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgGo.GetComponent<Image>(); bgImg.raycastTarget = false;
            Color panel = surfaceToolbar.HasValue ? surfaceToolbar.Value : new Color(0.07f, 0.08f, 0.11f, 1f);
            panel.a = surfaceToolbar.HasValue ? Mathf.Clamp(surfaceToolbar.Value.a * 0.92f, 0.78f, 0.9f) : 0.86f;
            bgImg.color = panel;
            var bgOutline = bgGo.AddComponent<Outline>();
            bgOutline.effectColor = new Color(1f, 0.690f, 0.125f, 1f);
            bgOutline.effectDistance = new Vector2(6f, -6f);
            bgOutline.useGraphicAlpha = false;
            EnsureCellDataPanelTextUnderInset(chrome.transform, textRt);
            ApplyCellDataPanelTextStyleInternal(gridCoordinatesText, 11);
            EnsureCellDataPanelHudMount(chromeRtNew);
            AlignCellDataPanel(chromeRtNew);
            UpdateCellDataPanelScrollLayout(chromeRtNew, gridCoordinatesText);
        }

        /// <summary>Rebuild lost GridCoordinatesText when SerializeField null.</summary>
        public void EnsureCellDataPanelTextField(ref Text gridCoordinatesText)
        {
            if (gridCoordinatesText != null) return;
            Transform hudLayoutRoot = ThemeTokenResolveService.FindHudLayoutRootForRebuild();
            if (hudLayoutRoot == null) return;
            GameObject textGo = new GameObject("GridCoordinatesText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(hudLayoutRoot, false);
            Text txt = textGo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 18; txt.color = Color.white; txt.alignment = TextAnchor.UpperLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap; txt.verticalOverflow = VerticalWrapMode.Overflow; txt.text = string.Empty;
            RectTransform rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f); rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(270f, 270f); rt.anchoredPosition = new Vector2(-16f, 0f);
            gridCoordinatesText = txt;
        }

        public void RefreshCellDataPanelLayout(Text gridCoordinatesText)
        {
            if (gridCoordinatesText == null) return;
            Transform chromeRoot = ThemeTokenResolveService.FindCellDataPanelRoot(gridCoordinatesText.transform);
            if (chromeRoot == null) return;
            RectTransform chromeRt = chromeRoot.GetComponent<RectTransform>();
            if (chromeRt == null) return;
            AlignCellDataPanel(chromeRt);
            UpdateCellDataPanelScrollLayout(chromeRt, gridCoordinatesText);
        }

        // ─── HUD mount + alignment ────────────────────────────────────────────────
        public static void EnsureCellDataPanelHudMount(RectTransform chromeRt)
        {
            if (chromeRt == null) return;
            Transform mount = ThemeTokenResolveService.FindHudLayoutRoot(chromeRt);
            if (mount == null) return;
            chromeRt.SetParent(mount, false);
            Transform cp = mount.Find(ThemeTokenResolveService.ControlPanelObjectName);
            if (cp != null) chromeRt.SetSiblingIndex(cp.GetSiblingIndex() + 1);
            else { Transform mm = mount.Find("MiniMapPanel"); if (mm != null) chromeRt.SetSiblingIndex(mm.GetSiblingIndex() + 1); }
        }

        public static void AlignCellDataPanel(RectTransform chromeRt)
        {
            if (chromeRt == null) return;
            Canvas.ForceUpdateCanvases();
            EnsureCellDataPanelHudMount(chromeRt);
            RectTransform parentRt = chromeRt.parent as RectTransform;
            if (parentRt == null) return;
            const float rightMargin = 16f;
            float side = Mathf.Min(ThemeTokenResolveService.CellDataPanelMaxSquareSide, parentRt.rect.height * 0.5f);
            side = Mathf.Max(side, 1f);
            chromeRt.anchorMin = new Vector2(1f, 0.5f); chromeRt.anchorMax = new Vector2(1f, 0.5f); chromeRt.pivot = new Vector2(1f, 0.5f);
            chromeRt.anchoredPosition = new Vector2(-rightMargin, 0f); chromeRt.sizeDelta = new Vector2(side, side);
        }

        public static void CellDataPanelApplySquareLayout(RectTransform chromeRt, RectTransform parentRt, float minX, float referenceTopY, float gapAboveReference, float widthBand)
        {
            float minChromeBottom = referenceTopY + gapAboveReference;
            float limitTop = float.PositiveInfinity;
            Transform dpTransform = parentRt.Find(ThemeTokenResolveService.DataPanelButtonsObjectName);
            RectTransform dpRt = dpTransform != null ? dpTransform.GetComponent<RectTransform>() : null;
            if (dpRt != null && ThemeTokenResolveService.TryGetRectBoundsInParent(parentRt, dpRt, out _, out _, out float dpMinY, out _))
                limitTop = dpMinY - ThemeTokenResolveService.CellDataPanelGapBelowDataPanelButtons;
            float verticalRoom = limitTop - minChromeBottom;
            if (!float.IsFinite(verticalRoom) || verticalRoom > 5000f) verticalRoom = ThemeTokenResolveService.CellDataPanelMaxSquareSide;
            verticalRoom = Mathf.Max(verticalRoom, 1f);
            float panelW = Mathf.Max(widthBand, 44f);
            float side = Mathf.Min(Mathf.Min(panelW, ThemeTokenResolveService.CellDataPanelMaxSquareSide), verticalRoom); side = Mathf.Max(side, 1f);
            float chromeBottomY = minChromeBottom; float chromeTopY = chromeBottomY + side;
            if (float.IsFinite(limitTop) && chromeTopY > limitTop)
            { chromeBottomY = limitTop - side; chromeBottomY = Mathf.Max(chromeBottomY, minChromeBottom); }
            Rect parentRect = parentRt.rect;
            Vector2 anchorRefBottomLeft = new Vector2(Mathf.Lerp(parentRect.xMin, parentRect.xMax, 0f), Mathf.Lerp(parentRect.yMin, parentRect.yMax, 0f));
            chromeRt.anchorMin = Vector2.zero; chromeRt.anchorMax = Vector2.zero; chromeRt.pivot = Vector2.zero;
            chromeRt.anchoredPosition = new Vector2(minX, chromeBottomY) - anchorRefBottomLeft; chromeRt.sizeDelta = new Vector2(side, side);
        }

        public static bool TryAlignCellDataPanelToControlPanel(RectTransform chromeRt)
        {
            RectTransform parentRt = chromeRt.parent as RectTransform;
            if (parentRt == null) return false;
            Transform cpTransform = parentRt.Find(ThemeTokenResolveService.ControlPanelObjectName);
            RectTransform cpRt = cpTransform != null ? cpTransform.GetComponent<RectTransform>() : null;
            if (cpRt == null) return false;
            if (!ThemeTokenResolveService.TryGetRectBoundsInParent(parentRt, cpRt, out float minX, out float maxX, out _, out float cpMaxY)) return false;
            float panelW = maxX - minX; if (panelW < 32f) panelW = Mathf.Max(cpRt.rect.width, 80f);
            CellDataPanelApplySquareLayout(chromeRt, parentRt, minX, cpMaxY, ThemeTokenResolveService.CellDataPanelGapAboveControlPanel, panelW);
            return true;
        }

        public static void AlignCellDataPanelToMiniMap(RectTransform chromeRt)
        {
            if (chromeRt == null) return;
            Canvas.ForceUpdateCanvases(); EnsureCellDataPanelHudMount(chromeRt);
            RectTransform parentRt = chromeRt.parent as RectTransform; if (parentRt == null) return;
            Transform mmTransform = parentRt.Find("MiniMapPanel");
            RectTransform mmRt = mmTransform != null ? mmTransform.GetComponent<RectTransform>() : null; if (mmRt == null) return;
            if (!ThemeTokenResolveService.TryGetRectBoundsInParent(parentRt, mmRt, out float minX, out float maxX, out _, out float mmMaxY)) return;
            float mmWidth = maxX - minX; if (mmWidth < 32f) mmWidth = Mathf.Max(mmRt.rect.width, 80f);
            CellDataPanelApplySquareLayout(chromeRt, parentRt, minX, mmMaxY, ThemeTokenResolveService.CellDataPanelGapAboveMinimap, mmWidth);
        }

        // ─── Scroll layout ────────────────────────────────────────────────────────
        public static void UpdateCellDataPanelScrollLayout(RectTransform chromeRt, Text dbgText)
        {
            if (chromeRt == null) return;
            RectTransform insetRt = ThemeTokenResolveService.FindCellDataPanelInset(chromeRt);
            if (insetRt == null) return;
            Transform scrollRootT = insetRt.Find(ThemeTokenResolveService.CellDataPanelScrollRootName);
            if (scrollRootT == null) return;
            ScrollRect sr = scrollRootT.GetComponent<ScrollRect>();
            if (sr == null) return;
            RectTransform viewport = sr.viewport; RectTransform content = sr.content as RectTransform;
            if (viewport == null || content == null) return;
            Canvas.ForceUpdateCanvases();
            float viewW = Mathf.Max(viewport.rect.width, 40f); float viewH = Mathf.Max(viewport.rect.height, 1f);
            float prefH = EstimateCellDataPanelTextPreferredHeight(dbgText, viewW);
            float contentH = Mathf.Max(prefH, viewH);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentH);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            bool needScroll = prefH > viewH + 0.5f;
            sr.vertical = needScroll; sr.enabled = true;
            if (!needScroll) sr.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }

        public static float EstimateCellDataPanelTextPreferredHeight(Text text, float innerWidth)
        {
            if (text == null) return 64f;
            string sample = string.IsNullOrEmpty(text.text) ? " " : text.text;
            float w = Mathf.Max(innerWidth, 40f);
            TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(w, 0.01f));
            float px = text.cachedTextGeneratorForLayout.GetPreferredHeight(sample, settings);
            return Mathf.Clamp(px, 28f, 10000f);
        }

        // ─── Text inset / scroll under-inset ─────────────────────────────────────
        public static void ApplyCellDataPanelTextInset(RectTransform insetRt)
        {
            if (insetRt == null) return;
            VerticalLayoutGroup vlg = insetRt.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) UnityEngine.Object.Destroy(vlg);
            insetRt.anchorMin = Vector2.zero; insetRt.anchorMax = Vector2.one; insetRt.anchoredPosition = Vector2.zero; insetRt.sizeDelta = Vector2.zero;
            insetRt.offsetMin = new Vector2(18f, 10f); insetRt.offsetMax = new Vector2(-18f, -10f);
        }

        public static void EnsureCellDataPanelTextLayoutDriver(RectTransform textRt)
        {
            if (textRt == null) return;
            ContentSizeFitter fitter = textRt.GetComponent<ContentSizeFitter>();
            if (fitter != null) UnityEngine.Object.Destroy(fitter);
            LayoutElement le = textRt.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            CanvasScaler scaler = textRt.GetComponent<CanvasScaler>();
            if (scaler != null) { scaler.enabled = false; UnityEngine.Object.Destroy(scaler); }
            Canvas nested = textRt.GetComponent<Canvas>();
            if (nested != null) { nested.enabled = false; UnityEngine.Object.Destroy(nested); }
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero; textRt.sizeDelta = Vector2.zero; textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
        }

        public static void EnsureCellDataPanelTextUnderInset(Transform chromeTransform, RectTransform textRt)
        {
            if (chromeTransform == null || textRt == null) return;
            Transform insetTransform = chromeTransform.Find(ThemeTokenResolveService.CellDataPanelTextInsetName);
            if (insetTransform == null) insetTransform = chromeTransform.Find(ThemeTokenResolveService.CellDataPanelTextHolderAlt);
            if (insetTransform == null)
            {
                GameObject insetGo = new GameObject(ThemeTokenResolveService.CellDataPanelTextInsetName, typeof(RectTransform));
                insetTransform = insetGo.transform; insetTransform.SetParent(chromeTransform, false);
                Transform bg = chromeTransform.Find("CellDataPanelBg");
                if (bg != null) insetTransform.SetSiblingIndex(bg.GetSiblingIndex() + 1);
            }
            RectTransform insetRt = insetTransform.GetComponent<RectTransform>();
            ApplyCellDataPanelTextInset(insetRt);
            EnsureCellDataPanelScrollUnderInset(insetRt, textRt);
            EnsureCellDataPanelTextLayoutDriver(textRt);
        }

        public static void EnsureCellDataPanelScrollUnderInset(RectTransform insetRt, RectTransform textRt)
        {
            if (insetRt == null || textRt == null) return;
            Transform scrollRootT = insetRt.Find(ThemeTokenResolveService.CellDataPanelScrollRootName);
            if (scrollRootT == null)
            {
                GameObject scrollGo = new GameObject(ThemeTokenResolveService.CellDataPanelScrollRootName, typeof(RectTransform), typeof(ScrollRect));
                scrollRootT = scrollGo.transform; scrollRootT.SetParent(insetRt, false);
                RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
                scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one; scrollRt.offsetMin = Vector2.zero; scrollRt.offsetMax = Vector2.zero;
                GameObject vpGo = new GameObject(ThemeTokenResolveService.CellDataPanelViewportName, typeof(RectTransform), typeof(RectMask2D), typeof(Image));
                vpGo.transform.SetParent(scrollRootT, false);
                RectTransform vpRt = vpGo.GetComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
                Image vpImg = vpGo.GetComponent<Image>(); vpImg.color = new Color(0f, 0f, 0f, 0.01f); vpImg.raycastTarget = true;
                GameObject contentGo = new GameObject(ThemeTokenResolveService.CellDataPanelContentName, typeof(RectTransform));
                contentGo.transform.SetParent(vpGo.transform, false);
                RectTransform contentRt = contentGo.GetComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f); contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero; contentRt.sizeDelta = Vector2.zero;
                ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
                sr.viewport = vpRt; sr.content = contentRt; sr.horizontal = false; sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 24f; sr.inertia = true; sr.decelerationRate = 0.135f;
            }
            Transform viewportT = scrollRootT.Find(ThemeTokenResolveService.CellDataPanelViewportName);
            Transform contentTransform = viewportT != null ? viewportT.Find(ThemeTokenResolveService.CellDataPanelContentName) : null;
            RectTransform contentParent = contentTransform != null ? contentTransform.GetComponent<RectTransform>() : null;
            if (contentParent == null) return;
            textRt.SetParent(contentParent, false);
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero; textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero; textRt.localScale = Vector3.one;
        }

        // ─── Demand gauge ────────────────────────────────────────────────────────
        public void EnsureDemandGaugeForPanel(Text anchorText, string panelExactName, ref Image fillImageRef, Color? borderSubtle, Color? surfaceToolbar)
        {
            if (anchorText == null) return;
            Transform panel = ThemeTokenResolveService.FindNamedAncestor(anchorText.transform, panelExactName);
            if (panel == null) return;
            Transform existing = panel.Find("Fe50DemandGauge");
            if (existing != null)
            {
                RectTransform gaugeRtExisting = existing.GetComponent<RectTransform>();
                if (gaugeRtExisting != null) { gaugeRtExisting.offsetMin = new Vector2(8f, 5f); gaugeRtExisting.offsetMax = new Vector2(-8f, 18f); }
                if (fillImageRef == null) { Transform fillT = existing.Find("Fe50DemandGaugeFill"); if (fillT != null) fillImageRef = fillT.GetComponent<Image>(); }
                return;
            }
            var panelBg = panel.GetComponent<Image>();
            Sprite sprite = panelBg != null ? panelBg.sprite : null;
            GameObject gauge = new GameObject("Fe50DemandGauge", typeof(RectTransform));
            gauge.transform.SetParent(panel, false);
            RectTransform gaugeRt = gauge.GetComponent<RectTransform>();
            gaugeRt.anchorMin = new Vector2(0f, 0f); gaugeRt.anchorMax = new Vector2(1f, 0f); gaugeRt.pivot = new Vector2(0.5f, 0f);
            gaugeRt.offsetMin = new Vector2(8f, 5f); gaugeRt.offsetMax = new Vector2(-8f, 18f);
            GameObject trackGo = new GameObject("Fe50DemandGaugeTrack", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(gauge.transform, false);
            RectTransform trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.anchorMin = Vector2.zero; trackRt.anchorMax = Vector2.one; trackRt.offsetMin = Vector2.zero; trackRt.offsetMax = Vector2.zero;
            Image trackImg = trackGo.GetComponent<Image>(); trackImg.sprite = sprite; trackImg.type = Image.Type.Simple; trackImg.raycastTarget = false;
            Color trackCol = borderSubtle.HasValue ? borderSubtle.Value : new Color(0.12f, 0.13f, 0.16f, 1f);
            trackCol.a = Mathf.Max(0.55f, trackCol.a * 0.85f); trackImg.color = trackCol;
            GameObject fillGo = new GameObject("Fe50DemandGaugeFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(gauge.transform, false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            Image fillImg = fillGo.GetComponent<Image>(); fillImg.sprite = sprite; fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal; fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0.5f; fillImg.raycastTarget = false; fillImageRef = fillImg;
        }

        // ─── Private helpers ──────────────────────────────────────────────────────
        private static void ApplyCellDataPanelTextStyleInternal(Text gridCoordinatesText, int fontSizeCaption)
        {
            if (gridCoordinatesText == null) return;
            gridCoordinatesText.color = Color.white;
            gridCoordinatesText.fontSize = Mathf.Max(fontSizeCaption + 6, 17);
            gridCoordinatesText.horizontalOverflow = HorizontalWrapMode.Wrap;
            gridCoordinatesText.verticalOverflow = VerticalWrapMode.Overflow;
            gridCoordinatesText.alignment = TextAnchor.UpperLeft;
        }
    }
}

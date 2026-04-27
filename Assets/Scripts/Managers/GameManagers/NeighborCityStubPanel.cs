using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Territory.Core;

namespace Territory.UI
{
    /// <summary>
    /// City HUD sidebar panel: iterates <see cref="BorderSide"/> enum, calls
    /// <see cref="GridManager.GetNeighborStub(BorderSide)"/> per side, and
    /// instantiates one inert <see cref="NeighborStubCard"/> per non-null result.
    /// Cards display stub <c>displayName</c> + border direction; no input handlers.
    /// Establishes the neighbor-stub UI consumer pattern that Step 3 region UI
    /// will flesh out with reactive refresh + icons.
    ///
    /// Empty-state: zero non-null stubs renders one <c>(no neighbors)</c> placeholder
    /// card so the panel surfaces missing-seed regressions instead of going blank.
    /// </summary>
    public class NeighborCityStubPanel : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private GridManager _gridManager;

        [Header("Card factory")]
        [Tooltip("Optional prefab; when null, panel creates cards programmatically (matches BudgetPanel runtime-root pattern).")]
        [SerializeField] private GameObject _stubCardPrefab;
        [SerializeField] private Transform _cardContainer;

        private bool _populated;

        private void Awake()
        {
            if (_gridManager == null)
                _gridManager = FindObjectOfType<GridManager>();
        }

        private void Start()
        {
            PopulateCards();
        }

        private void OnEnable()
        {
            // Re-populate on re-enable for HUD toggles after Start.
            if (_populated)
                PopulateCards();
        }

        private void PopulateCards()
        {
            if (_gridManager == null)
            {
                Debug.LogWarning("NeighborCityStubPanel: GridManager missing");
                return;
            }

            EnsureCardContainerIfNeeded();

            // Clear prior cards so re-populate does not stack duplicates.
            for (int i = _cardContainer.childCount - 1; i >= 0; i--)
                DestroyImmediateOrRuntime(_cardContainer.GetChild(i).gameObject);

            int rendered = 0;
            foreach (BorderSide side in Enum.GetValues(typeof(BorderSide)))
            {
                NeighborCityStub? stub = _gridManager.GetNeighborStub(side);
                if (!stub.HasValue)
                    continue;
                AddStubCard(stub.Value.displayName, $"Border: {stub.Value.borderSide}");
                rendered++;
            }

            if (rendered == 0)
                AddStubCard("(no neighbors)", "Border: —");

            _populated = true;
        }

        private void AddStubCard(string displayName, string borderText)
        {
            GameObject cardGo;
            NeighborStubCard binder;

            if (_stubCardPrefab != null)
            {
                cardGo = Instantiate(_stubCardPrefab, _cardContainer);
                binder = cardGo.GetComponent<NeighborStubCard>();
                if (binder == null)
                    binder = cardGo.AddComponent<NeighborStubCard>();
            }
            else
            {
                cardGo = new GameObject("NeighborStubCard");
                cardGo.transform.SetParent(_cardContainer, false);
                var rt = cardGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(220, 44);
                var v = cardGo.AddComponent<VerticalLayoutGroup>();
                v.spacing = 2;
                v.padding = new RectOffset(6, 6, 4, 4);
                v.childForceExpandWidth = true;
                v.childControlWidth = true;
                v.childControlHeight = true;
                binder = cardGo.AddComponent<NeighborStubCard>();
                binder.BootstrapRuntimeChildren();
            }

            binder.SetDisplayName(displayName);
            binder.SetBorder(borderText);
        }

        private void EnsureCardContainerIfNeeded()
        {
            if (_cardContainer != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // No canvas at all — fall back to using this transform so smoke
                // assertions still see child cards even outside HUD wiring.
                _cardContainer = transform;
                return;
            }

            if (GetComponentInParent<Canvas>() == null)
                transform.SetParent(canvas.transform, false);

            if (GetComponent<RectTransform>() == null)
                gameObject.AddComponent<RectTransform>();

            if (GetComponent<VerticalLayoutGroup>() == null)
            {
                var v = gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 4;
                v.padding = new RectOffset(8, 8, 8, 8);
                v.childForceExpandWidth = true;
                v.childControlWidth = true;
                v.childControlHeight = true;
            }
            _cardContainer = transform;
        }

        private static void DestroyImmediateOrRuntime(GameObject go)
        {
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }
    }
}

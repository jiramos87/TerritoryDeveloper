using UnityEngine;

namespace Territory.UI.Themed
{
    /// <summary>Themed panel root + runtime slot graph composer; baked by <c>UiBakeHandler</c> from IR <c>panels[]</c>.</summary>
    public class ThemedPanel : ThemedPrimitiveBase
    {
        [SerializeField] private SlotSpec[] _slots;
        [SerializeField] private GameObject[] _children;

        /// <summary>Bake-time SlotSpec[] (read-only — runtime accessor used by EditMode smoke).</summary>
        public SlotSpec[] Slots => _slots;

        /// <summary>Bake-time child candidate refs (read-only — runtime accessor used by EditMode smoke).</summary>
        public GameObject[] Children => _children;

        public override void ApplyTheme(UiTheme theme)
        {
            // Slot graph composer: walk slots, match each accept-rule against children, parent matched child.
            // Bake-time-vs-runtime contract: bake handler populates _slots + _children deterministically;
            // runtime composer is reparent-only — no PlayMode-only API (no Instantiate of scene refs).
            if (_slots == null || _children == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot.accepts == null || slot.accepts.Length == 0) continue;

                GameObject matched = null;
                for (int c = 0; c < _children.Length; c++)
                {
                    var child = _children[c];
                    if (child == null) continue;
                    if (ChildMatches(child, slot.accepts))
                    {
                        matched = child;
                        break;
                    }
                }

                if (matched == null)
                {
                    Debug.LogWarning(
                        $"[ThemedPanel] slot {slot.slug} unbound — no child matched accepts[{string.Join(",", slot.accepts)}]");
                    continue;
                }

                if (matched.transform.parent != transform)
                {
                    matched.transform.SetParent(transform, false);
                }
            }
        }

        private static bool ChildMatches(GameObject child, string[] accepts)
        {
            for (int i = 0; i < accepts.Length; i++)
            {
                var token = accepts[i];
                if (string.IsNullOrEmpty(token)) continue;
                if (child.name == token) return true;
                // Tag fallback — guarded since CompareTag throws on undefined tags.
                try
                {
                    if (child.CompareTag(token)) return true;
                }
                catch (UnityException)
                {
                    // Undefined tag — name-only match path; non-fatal.
                }
            }
            return false;
        }
    }
}

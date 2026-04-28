using Territory.UI.StudioControls;
using UnityEngine;
using UnityEngine.Events;

namespace Territory.UI.Juice
{
    /// <summary>
    /// Particle-burst surface decorator for <see cref="IlluminatedButton"/>. Idempotent <c>Awake</c>
    /// instantiates a child <see cref="ParticleSystem"/> named <c>SparkleBurst_Particles</c> once;
    /// burst <see cref="ParticleSystem.Emit(int)"/> on a configured <see cref="UnityEvent"/>
    /// trigger (default = button OnClick) per <c>burstCount</c>.
    /// </summary>
    [RequireComponent(typeof(IlluminatedButton))]
    public class SparkleBurst : JuiceBase
    {
        /// <summary>Child particle object name; idempotency anchor for re-bake.</summary>
        public const string ParticleChildName = "SparkleBurst_Particles";

        [SerializeField] private UnityEvent triggerEvent;
        [SerializeField] private int burstCount = 12;
        [SerializeField] private Sprite sparkleSprite;

        private IlluminatedButton _button;
        private ParticleSystem _particles;

        /// <summary>Read-only particle system (used by smoke tests).</summary>
        public ParticleSystem Particles => _particles;

        /// <summary>External hook for non-Click triggers.</summary>
        public UnityEvent TriggerEvent => triggerEvent;

        protected override void Awake()
        {
            base.Awake();
            _button = GetComponent<IlluminatedButton>();
            EnsureParticleChild();
        }

        private void EnsureParticleChild()
        {
            var existing = transform.Find(ParticleChildName);
            if (existing != null)
            {
                _particles = existing.GetComponent<ParticleSystem>();
                return;
            }

            var go = new GameObject(ParticleChildName, typeof(RectTransform), typeof(ParticleSystem));
            go.transform.SetParent(transform, false);

            _particles = go.GetComponent<ParticleSystem>();
            var main = _particles.main;
            main.startLifetime = 0.6f;
            main.startSize = 4f;
            main.startSpeed = 30f;
            main.startColor = Color.white;
            main.loop = false;
            main.playOnAwake = false;

            var emission = _particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = _particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 4f;

            if (sparkleSprite != null)
            {
                var renderer = _particles.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Sprites/Default"));
                    mat.mainTexture = sparkleSprite.texture;
                    renderer.material = mat;
                }
            }
        }

        private void OnEnable()
        {
            if (triggerEvent == null) triggerEvent = new UnityEvent();
            triggerEvent.AddListener(OnTriggerFired);
            if (_button != null) _button.OnClick.AddListener(OnTriggerFired);
        }

        private void OnDisable()
        {
            triggerEvent?.RemoveListener(OnTriggerFired);
            if (_button != null) _button.OnClick.RemoveListener(OnTriggerFired);
        }

        private void OnTriggerFired()
        {
            Burst();
        }

        /// <summary>Manual burst entry point (test fixture / non-event triggers).</summary>
        public void Burst()
        {
            if (_particles == null) return;
            _particles.Emit(burstCount);
        }
    }
}

using UnityEngine;

namespace Territory.IsoSceneCore
{
    /// <summary>Stub camera pan service for IsoSceneCore. Full extraction from GridManager lands in Stage 1.1.</summary>
    public sealed class IsoSceneCamera
    {
        private Camera _cam;
        private float _panSpeed = 5f;

        public void Configure(Camera cam, float panSpeed)
        {
            _cam = cam;
            _panSpeed = panSpeed;
        }

        public void Tick(float dt)
        {
            if (_cam == null) return;
            var v = Vector3.zero;
            if (Input.GetKey(KeyCode.LeftArrow))  v.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
            if (Input.GetKey(KeyCode.UpArrow))    v.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow))  v.y -= 1f;
            _cam.transform.position += v * _panSpeed * dt;
        }
    }
}

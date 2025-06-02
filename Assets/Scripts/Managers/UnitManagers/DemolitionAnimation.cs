using UnityEngine;

public class DemolitionAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDuration = 0.42f;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // Set speed to match current time scale but don't register with AnimatorManager
        if (animator != null)
        {
            animator.speed = Time.timeScale;
        }

        // Play animation and destroy
        PlayOnce();
    }

    private void PlayOnce()
    {
        if (animator != null)
        {
            animator.Play("DemolitionExplosion", 0, 0f);
        }

        // Destroy after fixed duration
        Destroy(gameObject, animationDuration);
    }

    public void Initialize(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Territory.Timing
{
/// <summary>
/// Controls animation speed scaling for animated game objects (e.g., water tiles).
/// Synchronizes animator playback speed with TimeManager's game speed setting.
/// </summary>
public class AnimatorManager : MonoBehaviour
{
    private List<Animator> animators = new List<Animator>();
    public TimeManager timeManager;

    void Start()
    {
        float currentTimeSpeed = timeManager.GetCurrentTimeMultiplier();

        SetAnimatorSpeed(currentTimeSpeed);
    }

    public void RegisterAnimator(Animator animator)
    {
        if (!animators.Contains(animator))
        {
            animators.Add(animator);
        }
    }

    void Update()
    {
    }

    public void SetAnimatorSpeed(float speed)
    {
        float timeScale = speed * 0.5f;
        UnityEngine.Time.timeScale = timeScale;
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.speed = timeScale;
                animator.Update(0f);
            }
        }
    }
}
}

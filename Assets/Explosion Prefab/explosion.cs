using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    private void Start()
    {
        // Play the explosion animation.
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("explosion");
        }
    }
}

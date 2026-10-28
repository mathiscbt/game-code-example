using UnityEngine;
using System.Collections;

/// <summary>
/// A placeable bomb: once placed, it counts down, then explodes and pushes nearby Rigidbodies away.
/// Implements IPlaceable.
/// </summary>
public class TimeBomb : MonoBehaviour, IPlaceable
{
    [Header("Bomb Settings")]
    [SerializeField] private float timeToExplode = 3f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float upwardModifier = 1f;

    private Renderer bombRenderer;
    private Vector3 originalScale;
    private Color[] originalColors;

    /// <summary>
    /// Caches the renderer, the original scale and the original material colors,
    /// for the countdown after placement.
    /// </summary>
    void Awake()
    {
        bombRenderer = GetComponentInChildren<Renderer>(); 
        originalScale = transform.localScale;
        
        if (bombRenderer != null)
        {
            originalColors = new Color[bombRenderer.materials.Length];
            
            for (int i = 0; i < bombRenderer.materials.Length; i++)
            {
                originalColors[i] = bombRenderer.materials[i].color;
            }
        }
    }

   /// <summary>
    /// Implements IPlaceable.OnPlaced(). Called when the bomb is placed in the world:
    /// starts the countdown and removes the pickup component, so the bomb can no longer be picked up.
    /// </summary>
    public void OnPlaced()
    {
        StartCoroutine(ExplosionCountdown());
        
        PickupItem pickupScript = GetComponentInChildren<PickupItem>();
        if (pickupScript != null)
        {
            Destroy(pickupScript);
        }
    }

    /// <summary>
    /// Animates the bomb until timeToExplode elapses: it grows, blinks red faster and faster.
    /// </summary>
    private IEnumerator ExplosionCountdown()
    {
        float timer = 0f;

        while (timer < timeToExplode)
        {
            timer += Time.deltaTime;
            
            float percent = timer / timeToExplode; 
            transform.localScale = originalScale * (1f + (percent * 0.5f));

            if (bombRenderer != null)
            {
                float blinkSpeed = 2f + (percent * 20f); 
                float blinkValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                
                for (int i = 0; i < bombRenderer.materials.Length; i++)
                {
                    bombRenderer.materials[i].color = Color.Lerp(originalColors[i], Color.red, blinkValue);
                }
            }
            yield return null;
        }
        Explode();
    }

    /// <summary>
    /// Pushes every nearby Rigidbody away from the bomb, then destroys the bomb.
    /// </summary>
    private void Explode()
    {
        Collider[] collidersAround = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in collidersAround)
        {
            Rigidbody target = hit.GetComponent<Rigidbody>();

            if (target != null)
            {
                target.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardModifier, ForceMode.Impulse);
            }
        }
        Destroy(gameObject);
    }
}
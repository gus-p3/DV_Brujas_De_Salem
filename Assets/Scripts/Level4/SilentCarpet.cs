using UnityEngine;

namespace Salem.Level4
{
    [RequireComponent(typeof(Collider2D))]
    public class SilentCarpet : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var noise = other.GetComponent<NoiseProducer>();
                if (noise != null)
                {
                    noise.SetOnCarpet(true);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var noise = other.GetComponent<NoiseProducer>();
                if (noise != null)
                {
                    noise.SetOnCarpet(false);
                }
            }
        }
    }
}

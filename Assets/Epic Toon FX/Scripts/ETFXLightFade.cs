using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal; // needed to access UniversalAdditionalLightData

namespace EpicToonFX
{
    public class ETFXLightFade : MonoBehaviour
    {
        [Header("Seconds to dim the light")]
        public float life = 0.2f;
        public bool killAfterLife = true;
        public bool destroyWholeGameObject = false; // set true if this is a temporary light prefab and you'd rather destroy the whole GO

        private Light li;
        private float initIntensity;
        private bool isFading = false;

        void Start()
        {
            li = GetComponent<Light>();
            if (li != null)
            {
                initIntensity = li.intensity;
                if (life <= 0f) life = 0.0001f; // avoid divide by zero
            }
            else
            {
                Debug.LogWarning("No Light component found on " + gameObject.name);
            }
        }

        void Update()
        {
            if (li == null) return;

            // fade
            li.intensity -= initIntensity * (Time.deltaTime / life);

            if (killAfterLife && !isFading && li.intensity <= 0f)
            {
                isFading = true;
                // if it's a directional light (the sun), never destroy it
                if (li.type == LightType.Directional)
                {
                    Debug.LogWarning("ETFXLightFade: skipping destruction of directional light on " + gameObject.name, gameObject);
                    enabled = false;
                    return;
                }

                // Option: disable the light and keep the GO for pooling (recommended for performance)
                // li.enabled = false;
                // return;

                // Break URP dependency: destroy UALD first (if present)
                var uald = GetComponent<UniversalAdditionalLightData>();
                if (uald != null)
                {
                    Destroy(uald);
                }

                // Wait a frame before removing the Light to give URP time to process
                StartCoroutine(DestroyLightNextFrame());
            }
        }

        private IEnumerator DestroyLightNextFrame()
        {
            yield return null; // allow one frame for URP cleanup

            if (li == null)
            {
                // maybe it was removed elsewhere already
                yield break;
            }

            if (destroyWholeGameObject)
            {
                Destroy(gameObject);
            }
            else
            {
                Destroy(li);
                // optionally, you can set li = null; if you rely on li later
                li = null;
            }
        }
    }
}

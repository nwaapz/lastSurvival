using UnityEngine;

namespace GlobalTools
{
    public class RandomMaterialSelector : MonoBehaviour
    {
        [Tooltip("The list of materials to choose from. This list will be cleared after selection to free memory.")]
        public Material[] availableMaterials;

        private void Start()
        {
            if (availableMaterials == null || availableMaterials.Length == 0)
            {
                Debug.LogWarning("RandomMaterialSelector: No materials assigned to availableMaterials array.", this);
                return;
            }

            Renderer rend = GetComponent<Renderer>();
            if (rend == null)
            {
                Debug.LogError("RandomMaterialSelector: No Renderer component found on this GameObject.", this);
                return;
            }

            // Select a random material
            int randomIndex = Random.Range(0, availableMaterials.Length);
            Material selectedMaterial = availableMaterials[randomIndex];

            // Assign the material
            rend.material = selectedMaterial;

            // Clear the array to free references as requested
            availableMaterials = null;
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;

namespace Warspire.Utils
{
    [ExecuteAlways]
    public class SkyboxController : MonoBehaviour
    {
        [Tooltip("The material using the 'Universal Render Pipeline/Skybox/SimpleGradientSkybox' shader")]
        public Material skyboxMaterial;

        [Header("Runtime Settings (Optional override)")]
        public bool updateEveryFrame = false;

        public bool forceCameraClearFlags = true;

        private void OnEnable()
        {
            ApplySkybox();
        }

        private void Start()
        {
            ApplySkybox();
        }

        private void Update()
        {
            if (updateEveryFrame && !Application.isPlaying)
            {
                ApplySkybox();
            }
        }

        public void ApplySkybox()
        {
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
                
                // Force settings
                RenderSettings.fog = false;

                if (forceCameraClearFlags)
                {
                    Camera[] allCameras = Camera.allCameras;
                    foreach (Camera cam in allCameras)
                    {
                        // Don't change UI cameras or hidden ones if possible, but for debugging let's be aggressive
                        // or just log what we find.
                        if (cam.clearFlags != CameraClearFlags.Skybox)
                        {
                            Debug.Log($"[SkyboxController] Forcing camera '{cam.name}' to Skybox ClearFlags (was {cam.clearFlags})");
                            cam.clearFlags = CameraClearFlags.Skybox;
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUI.color = Color.red;
            GUILayout.Label($"Skybox Mat: {(RenderSettings.skybox ? RenderSettings.skybox.name : "null")}");
            GUILayout.Label($"Fog Enabled: {RenderSettings.fog}");
            
            Camera main = Camera.main;
            if (main)
            {
                GUILayout.Label($"MainCam '{main.name}' Flags: {main.clearFlags}");
                GUILayout.Label($"MainCam FarClip: {main.farClipPlane}");
                GUILayout.Label($"MainCam CullingMask: {main.cullingMask}");
            }
            else
            {
                GUILayout.Label("NO MAIN CAMERA FOUND!");
            }

            if (GUILayout.Button("Force Update Settings"))
            {
                ApplySkybox();
            }
            
            GUILayout.EndArea();
        }
    }
}

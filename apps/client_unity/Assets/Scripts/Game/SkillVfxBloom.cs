using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Makes the composed skill VFX actually glow. Installs a global URP Bloom volume once at
    /// startup and turns on post-processing for whatever camera renders each scene. The VFX
    /// materials output HDR colour (see <see cref="SkillVfx.Glow"/>), so bloom catches their
    /// bright cores. Runtime-only + DontDestroyOnLoad, so no scene needs editing and the
    /// gameplay scenes (whose cameras may not have post-processing pre-enabled) still bloom.
    ///
    /// Tune the three values in the editor once you can see it — soft glow, not a wash-out.
    /// Safe no-op if the active pipeline isn't URP.
    /// </summary>
    public static class SkillVfxBloom
    {
        private const float Intensity = 1.1f; // glow strength
        private const float Threshold = 0.9f; // brightness a pixel needs before it blooms
        private const float Scatter = 0.6f;   // how far the glow spreads

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (GraphicsSettings.currentRenderPipeline == null) return; // built-in pipeline: no URP bloom
            EnsureVolume();
            EnableOnActiveCameras();
            SceneManager.sceneLoaded += (_, __) => EnableOnActiveCameras();
        }

        private static void EnsureVolume()
        {
            var go = new GameObject("SkillVfxBloom");
            Object.DontDestroyOnLoad(go);

            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f; // win over any scene volume

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(overrides: true);
            bloom.intensity.Override(Intensity);
            bloom.threshold.Override(Threshold);
            bloom.scatter.Override(Scatter);
        }

        // The gameplay scenes render through a Cinemachine main camera that may not have
        // post-processing enabled; turn it on wherever it isn't (idempotent).
        private static void EnableOnActiveCameras()
        {
            foreach (var cam in Camera.allCameras)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }
        }
    }
}

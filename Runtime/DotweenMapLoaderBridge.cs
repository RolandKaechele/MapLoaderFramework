#if MAPLOADER_DOTWEEN
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// Optional bridge that hooks <see cref="MapLoaderManager.TransitionCallback"/> so that
    /// every map/chapter transition is wrapped in a DOTween-driven full-screen fade rather than
    /// an instant switch.
    /// Enable define <c>MAPLOADER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Assign <see cref="fadeOverlay"/> to a full-screen black <see cref="Image"/> sitting above
    /// all gameplay content. The bridge will fade it to opaque, call the actual map load, then
    /// fade back to transparent — matching the same pattern used by <see cref="FadeController"/>
    /// in CutsceneManager.
    /// </para>
    /// </summary>
    [AddComponentMenu("MapLoaderFramework/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenMapLoaderBridge : MonoBehaviour
    {
        [Header("Fade Overlay")]
        [Tooltip("Full-screen black Image overlay used for map transition fades.")]
        [SerializeField] private Image fadeOverlay;

        [Tooltip("Duration for the fade-to-black before the map loads.")]
        [SerializeField] private float fadeOutDuration = 0.4f;

        [Tooltip("Duration for the fade-to-clear after the map finishes loading.")]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Tooltip("DOTween ease applied to fade-out (to black).")]
        [SerializeField] private Ease fadeOutEase = Ease.InSine;

        [Tooltip("DOTween ease applied to fade-in (to clear).")]
        [SerializeField] private Ease fadeInEase = Ease.OutSine;

        // -------------------------------------------------------------------------

        private MapLoaderManager _mlm;

        private void Awake()
        {
            _mlm = GetComponent<MapLoaderManager>() ?? FindFirstObjectByType<MapLoaderManager>();
            if (_mlm == null)
            {
                Debug.LogWarning("[MapLoaderFramework/DotweenMapLoaderBridge] MapLoaderManager not found.");
                return;
            }

            if (fadeOverlay != null)
            {
                var c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;
            }
        }

        private void OnEnable()
        {
            if (_mlm != null)
                _mlm.TransitionCallback = HandleTransition;
        }

        private void OnDisable()
        {
            if (_mlm != null && _mlm.TransitionCallback == (Action<string, Action>)HandleTransition)
                _mlm.TransitionCallback = null;
        }

        // -------------------------------------------------------------------------

        private void HandleTransition(string displayName, Action doLoad)
        {
            if (fadeOverlay == null)
            {
                // No overlay assigned — just call the load immediately.
                doLoad?.Invoke();
                return;
            }

            DOTween.Kill(fadeOverlay);

            var seq = DOTween.Sequence();

            // Fade to black.
            seq.Append(fadeOverlay.DOFade(1f, fadeOutDuration).SetEase(fadeOutEase));

            // Perform the actual map load in the middle.
            seq.AppendCallback(() => doLoad?.Invoke());

            // Fade back to clear.
            seq.Append(fadeOverlay.DOFade(0f, fadeInDuration).SetEase(fadeInEase));
        }
    }
}
#else
namespace MapLoaderFramework.Runtime
{
    /// <summary>No-op stub — enable define <c>MAPLOADER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("MapLoaderFramework/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenMapLoaderBridge : UnityEngine.MonoBehaviour { }
}
#endif

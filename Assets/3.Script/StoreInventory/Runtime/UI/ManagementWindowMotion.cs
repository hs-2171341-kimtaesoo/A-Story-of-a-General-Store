using UnityEngine;

namespace GeneralStore.Inventory
{
    public static class WindowSlideTiming
    {
        public static float Ease(float elapsed,float seconds)
        {float t=Mathf.Clamp01(elapsed/Mathf.Max(.05f,seconds));return 1-(1-t)*(1-t)*(1-t);}
    }
    // Only the moving window runs this tween; item cards and static lists are untouched.
    [DisallowMultipleComponent]
    public sealed class ManagementWindowMotion : MonoBehaviour
    {
        public RectTransform frame;
        public bool fromBelow;
        [Min(.05f)] public float duration = .3f;
        Vector2 target;
        float elapsed, progress, closeStart;
        bool hasTarget, closing, deactivateOnClose, inputBlocked, previousRaycasts, previousInteractable;
        CanvasGroup input;
        public bool IsClosed { get; private set; }
        public bool IsTransitioning{get{return hasTarget&&isActiveAndEnabled&&!IsClosed&&elapsed<Mathf.Max(.05f,duration);}}
        public bool IsClosing{get{return closing;}}

        public static void CloseWindow(GameObject window)
        {
            if(window==null||!window.activeSelf)return;
            var menu=window.transform.root.GetComponentInChildren<RightSideMenu>(true);
            if(menu!=null&&menu.CloseWindow(window))return;
            var motion=window.GetComponent<ManagementWindowMotion>();
            if(motion==null)
            {
                var moving=window.transform.Find("Panel Frame") as RectTransform;
                motion=Ensure(window,moving!=null?moving:window.transform as RectTransform);
                motion.progress=1;motion.elapsed=Mathf.Max(.05f,motion.duration);motion.Apply();
            }
            motion.BeginClose(true);
        }

        public static ManagementWindowMotion Ensure(GameObject window, RectTransform moving, bool below = false)
        {
            var motion = window.GetComponent<ManagementWindowMotion>();
            if (motion == null) motion = window.AddComponent<ManagementWindowMotion>();
            motion.frame = moving;
            motion.fromBelow = below;
            if (!motion.hasTarget) motion.SetTarget(moving.anchoredPosition);
            return motion;
        }

        public void SetTarget(Vector2 position)
        {
            target = position;
            hasTarget = true;
            if (Application.isPlaying && isActiveAndEnabled && progress < 1) BlockInput();
            Apply();
        }

        public void BeginClose(bool deactivate=false)
        {
            deactivateOnClose|=deactivate;if (closing) return;
            if(!hasTarget&&frame!=null)SetTarget(frame.anchoredPosition);
            closing = true; elapsed = 0; closeStart = progress; IsClosed = frame == null || !isActiveAndEnabled;
            BlockInput();
        }

        void OnEnable() { elapsed = progress = 0; closing = IsClosed = deactivateOnClose = false; if (Application.isPlaying) BlockInput(); Apply(); }
        void OnDisable() { RestoreInput(); if (frame != null && hasTarget) frame.anchoredPosition = target; }
        void LateUpdate() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float deltaTime)
        {
            float seconds = Mathf.Max(.05f, duration);
            if (!hasTarget || frame == null || IsClosed || elapsed >= seconds) return;
            elapsed = Mathf.Min(seconds, elapsed + Mathf.Max(0, deltaTime));
            float t = WindowSlideTiming.Ease(elapsed,seconds);
            progress = closing ? closeStart * (1 - t) : t;
            Apply();
            if (elapsed >= seconds) { if (closing){IsClosed=true;if(deactivateOnClose)gameObject.SetActive(false);} else RestoreInput(); }
        }

        void BlockInput()
        {
            if (frame == null || inputBlocked) return;
            input = frame.GetComponent<CanvasGroup>();
            if (input == null) input = frame.gameObject.AddComponent<CanvasGroup>();
            previousRaycasts = input.blocksRaycasts; previousInteractable = input.interactable;
            input.blocksRaycasts = input.interactable = false; inputBlocked = true;
        }
        void RestoreInput()
        {
            if (!inputBlocked) return;
            if (input != null) { input.blocksRaycasts = previousRaycasts; input.interactable = previousInteractable; }
            inputBlocked = false;
        }

        void Apply()
        {
            if (!hasTarget || frame == null) return;
            var parent = frame.parent as RectTransform;
            if (!Application.isPlaying || !isActiveAndEnabled || parent == null)
            { frame.anchoredPosition = target; return; }
            Vector2 hidden = target;
            // Convert parent-space edges to anchored coordinates, including non-central pivots.
            Vector2 anchor = (Vector2)frame.localPosition - frame.anchoredPosition;
            if (fromBelow)
                hidden.y = parent.rect.yMin - anchor.y - frame.rect.height * Mathf.Abs(frame.localScale.y) * (1 - frame.pivot.y) - 24;
            else
                hidden.x = parent.rect.xMin - anchor.x - frame.rect.width * Mathf.Abs(frame.localScale.x) * (1 - frame.pivot.x) - 24;
            Vector2 position = Vector2.LerpUnclamped(hidden, target, progress);
            if (frame.anchoredPosition != position) frame.anchoredPosition = position;
        }
    }
}

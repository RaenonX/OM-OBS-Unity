using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace OM.OBS
{
    public class Marquee : MonoBehaviour
    {
        [System.NonSerialized]
        private List<MarqueeSource> Sources = new List<MarqueeSource>();
        [System.NonSerialized]
        private MarqueeSource CurrentSource;
        [System.NonSerialized]
        private int CurrentIndexInSource;
        [System.NonSerialized]
        private float Timestamp;
        [System.NonSerialized]
        private string CurrentContent;

        [System.Serializable] public class ContentChangeEvent : UnityEvent<string> { }

        [SerializeField]
        public float RefreshRate;
        [SerializeField]
        public ContentChangeEvent ContentChanged;

        private void OnEnable()
        {
            Sources.Clear();
            GetComponentsInChildren(false, Sources);
            foreach (var src in Sources)
            {
                src.ContentChanged += OnContentChanged;
            }
            StartCoroutine(Looper());
        }

        private IEnumerator Looper()
        {
            RefreshContent(true);
            while (true)
            {    
                yield return null;

                if (Time.realtimeSinceStartup - Timestamp > RefreshRate)
                {
                    ForwardContent();
                }
            }
        }

        private void OnContentChanged(MarqueeSource src)
        {
            if (src == CurrentSource)
            {
                RefreshContent(true);
            }
        }

        private void SetContent(string content)
        {
            if (CurrentContent == content)
                return;

            Timestamp = Time.realtimeSinceStartup;
            CurrentContent = content;
            ContentChanged?.Invoke(content);
        }

        private void RefreshContent(bool autoForward)
        {
            if (CurrentSource &&
                CurrentIndexInSource < CurrentSource.Lines?.Count)
            {
                SetContent(CurrentSource.Lines[CurrentIndexInSource]);
            }
            else if (autoForward)
            {
                ForwardContent();
            }
            else
            {
                SetContent(string.Empty);
            }
        }

        private void ForwardContent()
        {
            // validate all instance
            Sources.RemoveAll(ms => ms && !ms.enabled);
            if (Sources.Count == 0)
            {
                SetContent(string.Empty);
                return;
            }

            // ensure CurrentSource has instance
            if (CurrentSource == null)
                CurrentSource = Sources[0];
            // forward index of lines
            else
                ++CurrentIndexInSource;

            // forward line if any more
            if (CurrentSource &&
                CurrentIndexInSource < CurrentSource.Lines?.Count)
            {
                RefreshContent(false);
            }
            // otherwise forward instance
            else
            {
                int srcIdx = Sources.IndexOf(CurrentSource);
                srcIdx = (srcIdx + 1) % Sources.Count;
                CurrentSource = Sources[srcIdx];
                CurrentIndexInSource = 0;
                RefreshContent(false);
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace OM.OBS
{
    public class MarqueeSource : MonoBehaviour
    {
        [SerializeField]
        public List<string> Lines;

        public event System.Action<MarqueeSource> ContentChanged;

        protected void SetContent(IEnumerable<string> lines)
        {
            Lines = Lines ?? new List<string>();
            Lines.Clear();
            Lines.AddRange(lines);
            ContentChanged?.Invoke(this);
        }

        protected void SetContent(string line)
        {
            Lines = Lines ?? new List<string>();
            Lines.Clear();
            Lines.Add(line);
            ContentChanged?.Invoke(this);
        }
    }
}

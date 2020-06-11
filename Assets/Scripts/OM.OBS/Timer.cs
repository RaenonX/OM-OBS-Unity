using System;
using UnityEngine;
using UnityEngine.Events;

namespace OM.OBS
{
    class Timer : MonoBehaviour
    {
        [System.Serializable]
        public class OnTimeChangedEvent : UnityEvent<string> { }

        [SerializeField]
        private OnTimeChangedEvent _TimeChanged;

        [System.NonSerialized]
        private DateTime _LastUpdate;

        private void Update()
        {
            var now = DateTime.Now;
            if (_LastUpdate.Second != now.Second)
            {
                _LastUpdate = now;

                _TimeChanged?.Invoke(now.ToString("HH:mm:ss"));
            }
        }
    }
}

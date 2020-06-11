using System;
using UnityEngine;
using UnityEngine.Events;

namespace OM.OBS
{
    class Timer : MonoBehaviour
    {
        [Serializable]
        public struct TimeZone
        {
            public string Location;
            public int UTCHoursModifier;
            public int UTCMinutesModifier;
        }

        [SerializeField]
        public TimeZone[] Locations;

        [Serializable]
        public class OnTimeChangedEvent : UnityEvent<string> { }

        [SerializeField]
        private float _TimeZoneChangeDuration;
        [SerializeField]
        private OnTimeChangedEvent _TimeChanged;
        [SerializeField]
        private OnTimeChangedEvent _TimeZoneChanged;

        [NonSerialized]
        private DateTime _LastTimeZoneUpdated;
        [NonSerialized]
        private DateTime _LastTimeUpdated;
        [NonSerialized]
        public int _CurrentTimeZoneIndex;

        private void Update()
        {
            var now = DateTime.UtcNow;
            if (_LastTimeUpdated.Second != now.Second)
            {
                _LastTimeUpdated = now;
                if (Locations?.Length > 0)
                {
                    TimeZone loc;
                    if (now - _LastTimeZoneUpdated > TimeSpan.FromSeconds(_TimeZoneChangeDuration))
                    {
                        _CurrentTimeZoneIndex = (_CurrentTimeZoneIndex + 1) % Locations.Length;
                        _LastTimeZoneUpdated = now;
                        loc = Locations[_CurrentTimeZoneIndex];
                        _TimeZoneChanged?.Invoke(loc.Location);
                    }
                    else
                    {
                        loc = Locations[_CurrentTimeZoneIndex];
                    }
                    now += new TimeSpan(loc.UTCHoursModifier, loc.UTCMinutesModifier, 0);
                }
                _TimeChanged?.Invoke(now.ToString("HH:mm:ss"));
            }
        }
    }
}

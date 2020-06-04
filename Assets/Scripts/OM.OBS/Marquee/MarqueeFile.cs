using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OM.OBS
{
    public class MarqueeFile : MarqueeSource
    {
        #region Serializable Fields

        [SerializeField]
        public string WatchFile;

        #endregion

        #region Fields

        [System.NonSerialized]
        private FileSystemWatcher Watcher;

        #endregion

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                LoadAsync();
            }
        }

        private async void LoadAsync()
        {
            var lines = await Task.Run(() => File.ReadAllLines(WatchFile));
            SetContent(lines);
        }

        private void OnEnable()
        {
            if (Watcher == null)
            {
                if (File.Exists(WatchFile))
                {
                    WatchFile = Path.GetFullPath(WatchFile);
                    Watcher = new FileSystemWatcher();
                    Watcher.Path = Path.GetDirectoryName(WatchFile);
                    Watcher.Filter = Path.GetFileName(WatchFile);
                    Watcher.NotifyFilter = NotifyFilters.LastWrite;
                    Watcher.Changed += Watcher_Changed;
                    Watcher.EnableRaisingEvents = true;
                }
            }
            LoadAsync();
        }

        private void OnDisable()
        {
            if (Watcher != null)
            {
                Watcher.Dispose();
                Watcher = null;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying &&
                enabled)
            {
                OnDisable();
                OnEnable();
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace OM.OBS
{
    class MarqueeYoutubeViewers : MarqueeSource
    {
        #region Response Structures

        [Serializable]
        public struct Response
        {
            public ResponseItem[] items;
        }

        [Serializable]
        public struct ResponseItem
        {
            public LiveStreamingDetails liveStreamingDetails;
        }

        [Serializable]
        public struct LiveStreamingDetails
        {
            public string concurrentViewers;
        }

        #endregion

        [SerializeField]
        public string API_URL;
        [SerializeField]
        public string Key;
        [SerializeField]
        public string VideoID;
        [SerializeField]
        public string DisplayFormat;
        [SerializeField]
        public float QueryFrequency;

        private void OnEnable()
        {
            StartCoroutine(Get());
        }

        private IEnumerator Get()
        {
            while (true)
            {
                var uri = $"{API_URL}?part=liveStreamingDetails&id={VideoID}&key={Key}&fields=items%2FliveStreamingDetails%2FconcurrentViewers";
                using (var request = UnityWebRequest.Get(uri))
                {
                    yield return request.SendWebRequest();

                    if (request.isNetworkError ||
                        request.isHttpError)
                    {
                        Debug.LogError($"[MarqueeYoutubeWatchers] {request.error}");
                    }

                    var content = DownloadHandlerBuffer.GetContent(request);
                    try
                    {
                        var resp = JsonUtility.FromJson<Response>(content);
                        if (resp.items?.Length > 0)
                        {
                            var viewers = int.Parse(resp.items[0].liveStreamingDetails.concurrentViewers);
                            if (viewers > 0)
                            {
                                SetContent(string.Format(DisplayFormat, viewers));
                            }
                            else
                            {
                                ClearContent();
                            }
                        }
                        else
                        {
                            Debug.LogError($"[MarqueeYoutubeWatchers] invalid content: {content}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        ClearContent();
                    }
                }
                yield return new WaitForSecondsRealtime(QueryFrequency);
            }
        }
    }
}

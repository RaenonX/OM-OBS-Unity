
using UnityEngine;
using TMPro;
using UnityEngine.Playables;

namespace OM.OBS
{
    public class AnimatedText : MonoBehaviour
    {
        public PlayableDirector Director;
        public TextMeshProUGUI AppearText;
        public TextMeshProUGUI DissolveText;

        public string text
        {
            set
            {
                if (isActiveAndEnabled)
                {
                    DissolveText.text = AppearText.text;
                    AppearText.text = value;
                    Director.time = 0;
                    Director.Play();
                }
                else
                {
                    DissolveText.text = value;
                    AppearText.text = value;
                }
            }
        }
    }
}

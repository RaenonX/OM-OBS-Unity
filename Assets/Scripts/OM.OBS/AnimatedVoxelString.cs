using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace OM.OBS
{
    [ExecuteInEditMode]
    public class AnimatedVoxelString : MonoBehaviour
    {
        [SerializeField] public Texture2D FontTexture;
        [SerializeField] public Vector2Int FontSize;
        [SerializeField] public float Space;
        [SerializeField] public Transform RotateAxis;
        [SerializeField] public float AnimateDuration = 1f;

        [SerializeField] public VoxelCharacter CharPrefab;
        [SerializeField] public string _Text;
        [SerializeField] TextAlignment Alignment;

        [NonSerialized] private List<VoxelCharacter> _FreeCharacters;
        [NonSerialized] private List<VoxelCharacter> _Characters;
        [NonSerialized] private List<VoxelCharacter> _FadeoutCharacters;

        [NonSerialized]
        private Dictionary<char, List<Vector4>> _CharactersCache = new Dictionary<char, List<Vector4>>(128);

        public string text
        {
            get => _Text;
            set
            {
                if (value != _Text)
                {
                    _Text = value;
                    UpdateCharacters();
                }
            }
        }

        private void OnEnable()
        {
            UpdateCharacters();
        }

        private void OnDisable()
        {
            if (_Characters != null)
            {
                _Characters.ForEach(vc => Destroy(vc.gameObject));
                _Characters.Clear();
            }
            if (_FreeCharacters != null)
            {
                _FreeCharacters.ForEach(vc => Destroy(vc.gameObject));
                _FreeCharacters.Clear();
            }
        }

        private VoxelCharacter NewFadeinCharacter(char newChar)
        {
            var fadeinC = NewCharacter(newChar);
            fadeinC.RotateEffect = VoxelCharacter.EffectKind.Forward;
            fadeinC.ScaleEffect = VoxelCharacter.EffectKind.Forward;

            if (Application.isPlaying)
            {
                fadeinC.Time = 0;    
                var fadeinT = DOTween.To(() => fadeinC.Time, (v) => fadeinC.Time = v, 1, AnimateDuration);
                fadeinT.target = this;
            }
            else
            {
                fadeinC.Time = 1;
            }
            return fadeinC;
        }

        private void FadeoutCharacter(VoxelCharacter fadeoutC)
        {
            if (Application.isPlaying)
            {
                fadeoutC.Time = 1;
                fadeoutC.RotateEffect = VoxelCharacter.EffectKind.Backward;
                fadeoutC.ScaleEffect = VoxelCharacter.EffectKind.Backward;
                var fadeoutT = DOTween.To(() => fadeoutC.Time, (v) => fadeoutC.Time = v, 0, AnimateDuration);
                fadeoutT.target = this;
                fadeoutT.onComplete += () => OnFadeoutCharacterCompleted(fadeoutC);

                if (_FadeoutCharacters == null)
                    _FadeoutCharacters = new List<VoxelCharacter>();

                _FadeoutCharacters.Add(fadeoutC);
            }
            else
            {
                FreeCharacter(fadeoutC);
            }
        }

        private void FreeCharacter(VoxelCharacter vc)
        {
            if (_FreeCharacters == null)
                _FreeCharacters = new List<VoxelCharacter>(128);
            _FreeCharacters.Add(vc);
        }

        private void OnFadeoutCharacterCompleted(VoxelCharacter fadeoutC)
        {
            _FadeoutCharacters.Remove(fadeoutC);
            FreeCharacter(fadeoutC);
        }

        private VoxelCharacter Transform(VoxelCharacter fadeoutC, char newChar)
        {
            if (fadeoutC == null)
            {
                return NewFadeinCharacter(newChar);
            }
            if (fadeoutC.Character != newChar)
            {
                FadeoutCharacter(fadeoutC);
                var fadeinC = NewFadeinCharacter(newChar);
                return fadeinC;
            }
            else
            {
                return fadeoutC;
            }
        }

        private void UpdateCharacters()
        {
            int used = 0;
            if (_Characters != null)
            {
                if (!string.IsNullOrEmpty(_Text))
                {
                    used = Mathf.Min(_Text.Length, _Characters.Count);
                    for (int i = 0; i < used; ++i)
                    {
                        var fadeoutC = _Characters[i];
                        var newChar = _Text[i];
                        _Characters[i] = Transform(fadeoutC, newChar);
                    }
                }
                if (used < _Characters.Count)
                {
                    for (int i = used; i < _Characters.Count; ++i)
                        FreeCharacter(_Characters[i]);
                    _Characters.RemoveRange(used, _Characters.Count - used);
                }
            }

            if (string.IsNullOrEmpty(_Text))
                return;
            if (_Characters == null)
                _Characters = new List<VoxelCharacter>(_Text.Length);
            while (used < _Text.Length)
                _Characters.Add(NewFadeinCharacter(_Text[used++]));

            // update positions
            Vector3 pos;
            Vector3 space = new Vector4(Space, 0, 0);
            switch (Alignment)
            {
                default:
                case TextAlignment.Left:
                    pos = new Vector4(0, 0, 0);
                    break;
                case TextAlignment.Center:
                    pos = new Vector4(_Text.Length * Space * -0.5f, 0, 0);
                    break;
                case TextAlignment.Right:
                    pos = new Vector4(_Text.Length * -Space, 0, 0);
                    break;
            }
            for (int i = 0; i < _Characters.Count; ++i)
            {
                var vc = _Characters[i];
                vc.transform.localPosition = pos;
                pos += space;
            }
        }

        private VoxelCharacter NewCharacter(char c)
        {
            if (_FreeCharacters != null)
            {
                for (int i = 0; i < _FreeCharacters.Count; ++i)
                {
                    if (_FreeCharacters[i].Character == c)
                    {
                        var vc = _FreeCharacters[i];
                        _FreeCharacters.RemoveAt(i);

                        if (!vc.IsValid())
                            vc.Assign(c, GetCharacterVoxelPositions(c));

                        return vc;
                    }
                }
            }

            var vp = GetCharacterVoxelPositions(c);
            if (vp != null)
            {
                var vc = Instantiate(CharPrefab, transform);
                vc.gameObject.hideFlags = HideFlags.HideAndDontSave;
                vc.Assign(c, vp);
                return vc;
            }

            return null;
        }

        private void OnValidate()
        {
            UpdateCharacters();
        }

        private void Update()
        {
            Vector3 forward = RotateAxis ? RotateAxis.forward : transform.forward;

            if (_FadeoutCharacters != null)
            {
                for (int i = 0; i < _FadeoutCharacters.Count; ++i)
                {
                    var ch = _FadeoutCharacters[i];
                    ch.RotateAxis = forward;
                    ch.Render();
                }
            }
            if (_Characters != null)
            {
                for (int i = 0; i < _Characters.Count; ++i)
                {
                    var ch = _Characters[i];
                    ch.RotateAxis = forward;
                    ch.Render();
                }
            }
        }

        public List<Vector4> GetCharacterVoxelPositions(char c)
        {
            if (c == 0)
                return null;

            if (_CharactersCache.TryGetValue(c, out var result))
                return result;

            int charactersPerLine = FontTexture.width / FontSize.x;
            int texHeight = FontTexture.height;
            int lines = texHeight / FontSize.y;
            int numCharacters = charactersPerLine * lines;
            int maxVoxels = FontSize.x * FontSize.y;

            if (c >= numCharacters)
                return null;

            result = new List<Vector4>(maxVoxels);
            Vector2Int indexOffset = new Vector2Int
            {
                x = ((c - 1) % charactersPerLine) * FontSize.x,
                y = ((lines - 1) - (c - 1) / charactersPerLine) * FontSize.y
            };

            Vector4 offset = new Vector4(-FontSize.x * 0.5f, -FontSize.y * 0.5f);
            for (int y = 0; y < FontSize.y; ++y)
            {
                for (int x = 0; x < FontSize.x; ++x)
                {
                    Color color = FontTexture.GetPixel(
                        indexOffset.x + x,
                        indexOffset.y + y);
                    if (color.grayscale >= 0.5f)
                    {
                        result.Add(new Vector4(x, y, 0f, 1f) + offset);
                    }
                }
            }

            return result;
        }
    }
}

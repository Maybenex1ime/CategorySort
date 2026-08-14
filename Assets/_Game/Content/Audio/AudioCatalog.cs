using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LogosGame.Features.Gameplay.Content.Audio
{
    [CreateAssetMenu(fileName = "SO_AudioCatalog", menuName = "WordStack/Audio Catalog")]
    public class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private string _clipId;
            [SerializeField] private AssetReferenceT<AudioClip> _clipRef;

            public string ClipId => _clipId;
            public AssetReferenceT<AudioClip> ClipRef => _clipRef;
        }

        [Header("SFX")]
        [SerializeField] private Entry[] _sfx = Array.Empty<Entry>();

        [Header("Music")]
        [SerializeField] private Entry[] _music = Array.Empty<Entry>();

        public IReadOnlyList<Entry> SfxEntries => _sfx;
        public IReadOnlyList<Entry> MusicEntries => _music;

#if UNITY_INCLUDE_TESTS
        internal void SetEntries_ForTest(Entry[] sfx, Entry[] music)
        {
            _sfx = sfx ?? Array.Empty<Entry>();
            _music = music ?? Array.Empty<Entry>();
        }
#endif
    }
}

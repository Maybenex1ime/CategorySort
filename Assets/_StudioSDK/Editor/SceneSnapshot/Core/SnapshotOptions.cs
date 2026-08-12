#if UNITY_EDITOR
using System;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot
{
    [Serializable]
    public class SnapshotOptions
    {
        public bool includeDontDestroyOnLoad = false;
        public bool stripMissingScripts = true;
        public bool clearReflexContainerRefs = true;
        public bool clearR3References = true;
        public bool killDOTweenTweens = true;
        public bool clearAddressablesHandles = true;

        public Predicate<GameObject> rootFilter = null;

        public HideFlags excludedHideFlags = HideFlags.DontSave | HideFlags.HideAndDontSave;

        public static SnapshotOptions Default => new SnapshotOptions();
    }
}
#endif

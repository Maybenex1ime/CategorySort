#if UNITY_EDITOR
using System.Collections.Generic;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public class SnapshotResult
    {
        public bool Success;
        public string SavedPath;
        public int RootCount;
        public int GameObjectCount;
        public int StrippedMissingScripts;
        public int ClearedReferences;
        public List<string> Warnings = new List<string>();
        public string ErrorMessage;

        /// <summary>
        /// True when the snapshot was staged as a prefab during Play Mode and the
        /// final .unity scene will be materialized on Play Mode exit.
        /// </summary>
        public bool PendingMaterialize;
    }
}
#endif

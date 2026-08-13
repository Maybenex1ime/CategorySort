#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public interface ISnapshotPostProcessor
    {
        string Name { get; }
        bool ShouldRun(SnapshotOptions options);
        void Process(GameObject clone, SnapshotResult result);
    }
}
#endif

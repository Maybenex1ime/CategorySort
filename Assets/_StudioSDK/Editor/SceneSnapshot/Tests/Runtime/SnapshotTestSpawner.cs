using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot.Tests
{
    /// <summary>
    /// Runtime helper for the SceneSnapshot acceptance tests.
    /// Spawns N cubes at random positions and starts a LitMotion rotation tween
    /// on the first cube to verify the snapshot carries no running tween.
    /// </summary>
    public sealed class SnapshotTestSpawner : MonoBehaviour
    {
        [SerializeField] private int _cubeCount = 50;
        [SerializeField] private float _spawnRange = 10f;
        [SerializeField] private bool _spawnTween = true;

        private void Start()
        {
            var parent = new GameObject("Cubes").transform;
            for (int i = 0; i < _cubeCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Cube_{i:00}";
                go.transform.SetParent(parent, worldPositionStays: true);
                go.transform.position = new Vector3(
                    Random.Range(-_spawnRange, _spawnRange),
                    Random.Range(0f, _spawnRange),
                    Random.Range(-_spawnRange, _spawnRange));
                go.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);

                if (_spawnTween && i == 0)
                {
                    // DORotate(0,360,0, FastBeyond360).SetLoops(-1): không SetEase → OutQuad.
                    LMotion.Create(go.transform.eulerAngles.y, 360f, 2f)
                        .WithEase(Ease.OutQuad)
                        .WithLoops(-1)
                        .BindToEulerAnglesY(go.transform)
                        .AddTo(go);
                }
            }
        }
    }
}

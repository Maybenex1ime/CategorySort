using System.Collections.Generic;
using LogosGame.Features.Cheat.Services;
using LogosGame.Features.Gameplay.Content;
using Reflex.Attributes;
using UnityEngine;

namespace LogosGame.Features.Cheat.Views
{
    /// <summary>
    /// BOOSTERS section: instantiates one <see cref="CheatBoosterRowView"/> per
    /// entry in <see cref="CheatSettings.BoosterEntries"/>. The section root
    /// hides itself when the SO list is empty so the UI stays clean.
    /// </summary>
    public sealed class CheatBoostersSectionView : MonoBehaviour
    {
        [Inject] private readonly ICheatService _cheatService;
        [Inject] private readonly CheatSettings _cheatSettings;

        [SerializeField] private CheatBoosterRowView _rowPrefab;
        [SerializeField] private Transform _rowContainer;
        [SerializeField] private GameObject _sectionRoot;

        private readonly List<CheatBoosterRowView> _rows = new List<CheatBoosterRowView>();

        private void Start()
        {
            if (_cheatSettings == null || _cheatSettings.BoosterEntries == null || _cheatSettings.BoosterEntries.Count == 0)
            {
                if (_sectionRoot != null) _sectionRoot.SetActive(false);
                else gameObject.SetActive(false);
                return;
            }

            if (_rowPrefab == null || _rowContainer == null)
                return;

            foreach (CheatSettings.BoosterEntry entry in _cheatSettings.BoosterEntries)
            {
                CheatBoosterRowView row = Instantiate(_rowPrefab, _rowContainer);
                row.Bind(_cheatService, entry.Id, entry.DisplayName);
                _rows.Add(row);
            }
        }
    }
}

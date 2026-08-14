using System;
using R3;
using UnityEngine;

namespace LogosGame.Features.UI.Popups.Args
{
    public sealed class BoosterPurchasePopupArgs
    {
        public Sprite Icon { get; set; }
        public string BoosterName { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public ReadOnlyReactiveProperty<int> Coins { get; set; }
        public Action OnPurchaseConfirmed { get; set; }
        public Action OnClose { get; set; }
    }
}

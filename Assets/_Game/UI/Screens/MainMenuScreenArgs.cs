using System;
using WordStack.Contracts;

namespace LogosGame.Features.UI.Screens
{
    public sealed class MainMenuScreenArgs
    {
        public string LevelTitle { get; set; }
        public LevelDifficulty Difficulty { get; set; } = LevelDifficulty.Normal;
        public Action OnStartLevel { get; set; }
        public Action OnOpenSettings { get; set; }
        /// <summary>Nút "+" ở ô tim: mở NoHeartsPopup (chỉ hiện khi tim chưa đầy).</summary>
        public Action OnOpenHearts { get; set; }
    }
}

namespace LogosSDK.Audio
{
    // Chọn AudioSource cho SFX mới: ưu tiên source đang rảnh; hết thì cướp source đã phát
    // lâu nhất (playOrder nhỏ nhất). Port thuật toán AudioRunner.PlaySFX (Mukbang / Everest).
    public static class SfxVoicePicker
    {
        public static int Pick(bool[] busy, ulong[] playOrder)
        {
            for (int i = 0; i < busy.Length; i++)
            {
                if (!busy[i]) return i;
            }

            int oldest = 0;
            for (int i = 1; i < playOrder.Length; i++)
            {
                if (playOrder[i] < playOrder[oldest]) oldest = i;
            }
            return oldest;
        }
    }
}

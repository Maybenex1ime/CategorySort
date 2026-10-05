using NUnit.Framework;

namespace LogosSDK.Audio.Tests.EditMode
{
    [Category("UnitTest")]
    public class SfxVoicePickerTests
    {
        [Test]
        public void CoSourceRanh_LaySourceRanhDauTien()
        {
            var busy = new[] { true, false, false };
            var order = new ulong[] { 5, 1, 2 };
            Assert.That(SfxVoicePicker.Pick(busy, order), Is.EqualTo(1));
        }

        [Test]
        public void TatCaBan_CuopSourcePhatLauNhat()
        {
            var busy = new[] { true, true, true };
            var order = new ulong[] { 7, 3, 9 };
            Assert.That(SfxVoicePicker.Pick(busy, order), Is.EqualTo(1));
        }

        [Test]
        public void TatCaBan_ThuTuBangNhau_LayIndexNhoHon()
        {
            var busy = new[] { true, true };
            var order = new ulong[] { 4, 4 };
            Assert.That(SfxVoicePicker.Pick(busy, order), Is.EqualTo(0));
        }

        [Test]
        public void SourceRanhUuTienHonSourceCu()
        {
            // source 0 cũ nhất nhưng đang bận, source 2 rảnh → lấy 2, không cướp 0
            var busy = new[] { true, true, false };
            var order = new ulong[] { 1, 8, 9 };
            Assert.That(SfxVoicePicker.Pick(busy, order), Is.EqualTo(2));
        }
    }
}

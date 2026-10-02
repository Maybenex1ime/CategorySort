using System;
using LogosSDK.Core.Logging;
using LogosSDK.Save;
using R3;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGame.Features.Shop.Impl
{
    public sealed class NoAdsService : INoAdsService, IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetLogger<NoAdsService>();

        private readonly ISaveManager _save;
        private readonly NoAdsData _data;
        private readonly ReactiveProperty<bool> _isNoAds;

        public NoAdsService(ISaveManager save)
        {
            _save = save;
            _data = _save.Load<NoAdsData>() ?? new NoAdsData();
            _isNoAds = new ReactiveProperty<bool>(_data.Owned);
        }

        public ReadOnlyReactiveProperty<bool> IsNoAds => _isNoAds;

        public void Grant()
        {
            if (_data.Owned) return;

            _data.Owned = true;
            // Ghi NGAY: ShopService.Fulfill chỉ trả true (store xác nhận đơn) sau dòng này.
            _save.SaveImmediate(_data);
            _isNoAds.Value = true;
            _logger.Info("[NoAdsService] Đã bật No-Ads.");
        }

        public void Dispose() => _isNoAds.Dispose();
    }
}

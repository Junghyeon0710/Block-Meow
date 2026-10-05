using UnityEngine;
using UnityEngine.UI;

namespace BlockMeow
{
    /// <summary>Header bar used by sub screens: back button, title and the coin chip.</summary>
    public static class Header
    {
        public static Text Build(Transform parent, string title, System.Action onBack, out Text coins)
        {
            var bar = UIKit.Rect("Header", parent).Top(0, 150);
            var back = UIKit.Round(bar, "ic_left", Pal.Panel3, onBack, 0.5f);
            ((RectTransform)back.transform).At(0f, 0.5f, 30, 0, 110, 110);
            var t = UIKit.Txt(bar, title, 64, Color.white);
            t.rectTransform.Fill(160, 0, 160, 0);
            coins = UIKit.CoinChip(bar, out var chip, () => UIRoot.I.ShowShop());
            chip.At(1f, 0.5f, -24, 0, 300, 96);
            return t;
        }
    }
}

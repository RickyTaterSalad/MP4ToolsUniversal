using System;

namespace MP4ToolsLib
{
    /// <summary>
    /// Intro drawtext sizes are tuned for 4K (3840×2160). Sub-4K sources use <see cref="DefaultSub4KFontScale"/>.
    /// </summary>
    public static class IntroVideoFontScaling
    {
        public const int ReferenceWidth4K = 3840;
        public const int ReferenceHeight4K = 2160;
        public const int ReferenceLineGap4K = 36;

        /// <summary>Font scale for resolutions below 4K (e.g. 2.7K / 2688×1512). Adjust to test sizing.</summary>
        public const double DefaultSub4KFontScale = 0.75;

        public static double GetFontScale(int width, int height, double sub4KFontScale = DefaultSub4KFontScale)
        {
            if (width <= 0 || height <= 0)
                return 1.0;
            if (width >= ReferenceWidth4K && height >= ReferenceHeight4K)
                return 1.0;
            return sub4KFontScale;
        }

        public static int ScaleFontSize(int baseSize, double scale) =>
            Math.Max(1, (int)Math.Round(baseSize * scale, MidpointRounding.AwayFromZero));

        public static int ScaleLineGap(double scale) =>
            Math.Max(1, (int)Math.Round(ReferenceLineGap4K * scale, MidpointRounding.AwayFromZero));
    }
}

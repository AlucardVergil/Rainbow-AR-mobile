using System;
using UnityEngine;

namespace Cortex
{
    namespace ColorExtensionMethods
    {
        /// <summary>
        /// Helper methods for colors
        /// </summary>
        public static class ColorUtils
        {
            /// <summary>
            /// Clamps the given color to the range [0,1] for all components
            /// </summary>
            /// <param name="col">The color itself</param>
            /// <returns>A new color that is a brighter version of the given color</returns>
            public static Color ClampColor(this Color col, float min = 0.0f, float max = 1.0f)
            {
                return new Color(
                    Math.Clamp(col.r, min, max),
                    Math.Clamp(col.g, min, max),
                    Math.Clamp(col.b, min, max),
                    Math.Clamp(col.a, min, max));
            }
            /// <summary>
            /// Linear transformation of a color. For each component c, aside from alpha, this will compute scale*c + amplitude.
            /// The result is clamped.
            /// </summary>
            /// <param name="col">The color itself</param>
            /// <param name="scale">A scaling factor</param>
            /// <param name="amplitude">An additive component</param>
            /// <returns>A new color that is an adjusted version of the given color</returns>
            public static Color AdjustColor(this Color col, float scale, float amplitude)
            {
                return ClampColor(new Color(
                    col.r * scale + amplitude,
                    col.g * scale + amplitude,
                    col.b * scale + amplitude,
                    col.a));
            }

            /// <summary>
            /// Brightens the given color
            /// </summary>
            /// <param name="col">The color itself</param>
            /// <returns>A new color that is a brighter version of the given color</returns>
            public static Color Brighten(this Color col)
            {
                return AdjustColor(col, 1.35f, 0.35f);
            }
            /// <summary>
            /// Darkens the given color
            /// </summary>
            /// <param name="col">The color itself</param>
            /// <returns>A new color that is a darker version of the given color</returns>
            public static Color Darken(this Color col)
            {
                // darken feels stronger than brightening, so we do it asymmetrically
                return AdjustColor(col, 0.65f, -0.25f);
            }

            /// <summary>
            /// Inverts the color. For each component c, aside from alpha, this will compute 1-c.
            /// </summary>
            /// <param name="col"></param>
            /// <returns>A new color that is the inverse of the given color</returns>
            public static Color Invert(this Color col)
            {
                return new Color(1.0f - col.r, 1.0f - col.g, 1.0f - col.b, col.a);
            }

        }
    }
} // end namespace Cortex
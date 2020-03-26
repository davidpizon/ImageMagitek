﻿  using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ImageMagitek
{
    public class ElementCopier
    {
        public static MagitekResult CanCopyElements(Arranger source, Arranger dest, Point sourceStart, Point destStart, int copyWidth, int copyHeight)
        {
            if (copyWidth > (source.ArrangerElementSize.Width - sourceStart.X))
                return new MagitekResult.Failed($"Source arranger '{source.Name}' with width ({source.ArrangerElementSize.Width}) is insufficient to copy {copyWidth} elements starting from position {sourceStart.X}");

            if (copyHeight > (source.ArrangerElementSize.Height - sourceStart.Y))
                return new MagitekResult.Failed($"Source arranger '{source.Name}' with height ({source.ArrangerElementSize.Height}) is insufficient to copy {copyHeight} elements starting from position {sourceStart.Y}");

            if (copyWidth > (dest.ArrangerElementSize.Width - destStart.X))
                return new MagitekResult.Failed($"Destination arranger '{dest.Name}' with width ({dest.ArrangerElementSize.Width}) is insufficient to copy {copyWidth} elements starting from position {destStart.X}");
            
            if (copyHeight > (dest.ArrangerElementSize.Height - destStart.Y))
                return new MagitekResult.Failed($"Destination arranger '{dest.Name}' with height ({dest.ArrangerElementSize.Height}) is insufficient to copy {copyHeight} elements starting from position {destStart.Y}");

            if (source.ElementPixelSize != dest.ElementPixelSize)
                return new MagitekResult.Failed($"Source arranger '{dest.Name}' with element size ({source.ElementPixelSize.Width}, {source.ElementPixelSize.Height}) does not match destination arranger '{dest.Name}' with element size ({dest.ElementPixelSize.Width}, {dest.ElementPixelSize.Height})");

            if (dest.Layout != ArrangerLayout.Tiled)
                return new MagitekResult.Failed($"Destination arranger '{dest.Name}' is not a tiled layout");

            if (source.ColorType != dest.ColorType)
                return new MagitekResult.Failed($"Source arranger '{source.Name}' ColorType {source.ColorType} does not match destination arranger ColorType {dest.ColorType}");

            return new MagitekResult.Success();
        }

        /// <summary>
        /// Copies elements from the source Arranger to destination ScatteredArranger
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="sourceStart">Starting point of source arranger in element coordinates</param>
        /// <param name="destStart">Starting point of destination arranger in element coordinates</param>
        /// <param name="copyWidth">Width of copy in elements</param>
        /// <param name="copyHeight">Height of copy in elements</param>
        public static MagitekResult CopyElements(Arranger source, ScatteredArranger dest, Point sourceStart, Point destStart, int copyWidth, int copyHeight)
        {
            var result = CanCopyElements(source, dest, sourceStart, destStart, copyWidth, copyHeight);
            
            if (result.Value is MagitekResult.Success)
                CopyElementsInternal(source, dest, sourceStart, destStart, copyWidth, copyHeight);

            return result;

            void CopyElementsInternal(Arranger source, ScatteredArranger dest, Point sourceStart, Point destStart, int copyWidth, int copyHeight)
            {
                for (int y = 0; y < copyHeight; y++)
                {
                    for (int x = 0; x < copyWidth; x++)
                    {
                        var el = source.GetElement(x + sourceStart.X, y + sourceStart.Y);
                        dest.SetElement(el, x + destStart.X, y + destStart.Y);
                    }
                }
            }
        }
    }
}

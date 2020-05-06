﻿using System;
using System.Linq;
using ImageMagitek.Colors;

namespace ImageMagitek.Codec
{
    public class BlankDirectCodec : DirectCodec
    {
        public override string Name => "Blank Direct";
        public override int Width { get; } = 8;
        public override int Height { get; } = 8;
        public override ImageLayout Layout => ImageLayout.Tiled;
        public override int ColorDepth => 0;
        public override int StorageSize => 0;

        public override int RowStride => 0;
        public override int ElementStride => 0;
        public override bool CanResize => true;
        public override int WidthResizeIncrement => 1;
        public override int HeightResizeIncrement => 1;
        public override int DefaultWidth => 8;
        public override int DefaultHeight => 8;

        private static ColorRgba32 _defaultFillColor = new ColorRgba32(0, 0, 0, 255);
        private ColorRgba32 _fillColor;

        public BlankDirectCodec() : this(_defaultFillColor) { }

        public BlankDirectCodec(ColorRgba32 fillColor)
        {
            _fillColor = fillColor;
            _foreignBuffer = Enumerable.Empty<byte>().ToArray();
        }

        public override ColorRgba32[,] DecodeElement(in ArrangerElement el, ReadOnlySpan<byte> encodedBuffer)
        {
            if (_nativeBuffer?.GetLength(0) != el.Width || _nativeBuffer?.GetLength(1) != el.Height)
            {
                _nativeBuffer = new ColorRgba32[el.Width, el.Height];

                for (int y = 0; y < el.Height; y++)
                    for (int x = 0; x < el.Width; x++)
                        _nativeBuffer[x, y] = _fillColor;
            }

            return NativeBuffer;
        }

        public override ReadOnlySpan<byte> EncodeElement(in ArrangerElement el, ColorRgba32[,] imageBuffer) => ForeignBuffer;
        public override ReadOnlySpan<byte> ReadElement(in ArrangerElement el) => ForeignBuffer;
        public override void WriteElement(in ArrangerElement el, ReadOnlySpan<byte> encodedBuffer) { }
    }
}

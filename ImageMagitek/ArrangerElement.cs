﻿using System;
using System.Collections.Generic;
using System.Linq;
using ImageMagitek.Codec;
using ImageMagitek.Project;

namespace ImageMagitek
{
    /// <summary>
    /// Contains all necessary data to encode/decode a single element in the arranger
    /// </summary>
    public class ArrangerElement
    {
        /// <summary>
        /// Gets or sets the parent Arranger for the Element
        /// </summary>
        public Arranger Parent { get; set; }

        /// <summary>
        /// DataFile which contains the element's pixel data
        /// </summary>
        public DataFile DataFile { get; set; }

        /// <summary>
        /// DataFile key in FileManager
        /// </summary>
        public string DataFileKey { get; set; }

        /// <summary>
        /// FileAddress of Element
        /// </summary>
        public FileBitAddress FileAddress { get; set; }

        /// <summary>
        /// GraphicsFormat name for encoding/decoding the Element
        /// </summary>
        public string FormatName { get; set; }

        public IGraphicsCodec Codec { get; set; }

        /// <summary>
        /// Width of Element in pixels
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of Element in pixels
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Palette to apply to the element's pixel data
        /// </summary>
        public Palette Palette { get; set; }

        /// <summary>
        /// Palette key in FileManager
        /// </summary>
        public string PaletteKey { get; set; }

        /// <summary>
        /// Left edge of the Element within the Arranger in unzoomed coordinates, inclusive
        /// </summary>
        public int X1 { get; set; }

        /// <summary>
        /// Top edge of the Element within the Arranger in unzoomed coordinates, inclusive
        /// </summary>
        public int Y1 { get; set; }

        /// <summary>
        /// Right edge of the Element within the Arranger in unzoomed coordinates, inclusive
        /// </summary>
        public int X2 { get => X1 + Width - 1; }

        /// <summary>
        /// Bottom edge of the Element within the Arranger in unzoomed coordinates, inclusive
        /// </summary>
        public int Y2 { get => Y1 + Height - 1; }

        public ArrangerElement()
        {
            DataFileKey = "";
            FileAddress = new FileBitAddress(0, 0);
            FormatName = "";
            Width = 0;
            Height = 0;
            PaletteKey = "Default";
            X1 = 0;
            Y1 = 0;

            Codec = new BlankCodec();
        }

        /// <summary>
        /// Creates a deep clone
        /// </summary>
        /// <returns></returns>
        public ArrangerElement Clone()
        {
            ArrangerElement el = new ArrangerElement()
            {
                Parent = Parent,
                DataFile = DataFile,
                DataFileKey = DataFileKey,
                FileAddress = FileAddress,
                FormatName = FormatName,
                Width = Width,
                Height = Height,
                Palette = Palette,
                PaletteKey = PaletteKey,
                X1 = X1,
                Y1 = Y1
            };

            return el;
        }
    }
}

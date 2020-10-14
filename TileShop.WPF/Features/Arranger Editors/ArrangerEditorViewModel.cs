﻿using System;
using System.Windows;
using System.Linq;
using Stylet;
using TileShop.WPF.Behaviors;
using TileShop.Shared.EventModels;
using TileShop.WPF.Models;
using TileShop.Shared.Models;
using TileShop.WPF.Imaging;
using GongSolutions.Wpf.DragDrop;
using ImageMagitek.Services;
using ImageMagitek;
using ImageMagitek.Codec;

namespace TileShop.WPF.ViewModels
{
    public enum EditMode { ArrangeGraphics, ModifyGraphics }

    public abstract class ArrangerEditorViewModel : ResourceEditorBaseViewModel, IMouseCaptureProxy, IDropTarget, IDragSource
    {
        protected Arranger _workingArranger;

        protected IEventAggregator _events;
        protected IPaletteService _paletteService;
        protected IWindowManager _windowManager;

        private BitmapAdapter _bitmapAdapter;
        public BitmapAdapter BitmapAdapter
        {
            get => _bitmapAdapter;
            set => SetAndNotify(ref _bitmapAdapter, value);
        }

        public bool IsSingleLayout => _workingArranger?.Layout == ArrangerLayout.Single;
        public bool IsTiledLayout => _workingArranger?.Layout == ArrangerLayout.Tiled;

        protected bool _showGridlines = false;
        public bool ShowGridlines
        {
            get => _showGridlines;
            set => SetAndNotify(ref _showGridlines, value);
        }

        protected BindableCollection<Gridline> _gridlines;
        public BindableCollection<Gridline> Gridlines
        {
            get => _gridlines;
            set => SetAndNotify(ref _gridlines, value);
        }

        protected int _zoom = 1;
        public int Zoom
        {
            get => _zoom;
            set
            {
                if (SetAndNotify(ref _zoom, value))
                    CreateGridlines();
            }
        }

        public int MinZoom => 1;
        public int MaxZoom { get; protected set; } = 16;

        public bool CanChangeSnapMode { get; protected set; }

        protected EditMode _editMode = EditMode.ArrangeGraphics;
        public EditMode EditMode
        {
            get => _editMode;
            set => SetAndNotify(ref _editMode, value);
        }

        public virtual bool CanEditSelection
        {
            get
            {
                if (Selection.HasSelection)
                {
                    var rect = Selection.SelectionRect;
                    if (rect.SnappedWidth == 0 || rect.SnappedHeight == 0)
                        return false;

                    return !_workingArranger.EnumerateElementsByPixel(rect.SnappedLeft, rect.SnappedTop, rect.SnappedWidth, rect.SnappedHeight)
                        .Any(x => x is null || x?.DataFile is null);
                }
                    
                return false;
            }
        }

        private SnapMode _snapMode = SnapMode.Element;
        public SnapMode SnapMode
        {
            get => _snapMode;
            set
            {
                SetAndNotify(ref _snapMode, value);
                if (Selection is object)
                    Selection.SnapMode = SnapMode;
            }
        }

        private ArrangerSelection _selection;
        public ArrangerSelection Selection
        {
            get => _selection;
            set => SetAndNotify(ref _selection, value);
        }

        private bool _isSelecting;
        public bool IsSelecting
        {
            get => _isSelecting;
            set => SetAndNotify(ref _isSelecting, value);
        }

        public bool CanAcceptPixelPastes { get; set; }
        public bool CanAcceptElementPastes { get; set; }

        private ArrangerPaste _paste;
        public ArrangerPaste Paste
        {
            get => _paste;
            set => SetAndNotify(ref _paste, value);
        }

        public ArrangerEditorViewModel(IEventAggregator events, IWindowManager windowManager, IPaletteService paletteService) 
        {
            _events = events;
            _events.Subscribe(this);
            _windowManager = windowManager;
            _paletteService = paletteService;
        }

        /// <summary>
        /// Redraws the ImageBase onto the BitmapAdapter
        /// </summary>
        public abstract void Render();

        /// <summary>
        /// Applies the paste to the Editor
        /// </summary>
        /// <param name="paste"></param>
        public abstract void ApplyPaste(ArrangerPaste paste);

        /// <summary>
        /// Creates the Gridlines for an overlay using the extents and element spacing of the Working Arranger
        /// </summary>
        protected virtual void CreateGridlines()
        {
            if (_workingArranger is object)
                CreateGridlines(0, 0, _workingArranger.ArrangerPixelSize.Width, _workingArranger.ArrangerPixelSize.Height,
                    _workingArranger.ElementPixelSize.Width, _workingArranger.ElementPixelSize.Height);
        }

        /// <summary>
        /// Creates the Gridlines for an overlay
        /// </summary>
        /// <param name="x">Starting x-coordinate in pixel coordinates, inclusive</param>
        /// <param name="y">Starting y-coordinate in pixel coordinates, inclusive</param>
        /// <param name="x2">Ending x-coordinate in pixel coordinates, inclusive</param>
        /// <param name="y2">Ending y-coordinate in pixel coordinates, inclusive</param>
        /// <param name="xSpacing">Spacing between gridlines in pixel coordinates</param>
        /// <param name="height">Spacing between gridlines in pixel coordinates</param>
        protected void CreateGridlines(int x1, int y1, int x2, int y2, int xSpacing, int ySpacing)
        {
            if (_workingArranger is null)
                return;

            _gridlines = new BindableCollection<Gridline>();
            for (int x = x1; x <= x2; x += xSpacing) // Vertical gridlines
            {
                var gridline = new Gridline(x * Zoom + 1, 0,
                    x * Zoom + 1, y2 * Zoom);
                _gridlines.Add(gridline);
            }

            for (int y = y1; y <= y2; y += ySpacing) // Horizontal gridlines
            {
                var gridline = new Gridline(0, y * Zoom + 1,
                    x2 * Zoom, y * Zoom + 1);
                _gridlines.Add(gridline);
            }

            NotifyOfPropertyChange(() => Gridlines);
        }

        #region Commands
        public virtual void ZoomIn() => Zoom = Math.Clamp(Zoom + 1, MinZoom, MaxZoom);
        public virtual void ZoomOut() => Zoom = Math.Clamp(Zoom - 1, MinZoom, MaxZoom);

        public virtual void ToggleGridlineVisibility() => ShowGridlines ^= true;

        public virtual void EditSelection()
        {
            if (!CanEditSelection)
                return;

            EditArrangerPixelsEvent editEvent;
            var rect = Selection.SelectionRect;

            if (SnapMode == SnapMode.Element && _workingArranger.Layout == ArrangerLayout.Tiled)
            {
                // Clone a subsection of the arranger and show the full subarranger
                _workingArranger.CopyElements();
                var arranger = _workingArranger.CloneArranger(rect.SnappedLeft, rect.SnappedTop, rect.SnappedWidth, rect.SnappedHeight);
                editEvent = new EditArrangerPixelsEvent(arranger, Resource as Arranger, 0, 0, rect.SnappedWidth, rect.SnappedHeight);
            }
            else
            {
                // Clone the entire arranger and show a subsection of the cloned arranger
                var arranger = _workingArranger.CloneArranger();
                editEvent = new EditArrangerPixelsEvent(arranger, Resource as Arranger, rect.SnappedLeft, rect.SnappedTop, rect.SnappedWidth, rect.SnappedHeight);
            }

            _events.PublishOnUIThread(editEvent);
            CancelOverlay();
        }

        public virtual void SelectAll()
        {
            CancelOverlay();
            Selection = new ArrangerSelection(_workingArranger, SnapMode);
            Selection.StartSelection(0, 0);
            Selection.UpdateSelectionEndpoint(_workingArranger.ArrangerPixelSize.Width, _workingArranger.ArrangerPixelSize.Height);
        }

        public virtual void CancelOverlay()
        {
            Selection = new ArrangerSelection(_workingArranger, SnapMode);
            Paste = null;

            NotifyOfPropertyChange(() => CanEditSelection);
        }

        public virtual void StartNewSelection(int x, int y)
        {
            Selection.StartSelection(x, y);
            IsSelecting = true;
        }

        public virtual void UpdateSelection(int x, int y)
        {
            if (IsSelecting)
                Selection.UpdateSelectionEndpoint(x, y);
        }

        public virtual void CompleteSelection()
        {
            if (IsSelecting)
            {
                if (Selection.SelectionRect.SnappedWidth == 0 || Selection.SelectionRect.SnappedHeight == 0)
                {
                    Selection = new ArrangerSelection(_workingArranger, SnapMode);
                }

                IsSelecting = false;
                NotifyOfPropertyChange(() => CanEditSelection);
            }
        }
        #endregion

        #region Mouse Actions
        public virtual void OnMouseMove(object sender, MouseCaptureArgs e)
        {
            int x = (int)(e.X / Zoom);
            int y = (int)(e.Y / Zoom);

            if (IsSelecting)
                UpdateSelection(x, y);

            if (Selection.HasSelection)
            {
                string notifyMessage;
                var rect = Selection.SelectionRect;
                if (rect.SnapMode == SnapMode.Element)
                    notifyMessage = $"Element Selection: {rect.SnappedWidth / _workingArranger.ElementPixelSize.Width} x {rect.SnappedHeight / _workingArranger.ElementPixelSize.Height}" +
                        $" at ({rect.SnappedLeft / _workingArranger.ElementPixelSize.Width}, {rect.SnappedRight / _workingArranger.ElementPixelSize.Height})";
                else
                    notifyMessage = $"Pixel Selection: {rect.SnappedWidth} x {rect.SnappedHeight}" +
                        $" at ({rect.SnappedLeft}, {rect.SnappedTop})";
                var notifyEvent = new NotifyStatusEvent(notifyMessage, NotifyStatusDuration.Indefinite);
                _events.PublishOnUIThread(notifyEvent);
            }
            else
            {
                var notifyMessage = $"{_workingArranger.Name}: ({(int)Math.Truncate(e.X / Zoom)}, {(int)Math.Truncate(e.Y / Zoom)})";
                var notifyEvent = new NotifyStatusEvent(notifyMessage, NotifyStatusDuration.Indefinite);
                _events.PublishOnUIThread(notifyEvent);
            }
        }

        public virtual void OnMouseLeave(object sender, MouseCaptureArgs e)
        {
            var notifyEvent = new NotifyStatusEvent("", NotifyStatusDuration.Indefinite);
            _events.PublishOnUIThread(notifyEvent);
        }

        public virtual void OnMouseUp(object sender, MouseCaptureArgs e)
        {
            if (IsSelecting)
            {
                CompleteSelection();
            }
        }

        public virtual void OnMouseDown(object sender, MouseCaptureArgs e)
        {
            int x = (int)(e.X / Zoom);
            int y = (int)(e.Y / Zoom);

            if (e.LeftButton && Paste is object && !Paste.Rect.ContainsPointSnapped(x, y))
            {
                ApplyPaste(Paste);
                Paste = null;
            }

            if (Selection?.HasSelection is true && e.LeftButton && Selection.SelectionRect.ContainsPointSnapped(x, y))
            {
                // Start drag for selection (Handled by DragDrop in View)
            }
            else if (Paste is object && e.LeftButton && Paste.Rect.ContainsPointSnapped(x, y))
            {
                // Start drag for paste (Handled by DragDrop in View)
            }
            else if (e.LeftButton)
            {
                StartNewSelection(x, y);
            }
        }

        public virtual void OnMouseWheel(object sender, MouseCaptureArgs e)
        {
            if (e.WheelDelta > 0)
                ZoomIn();
            else
                ZoomOut();
        }

        #pragma warning disable CS0067
        // Unused events that are required to be present for IMouseCaptureProxy
        public virtual event EventHandler Capture;
        public virtual event EventHandler Release;
        #pragma warning restore CS0067
        #endregion

        #region Drag and Drop Implementation
        public virtual void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is ArrangerPaste paste)
            {
                paste.SnapMode = SnapMode;
                Paste = paste;
                Paste.MoveTo((int)dropInfo.DropPosition.X, (int)dropInfo.DropPosition.Y);
            }
        }

        public virtual void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is ArrangerPaste paste)
            {
                if (paste.Copy is ElementCopy && !CanAcceptElementPastes)
                    return;

                if ((paste.Copy is IndexedPixelCopy || paste.Copy is DirectPixelCopy) && !CanAcceptPixelPastes)
                    return;

                if (!ReferenceEquals(dropInfo.DragInfo.SourceItem, this))
                    (dropInfo.DragInfo.SourceItem as ArrangerEditorViewModel).CancelOverlay();

                if (Paste != paste)
                {
                    Paste = new ArrangerPaste(paste.Copy, SnapMode)
                    {
                        DeltaX = paste.DeltaX,
                        DeltaY = paste.DeltaY
                    };
                }

                Paste.MoveTo((int)dropInfo.DropPosition.X, (int)dropInfo.DropPosition.Y);
                dropInfo.Effects = DragDropEffects.Copy | DragDropEffects.Move;
            }
        }

        public virtual void StartDrag(IDragInfo dragInfo)
        {
            if (Selection.HasSelection)
            {
                var rect = Selection.SelectionRect;

                ArrangerCopy copy = default;
                if (SnapMode == SnapMode.Element)
                {
                    int x = rect.SnappedLeft / _workingArranger.ElementPixelSize.Width;
                    int y = rect.SnappedTop / _workingArranger.ElementPixelSize.Height;
                    int width = rect.SnappedWidth / _workingArranger.ElementPixelSize.Width;
                    int height = rect.SnappedHeight / _workingArranger.ElementPixelSize.Height;
                    copy = _workingArranger.CopyElements(x, y, width, height);
                }
                else if (SnapMode == SnapMode.Pixel && _workingArranger.ColorType == PixelColorType.Indexed)
                {
                    copy = _workingArranger.CopyPixelsIndexed(rect.SnappedLeft, rect.SnappedTop, rect.SnappedWidth, rect.SnappedHeight);
                }
                else if (SnapMode == SnapMode.Pixel && _workingArranger.ColorType == PixelColorType.Direct)
                {
                    copy = _workingArranger.CopyPixelsDirect(rect.SnappedLeft, rect.SnappedTop, rect.SnappedWidth, rect.SnappedHeight);
                }

                var paste = new ArrangerPaste(copy, SnapMode)
                {
                    DeltaX = (int)dragInfo.DragStartPosition.X - Selection.SelectionRect.SnappedLeft,
                    DeltaY = (int)dragInfo.DragStartPosition.Y - Selection.SelectionRect.SnappedTop
                };
                dragInfo.Data = paste;
                dragInfo.Effects = DragDropEffects.Copy | DragDropEffects.Move;

                Selection = new ArrangerSelection(_workingArranger, SnapMode);
            }
            else if (Paste is object)
            {
                Paste.DeltaX = (int)dragInfo.DragStartPosition.X - Paste.Rect.SnappedLeft;
                Paste.DeltaY = (int)dragInfo.DragStartPosition.Y - Paste.Rect.SnappedTop;
                Paste.SnapMode = SnapMode;

                dragInfo.Data = Paste;
                dragInfo.Effects = DragDropEffects.Copy | DragDropEffects.Move;
            }
        }

        public virtual bool CanStartDrag(IDragInfo dragInfo)
        {
            if (Selection.HasSelection)
            {
                return Selection.SelectionRect.ContainsPointSnapped(dragInfo.DragStartPosition.X, dragInfo.DragStartPosition.Y);
            }
            else if (Paste is object)
            {
                return Paste.Rect.ContainsPointSnapped(dragInfo.DragStartPosition.X, dragInfo.DragStartPosition.Y);
            }
            else
                return false;
        }

        public virtual void Dropped(IDropInfo dropInfo) { }

        public virtual void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo) { }

        public virtual void DragCancelled()
        {
            CancelOverlay();
        }
        public virtual bool TryCatchOccurredException(Exception exception) => false;
        #endregion
    }
}

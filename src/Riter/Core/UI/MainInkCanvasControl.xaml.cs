﻿using System.Windows.Controls;
using System.Windows.Ink;
using Microsoft.Extensions.DependencyInjection;
using Riter.Core.Consts;
using Riter.Core.Drawing;
using Riter.Core.Enum;
using Riter.Core.Interfaces;
using Riter.ViewModel;

namespace Riter.Core.UI;

public partial class MainInkCanvasControl : UserControl
{
    private readonly IStrokeHistoryService _strokeHistoryService;
    private readonly IInkEditingModeStateHandler _inkEditingModeStateHandler;
    private readonly Dictionary<DrawingShape, IShapeDrawer> _shapeDrawers;
    private bool _isDrawing = false;
    private Point _startPoint;
    private Stroke _lastStroke;

    public MainInkCanvasControl()
    {
        InitializeComponent();
        _strokeHistoryService = App.ServiceProvider.GetService<IStrokeHistoryService>();
        _inkEditingModeStateHandler = App.ServiceProvider.GetService<IInkEditingModeStateHandler>();
        _shapeDrawers = App.ServiceProvider.GetService<IEnumerable<IShapeDrawer>>().ToDictionary(drawer => drawer.SupportedShape);
        _strokeHistoryService.SetMainElementToRedoAndUndo(MainInkCanvas);
        MainInkCanvas.Strokes.StrokesChanged += StrokesChanged;

        MainInkCanvas.MouseLeftButtonDown += StartDrawing;
        MainInkCanvas.MouseLeftButtonUp += EndDrawing;
        MainInkCanvas.MouseMove += DrawShape;
    }

    private static bool IsDrawingShapeKeyEntered(Key key) => key == Key.LeftShift || key == Key.RightShift;

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (IsDrawingShapeKeyEntered(e.Key) && _inkEditingModeStateHandler.InkEditingMode is InkCanvasEditingMode.Ink)
        {
            _inkEditingModeStateHandler.None();
            MainInkCanvas.UseCustomCursor = true;
        }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        var vm = (PaletteStateOrchestratorViewModel)DataContext;
        if (IsDrawingShapeKeyEntered(e.Key)
            && _inkEditingModeStateHandler.InkEditingMode is InkCanvasEditingMode.None
            && (vm.ButtonSelectedViewModel.ButtonSelectedName == ButtonNames.DrawingButton || vm.ButtonSelectedViewModel.ButtonSelectedName == ButtonNames.HighlighterButton))
        {
            _inkEditingModeStateHandler.Ink();
            MainInkCanvas.UseCustomCursor = false;
        }
    }

    private void StartDrawing(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = true;
        _startPoint = e.GetPosition(MainInkCanvas);
        _lastStroke = null;
        _strokeHistoryService.IgnoreStrokesChange = true;
    }

    private void EndDrawing(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawing && _lastStroke != null)
            _strokeHistoryService.Push(StrokesHistoryNode.CreateAddedType([_lastStroke]));

        _isDrawing = false;
        _strokeHistoryService.IgnoreStrokesChange = false;
    }

    private void DrawShape(object sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;

        var viewModel = (PaletteStateOrchestratorViewModel)DataContext;
        var currentShape = viewModel.DrawingViewModel.CurrentShape;
        if (viewModel.ButtonSelectedViewModel.ButtonSelectedName == ButtonNames.DrawingButton || viewModel.ButtonSelectedViewModel.ButtonSelectedName == ButtonNames.HighlighterButton)
        {
            currentShape = DrawingShape.Line;
        }

        if (_shapeDrawers.TryGetValue(currentShape, out var drawer))
        {
            var endPoint = e.GetPosition(MainInkCanvas);
            var stroke = drawer.DrawShape(
                MainInkCanvas,
                _startPoint,
                endPoint,
                viewModel.BrushSettingsViewModel.IsRainbow);

            if (_lastStroke != null)
                MainInkCanvas.Strokes.Remove(_lastStroke);

            MainInkCanvas.Strokes.Add(stroke);
            _lastStroke = stroke;
        }
    }

    /// <summary>
    /// Handles the StrokesChanged event when the user draws on the InkCanvas.
    /// This method will be used to track and store stroke changes in a stack for history purposes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Contains the stroke collection that has changed.</param>
    private void StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
    {
        if (_strokeHistoryService.IgnoreStrokesChange)
        {
            return;
        }

        if (e.Added.Count != 0)
        {
            _strokeHistoryService.Push(StrokesHistoryNode.CreateAddedType(e.Added));
        }

        if (e.Removed.Count != 0)
        {
            _strokeHistoryService.Push(StrokesHistoryNode.CreateRemovedType(e.Removed));
        }
    }
}

//using System.Windows.Controls;
//using System.Windows.Ink;
//using Microsoft.Extensions.DependencyInjection;
//using Riter.Core.Consts;
//using Riter.Core.Drawing;
//using Riter.Core.Enum;
//using Riter.Core.Interfaces;
//using Riter.ViewModel;

//namespace Riter.Core.UI;

//public partial class MainInkCanvasControl : UserControl
//{
//    private readonly IStrokeHistoryService _strokeHistoryService;
//    private readonly IInkEditingModeStateHandler _inkEditingModeStateHandler;
//    private readonly Dictionary<DrawingShape, IShapeDrawer> _shapeDrawers;
//    private bool _isDrawing;
//    private Point _startPoint;
//    private Stroke _lastStroke;

//    public MainInkCanvasControl()
//    {
//        InitializeComponent();
//        _strokeHistoryService = App.ServiceProvider.GetRequiredService<IStrokeHistoryService>();
//        _inkEditingModeStateHandler = App.ServiceProvider.GetRequiredService<IInkEditingModeStateHandler>();
//        _shapeDrawers = App.ServiceProvider.GetRequiredService<IEnumerable<IShapeDrawer>>()
//            .ToDictionary(drawer => drawer.SupportedShape);

//        InitializeInkCanvas();
//    }

//    private void InitializeInkCanvas()
//    {
//        _strokeHistoryService.SetMainElementToRedoAndUndo(MainInkCanvas);
//        MainInkCanvas.Strokes.StrokesChanged += HandleStrokesChanged;

//        MainInkCanvas.MouseLeftButtonDown += StartDrawing;
//        MainInkCanvas.MouseLeftButtonUp += EndDrawing;
//        MainInkCanvas.MouseMove += DrawShape;
//    }

//    private void StartDrawing(object sender, MouseButtonEventArgs e)
//    {
//        _isDrawing = true;
//        _startPoint = e.GetPosition(MainInkCanvas);
//        _lastStroke = null;
//        _strokeHistoryService.IgnoreStrokesChange = true;
//    }

//    private void EndDrawing(object sender, MouseButtonEventArgs e)
//    {
//        if (_isDrawing && _lastStroke != null)
//        {
//            AddStrokeToHistory(_lastStroke);
//        }

//        _isDrawing = false;
//        _strokeHistoryService.IgnoreStrokesChange = false;
//    }

//    private void AddStrokeToHistory(Stroke stroke)
//        => _strokeHistoryService.Push(StrokesHistoryNode.CreateAddedType([stroke]));

//    private void DrawShape(object sender, MouseEventArgs e)
//    {
//        if (!_isDrawing) return;

//        var viewModel = (PaletteStateOrchestratorViewModel)DataContext;
//        var currentShape = GetCurrentShape(viewModel);

//        if (_shapeDrawers.TryGetValue(currentShape, out var drawer))
//        {
//            var endPoint = e.GetPosition(MainInkCanvas);
//            var stroke = drawer.DrawShape(MainInkCanvas, _startPoint, endPoint, viewModel.BrushSettingsViewModel.IsRainbow);

//            UpdateCanvasStroke(stroke);
//        }
//    }

//    private static DrawingShape GetCurrentShape(PaletteStateOrchestratorViewModel viewModel) => viewModel.ButtonSelectedViewModel.ButtonSelectedName is ButtonNames.DrawingButton or ButtonNames.HighlighterButton
//            ? DrawingShape.Line
//            : viewModel.DrawingViewModel.CurrentShape;

//    private void UpdateCanvasStroke(Stroke newStroke)
//    {
//        if (_lastStroke != null)
//        {
//            MainInkCanvas.Strokes.Remove(_lastStroke);
//        }

//        MainInkCanvas.Strokes.Add(newStroke);
//        _lastStroke = newStroke;
//    }

//    private void HandleStrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
//    {
//        if (_strokeHistoryService.IgnoreStrokesChange) return;

//        if (e.Added.Count > 0)
//        {
//            _strokeHistoryService.Push(StrokesHistoryNode.CreateAddedType(e.Added));
//        }

//        if (e.Removed.Count > 0)
//        {
//            _strokeHistoryService.Push(StrokesHistoryNode.CreateRemovedType(e.Removed));
//        }
//    }

//    private static bool IsDrawingShapeKey(Key key) =>
//        key is Key.LeftShift or Key.RightShift;

//    private void Window_KeyDown(object sender, KeyEventArgs e)
//    {
//        if (IsDrawingShapeKey(e.Key) && _inkEditingModeStateHandler.InkEditingMode == InkCanvasEditingMode.Ink)
//        {
//            SwitchToNoneMode();
//        }
//    }

//    private void Window_KeyUp(object sender, KeyEventArgs e)
//    {
//        var viewModel = (PaletteStateOrchestratorViewModel)DataContext;
//        if (IsDrawingShapeKey(e.Key) &&
//            _inkEditingModeStateHandler.InkEditingMode == InkCanvasEditingMode.None &&
//            IsDrawingOrHighlighterButtonSelected(viewModel))
//        {
//            SwitchToInkMode();
//        }
//    }

//    private void SwitchToNoneMode()
//    {
//        _inkEditingModeStateHandler.None();
//        MainInkCanvas.UseCustomCursor = true;
//    }

//    private void SwitchToInkMode()
//    {
//        _inkEditingModeStateHandler.Ink();
//        MainInkCanvas.UseCustomCursor = false;
//    }

//    private static bool IsDrawingOrHighlighterButtonSelected(PaletteStateOrchestratorViewModel viewModel) =>
//        viewModel.ButtonSelectedViewModel.ButtonSelectedName is ButtonNames.DrawingButton or ButtonNames.HighlighterButton;
//}

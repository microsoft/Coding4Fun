//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace FaceTheremin
{
    public sealed partial class MainPage
    {
        // Dimensions of the display area
        private const int CellsRowsCount = 8;
        private const int CellsColumnsCount = 6;

        // All the rectangles of the cells
        private readonly Rectangle[,] _cellRectangles = new Rectangle[CellsRowsCount, CellsColumnsCount];
        // Predefined XAML rectangles for highlighting faces
        private Rectangle[] _faceRectangles;

        // Brush for grid borders
        private readonly SolidColorBrush _cellStrokeBrush = new SolidColorBrush(Colors.Red);
        // Brush for background of cell with detected face
        private readonly SolidColorBrush _currentCellFillBrush = new SolidColorBrush(Color.FromArgb(0x3F, 0xFF, 0x00, 0x00));

        private AudioMatrix _audioMatrix;
        private FaceMatrix _faceMatrix;

        public MainPage()
        {
            InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            InitializeVisuals();

            await InitializeVideoAsync();

            _audioMatrix = await AudioMatrix.CreateAsync(CellsRowsCount, CellsColumnsCount);

            _faceMatrix.Frame += (sender, args) =>
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                {
                    RenderFaceRectangles(args.PreviewFrameSize, args.DetectedFaces);

                    // remove background for cells from previous frame
                    SetCellsFill(args.PreviousFaceCells, null);
                    // set background for cells with faces on the current frame
                    SetCellsFill(args.NewFaceCells, _currentCellFillBrush);

                    // and play corresponding sound
                    _audioMatrix.PlayCells(args.NewFaceCells);
                });
            };
        }

        /// <summary>
        /// Initialize CellsGrid and _cellRectangles
        /// </summary>
        /// <returns></returns>
        private void InitializeVisuals()
        {
            for (var i = 0; i < CellsRowsCount; i++)
            {
                CellsGrid.RowDefinitions.Add(new RowDefinition());
            }

            for (var i = 0; i < CellsColumnsCount; i++)
            {
                CellsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (var y = 0; y < CellsRowsCount; y++)
            {
                for (var x = 0; x < CellsColumnsCount; x++)
                {
                    var rectangle = new Rectangle
                    {
                        Stroke = _cellStrokeBrush,
                        StrokeThickness = 1.0,
                    };
                    Grid.SetRow(rectangle, y);
                    Grid.SetColumn(rectangle, x);
                    CellsGrid.Children.Add(rectangle);
                    _cellRectangles[y, x] = rectangle;
                }
            }

            _faceRectangles = FaceRectanglesCanvas.Children.Cast<Rectangle>().ToArray();
        }

        /// <summary>
        /// Initialize webcam, set source for StreamElement and start process of face detection
        /// </summary>
        /// <returns></returns>
        private async Task InitializeVideoAsync()
        {
            StatusTextBlock.Text = "Opening video stream...";
            try
            {
                // we only need video from our webcam
                var settings = new MediaCaptureInitializationSettings {StreamingCaptureMode = StreamingCaptureMode.Video};
                var mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(settings);

                // set source to see ourselves on the screen
                StreamingElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();

                // start face detection for the first time
                _faceMatrix = await FaceMatrix.CreateAsync(mediaCapture, CellsRowsCount, CellsColumnsCount);
                StatusTextBlock.Text = "Live video";
            }
            catch (UnauthorizedAccessException)
            {
                //We'll get this if the webcam is disabled, or inaccessible.
                StatusTextBlock.Text = "Unable to access the webcam. Ensure that it is connected, and that the app has permission to access it. In addition, ensure that \"Webcam\" is declared in the AppManifest's Capabilities.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Start webcam error: {ex}";
            }
        }

        /// <summary>
        /// Draw rectangles for each detected face
        /// </summary>
        /// <param name="previewFrameSize">Webcam resolution</param>
        /// <param name="faces">Detected faces</param>
        private void RenderFaceRectangles(Size previewFrameSize, IList<DetectedFace> faces)
        {
            // calculate values to scale face rectangles correctly
            var widthScale = previewFrameSize.Width / FaceRectanglesCanvas.ActualWidth;
            var heightScale = previewFrameSize.Height / FaceRectanglesCanvas.ActualHeight;

            for (var i = 0; i < _faceRectangles.Length; i++)
            {
                // get one of the predefined rectangles for the face highlighting
                var rectangle = _faceRectangles[i];

                if (i < faces.Count)
                {
                    // show rectangle for each detected face 
                    var face = faces[i];
                    Canvas.SetTop(rectangle, face.FaceBox.Y / heightScale);
                    Canvas.SetLeft(rectangle, face.FaceBox.X / widthScale);
                    rectangle.Width = face.FaceBox.Width/widthScale;
                    rectangle.Height = face.FaceBox.Height/heightScale;
                    rectangle.Visibility = Visibility.Visible;
                }
                else
                {
                    // hide rectangle if there is no face for it
                    rectangle.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Set background for cells
        /// </summary>
        /// <param name="cells">Cells with faces</param>
        /// <param name="currentCellFillBrush">Brush or null if need to remove background</param>
        private void SetCellsFill(IEnumerable<Cell> cells, Brush currentCellFillBrush)
        {
            foreach (var cell in cells)
            {
                _cellRectangles[cell.Y, cell.X].Fill = currentCellFillBrush;
            }
        }
    }
}

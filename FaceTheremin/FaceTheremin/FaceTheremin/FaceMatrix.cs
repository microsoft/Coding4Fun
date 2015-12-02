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
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.System.Threading;

namespace FaceTheremin
{
    public class FaceMatrix
    {
        private readonly int _rowsCount;
        private readonly int _columnsCount;

        // FaceTracker detects faces
        private readonly FaceTracker _faceTracker;

        // MediaCapture provides source for streaming element and frames for face detection
        private readonly MediaCapture _mediaCapture;

        // Time interval between two separate face detections (8.33 fps)
        private readonly TimeSpan _frameProcessingTimerInterval = TimeSpan.FromMilliseconds(120);

        // The cells which had faces on a previous frame
        private readonly List<Cell> _previousFrameCells = new List<Cell>();
        private readonly VideoFrame _previewFrame;

        public event EventHandler<FaceMatrixFrameEventArgs> Frame;

        private FaceMatrix(FaceTracker faceTracker, MediaCapture mediaCapture, int rowsCount, int columnsCount)
        {
            _faceTracker = faceTracker;
            _mediaCapture = mediaCapture;
            
            // get properties of the stream, we need them to get width/height for face detection
            var videoProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            _previewFrame = new VideoFrame(BitmapPixelFormat.Nv12, (int)videoProperties.Width, (int)videoProperties.Height);

            _rowsCount = rowsCount;
            _columnsCount = columnsCount;
        }

        public static async Task<FaceMatrix> CreateAsync(MediaCapture mediaCapture, int rowsCount, int columnsCount)
        {
            var faceTracker = await FaceTracker.CreateAsync();
            var faceMatrix = new FaceMatrix(faceTracker, mediaCapture, rowsCount, columnsCount);
            faceMatrix.StartRecognitionLoop();

            return faceMatrix;
        }

        private void StartRecognitionLoop()
        {
            ThreadPoolTimer.CreateTimer(ProcessCurrentVideoFrameAsync, _frameProcessingTimerInterval);
        }

        /// <summary>
        /// Detect faces and process them
        /// </summary>
        /// <param name="timer"></param>
        private async void ProcessCurrentVideoFrameAsync(ThreadPoolTimer timer)
        {
            // fill the frame
            await _mediaCapture.GetPreviewFrameAsync(_previewFrame);

            // collection for faces
            IList<DetectedFace> faces;
            if (FaceDetector.IsBitmapPixelFormatSupported(_previewFrame.SoftwareBitmap.BitmapPixelFormat))
            {
                // get detected faces on the frame
                faces = await _faceTracker.ProcessNextFrameAsync(_previewFrame);
            }
            else
            {
                throw new NotSupportedException($"PixelFormat {BitmapPixelFormat.Nv12} is not supported by FaceDetector.");
            }

            // get the size of frame webcam provided, we need it to scale image on the screen
            var previewFrameSize = new Size(_previewFrame.SoftwareBitmap.PixelWidth, _previewFrame.SoftwareBitmap.PixelHeight);
            ProcessFrameFaces(previewFrameSize, faces);

            // arrange the next processing time
            ThreadPoolTimer.CreateTimer(ProcessCurrentVideoFrameAsync, _frameProcessingTimerInterval);
        }

        /// <summary>
        /// Color the cells with faces and play sounds for them
        /// </summary>
        /// <param name="previewFrameSize">Webcam resolution</param>
        /// <param name="faces"></param>
        private void ProcessFrameFaces(Size previewFrameSize, IList<DetectedFace> faces)
        {
            // get cells with faces
            var cells = faces.Select(x => CreateFaceCell(previewFrameSize, x)).ToArray();
            // exclude cells were on the previous frame
            var newCells = cells.Except(_previousFrameCells).ToArray();

            OnFrame(new FaceMatrixFrameEventArgs
                    {
                        PreviewFrameSize = previewFrameSize,
                        DetectedFaces = faces.ToArray(),
                        PreviousFaceCells = _previousFrameCells.ToArray(),
                        NewFaceCells = newCells.ToArray()
                    });

            // remember current cells with faces for next frame processing
            _previousFrameCells.Clear();
            _previousFrameCells.AddRange(cells);
        }

        /// <summary>
        /// Get the cell for a face
        /// </summary>
        /// <param name="previewFrameSize">Webcam resolution</param>
        /// <param name="face">Detected face</param>
        /// <returns>Cell</returns>
        private Cell CreateFaceCell(Size previewFrameSize, DetectedFace face)
        {
            var cellX = (face.FaceBox.X + face.FaceBox.Width / 2) / (uint)(previewFrameSize.Width / _columnsCount);
            var cellY = (face.FaceBox.Y + face.FaceBox.Height / 2) / (uint)(previewFrameSize.Height / _rowsCount);

            return new Cell((int)cellX, (int)cellY);
        }

        protected virtual void OnFrame(FaceMatrixFrameEventArgs e)
        {
            Frame?.Invoke(this, e);
        }
    }

    public class FaceMatrixFrameEventArgs : EventArgs
    {
        public Size PreviewFrameSize { get; set; }
        public IList<DetectedFace> DetectedFaces { get; set; }
        public IList<Cell> PreviousFaceCells { get; set; }
        public IList<Cell> NewFaceCells { get; set; }
    }
}
# FaceTheremin
## Overview 
A musical coding project that turns users into musical instruments using new media APIs.

At the end of this project you will have a simple Windows 10 app that uses face detection on a live video stream from a camera to trigger musical events. Users will be able to make music by moving their faces to different positions on the screen.  This is accomplished by creating a grid of cells and assigning a unique sound to each cell. When a face is detected within a cell, the sound is triggered. The FaceTracker API makes it possible for multiple people to join as it is able to track several faces at one time. 

Showcasing new APIs for image processing and low-latency audio. 

**Estimated time commitment:** 2 hours 

## Technical requirements: 
* [Visual Studio 2015 and Windows developer tooling.](https://dev.windows.com/en-us/downloads)
* Ensure you are using [Windows 10 or better.](https://www.microsoft.com/en-us/windows/windows-10-upgrade)
* Hardware: 
  * A device that supports video capture. 

## Sample features: 
**Note:** Features in this app are subject to change. 
* [FaceTracker](https://msdn.microsoft.com/en-us/library/windows/apps/windows.media.faceanalysis.facetracker.aspx) – Detects faces in VideoFrame objects and tracks faces across subsequent video frames. 
  * Part of [Windows.Media.FaceAnalysis](C:\Users\v-rehodg\Desktop\Windows.Media.FaceAnalysis) namespace which provides APIs for face detection in bitmaps or video frames. 
* [Audio Graphs class](https://msdn.microsoft.com/en-us/library/windows/apps/mt203787.aspx) -  parent of all nodes that make up the graph.
* [MediaCapture class](https://msdn.microsoft.com/en-us/library/windows/apps/windows.media.capture.mediacapture.aspx) – Provides functionality for capturing photos, audio, and videos from a capture device, such as a webcam.  

## Getting started:
To use these samples, download the entire samples ZIP or clone the repository. If you download the ZIP, you can then unzip the entire archive and use the samples in Visual Studio 2015. An overview of the code can be found in the FaceTheremin exercises folder. 

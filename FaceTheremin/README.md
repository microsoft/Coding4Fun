# FaceTheremin
To inspire you to experiment more with the new media APIs in UWP, we decided to make something very simple, but at the same time highly visual and entertaining. 

You’ve probably seen people play Guitar Hero or similar games at parties countless times. It’s a lot of fun! If you’re a real geek and developer, you can make a similar entertainment system for a party in just few hours. All you need is Windows 10 PC with a web camera and Visual Studio.

We’re going to use face detection on a live video stream from a camera to trigger musical events. The easiest way to do this is to consider the video frame as a grid of cells where each cell corresponds to a unique sound. If our app detects a face in a given cell, it should trigger a sound. How?

With the new FaceTracker API you can track a collection of faces at the same time. We are going to use this API as our facial musical input. 

There are multiple options for making sounds available to developers, including real-time tone generation, but the simplest way is to have a collection of readymade MP3 or WAV audio files. The new Audio Graph API is very easy to use, supports compressed audio formats and uses Windows 10’s low-latency audio pipeline.

Combining these new APIs together, building a FaceTheremin is very easy.

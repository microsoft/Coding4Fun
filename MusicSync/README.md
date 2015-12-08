# MusicSync
## Overview:
MusicSync is a music streaming application designed to play one track on many Windows 10 machines simultaneously. There are three components in this solution. The first is the Server, or Publisher, called MusicServer. The second is the Speaker, or Subscriber called VirtualSpeaker. The final component is a runtime component which contains the Subscriber logic. This final component will be referenced in both the Server and Speaker projects as both will act as a Subscriber.

The MusicServer application contains both a Publisher and a Subscriber. It is a Subscriber to its Publisher in order to play music on the local host device. The VirtualSpeaker application only contains a Subscriber.

## Technical requirements:
* Some experience with C# and XAML.
* [Visual Studio 2015 and Windows developer tooling.](https://dev.windows.com/en-us/downloads)
* Ensure you are using [Windows 10 or better.](https://www.microsoft.com/en-us/windows/windows-10-upgrade)

## Getting started: 
To use these samples, download the entire samples ZIP or clone the repository. If you download the ZIP, you can then unzip the entire archive and use the samples in Visual Studio 2015. An overview of the code can be found in the FaceTheremin exercises folder. 



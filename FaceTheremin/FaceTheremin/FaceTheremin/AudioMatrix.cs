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
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;

namespace FaceTheremin
{
    public class AudioMatrix
    {
        private static readonly string[] InstrumentPrefixes = { "snd_2", "snd_1", "synth_2", "synth_1", "drum_2", "drum_1" };

        // Still need to keep explicit reference to the AudioGraph object, otherwise it gets disposed
        private readonly AudioGraph _audioGraph;

        // Two-dimentional array for storing input nodes corresponding to the cells 
        private readonly AudioFileInputNode[,] _audioFileInputNodes;

        private AudioMatrix(AudioGraph audioGraph, AudioFileInputNode[,] audioFileInputNodes)
        {
            _audioGraph = audioGraph;
            _audioFileInputNodes = audioFileInputNodes;

            // we have to start audioGraph one time but we can start/stop individual 
            //input nodes as many times as needed
            _audioGraph.Start();
        }

        /// <summary>
        /// Initialize and start AudioGraph
        /// </summary>
        /// <returns></returns>
        public static async Task<AudioMatrix> CreateAsync(int rowsCount, int columnsCount)
        {
            var audioGraph = await CreateAudioGraph();
            var audioFileInputNodes = await LoadAudioFileInputNodesAsync(rowsCount, columnsCount, audioGraph);

            return new AudioMatrix(audioGraph, audioFileInputNodes);
        }

        private static async Task<AudioGraph> CreateAudioGraph()
        {
            // we will only play files that's why we are using AudioRenderCategory.Media
            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            var result = await AudioGraph.CreateAsync(settings);
            if (result.Status != AudioGraphCreationStatus.Success)
            {
                throw new Exception("Audio graph unavailable.");
            }

            return result.Graph;
        }

        private static async Task<AudioFileInputNode[,]> LoadAudioFileInputNodesAsync(int rowsCount, int columnsCount, AudioGraph audioGraph)
        {
            var audioDeviceOutputNode = await CreateAudioDeviceOutputNodeAsync(audioGraph);
            var storageFiles = await LoadStorageFiles(rowsCount, columnsCount);
            var result = new AudioFileInputNode[rowsCount, columnsCount];

            // initialize an input node for each cell
            for (var y = 0; y < rowsCount; y++)
            {
                for (var x = 0; x < columnsCount; x++)
                {
                    var inputResult = await audioGraph.CreateFileInputNodeAsync(storageFiles[y, x]);
                    if (inputResult.Status != AudioFileNodeCreationStatus.Success) continue;

                    var audioFileInputNode = inputResult.FileInputNode;
                    // it shouldn't start when we add it to audioGraph
                    audioFileInputNode.Stop();
                    // link it to the output node
                    audioFileInputNode.AddOutgoingConnection(audioDeviceOutputNode);
                    // add to the array for easier access to playback
                    result[y, x] = audioFileInputNode;
                }
            }

            return result;
        }

        private static async Task<AudioDeviceOutputNode> CreateAudioDeviceOutputNodeAsync(AudioGraph audio)
        {
            //create output node - speakers in our case
            var result = await audio.CreateDeviceOutputNodeAsync();
            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception("Audio device node unavailable.");
            }

            return result.DeviceOutputNode;
        }

        private static async Task<StorageFile[,]> LoadStorageFiles(int rowsCount, int columnsCount)
        {
            var result = new StorageFile[rowsCount, columnsCount];
            for (var x = 0; x < columnsCount; x++)
            {
                for (var y = 0; y < rowsCount; y++)
                {
                    //Use Assets folder or AssetsHolidaySpecial folder for reguilar or special sounds
                    //result[y, x] = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///AssetsHolidaySpecial/{InstrumentPrefixes[x]}_{y + 1}.mp3"));
                    result[y, x] = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{InstrumentPrefixes[x]}_{y + 1}.mp3"));
                }
            }

            return result;
        }

        /// <summary>
        /// Play sounds for cells with faces
        /// </summary>
        /// <param name="newCells">Cells with faces</param>
        public void PlayCells(IEnumerable<Cell> newCells)
        {
            // get the corresponding  sound by coordinates of the cell
            foreach (var audioFileInputNode in newCells.Select(x => _audioFileInputNodes[x.Y, x.X]))
            {
                // we want to play the sound from the beginning every time
                audioFileInputNode.Reset();
                audioFileInputNode.Start();
            }
        }
    }
}

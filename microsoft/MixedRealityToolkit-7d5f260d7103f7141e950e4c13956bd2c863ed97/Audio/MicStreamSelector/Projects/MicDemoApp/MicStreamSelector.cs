﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace MicDemoApp
{
    public class MicStreamSelector
    {
        // Make sure you enable MusicLibrary and Microphone capabilities in APPX Manifest

        // Can't call blocking APIs from the UI thread. It's annoying to deal with, but your safest bet is to Task-ify everything, which is why this code is bloated.
        // Concurrency::invalid_operation and SEHExceptions will occur if you try to call blocking APIs from the UI thread

        // To use this API first:
        //    await CreateAudioGraph();
        // then, call anything else
        // you really only have to create the graph once and it will run forever with all sorts of functions. I only stop the graph here as an example. 

        // CheckForErrorOnCall() is entirely optional, but incredibly helpful for debugging

        // this class is made for hololens mic stream selection, but does work well on all windows 10 devices
        // chooses from one of three possible microphone modes on HoloLens. More modes exist on other devices, so this can be extended.

        // Streams: SPEECH is optimized for voice transmission, COMMUNICATIONS is higher quality voice capture, MEDIA is a "room capture"
        // can only be set on initialization
        public enum StreamCategory { SPEECH, COMMUNICATIONS, MEDIA }
        public static StreamCategory streamtype = StreamCategory.SPEECH;
        private enum ErrorCodes { ALREADY_RUNNING = -10, NO_AUDIO_DEVICE, NO_INPUT_DEVICE, ALREADY_RECORDING, GRAPH_NOT_EXIST, CHANNEL_COUNT_MISMATCH, FILE_CREATION_PERMISSION_ERROR, NOT_ENOUGH_DATA, NEED_ENABLED_MIC_CAPABILITY };

        // Unfortunately, we can't create AudioGraph from Task because it attaches to the background. We have to make our graph here and pass that to the plugin.
        private static AudioGraph graph;

        // can boost input here as desired. 1 is default but almost definitely too quiet. can change during operation without problem.
        private static float inputGain = 1;

        // if keepAllData==false, you'll always get the newest data no matter how long the program hangs for any reason, but will lose some data if the program does hang 
        // can only be set on initialization
        private static bool keepAllData = false;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]   // this callback will be triggered if desired by the plugin to tell us that a buffer of audio data is ready to grab
        public delegate void LiveMicCallback();

        [DllImport("MicStreamSelector", ExactSpelling = true)]    public static extern int MicInitializeDefaultWithGraph(int category, AudioGraph appGraph); // pass graph from app here to mic plugin
        [DllImport("MicStreamSelector", ExactSpelling = true)]    public static extern int MicInitializeCustomRateWithGraph(int category, int samplerate, AudioGraph appGraph);  // pass graph from app here to mic plugin w/ custom sample rate
        [DllImport("MicStreamSelector")]    public static extern int MicStartStream(bool keepData, bool previewOnDevice, LiveMicCallback micsignal); // preview on device will play micstream in speakers
        [DllImport("MicStreamSelector")]    public static extern int MicStopStream();

        // takes only the wav file's name with extenions, aka "myfile.wav", not full path
        // if you don't want to hear the microphone when you're recording, just set the audiosource volume to zero
        [DllImport("MicStreamSelector")]    public static extern int MicStartRecording(string filename, bool previewOnDevice); // preview on device will play micstream in speakers

        // if you want to stop the microphone streaming to audiosource after recording, just call MicStopStream()
        // sb returns full path to recorded audio file
        [DllImport("MicStreamSelector")]    public static extern void MicStopRecording(StringBuilder sb);

        [DllImport("MicStreamSelector")]    public static extern int MicDestroy();  // releases everything including device
        [DllImport("MicStreamSelector")]    public static extern int MicPause();
        [DllImport("MicStreamSelector")]    public static extern int MicResume();
        [DllImport("MicStreamSelector")]    public static extern int MicSetGain(float g);
        [DllImport("MicStreamSelector")]    private static extern int MicGetDefaultBufferSize(); // if you were doing default setup, you need to know what's going on sometimes
        [DllImport("MicStreamSelector")]    private static extern int MicGetDefaultNumChannels(); // if you were doing default setup, you need to know what's going on sometimes
        [DllImport("MicStreamSelector")]    private static extern int MicGetFrame(float[] buffer, int length, int numchannels); // returns buffer of length to the app. handy if working in another engine

        static int buffersize = 0;
        static int numChannels = 0;

        // If function is passed to the mic stream, this function will be called by the mic stream plugin when data is ready for consumption
        public static LiveMicCallback micSignal = () =>
        {
            // DO VOIP OR OTHER LIVE MIC DATA HANDLING HERE !!!!

            float[] audiodata = new float[buffersize];
            MicGetFrame(audiodata, buffersize, numChannels);    // our app uses default buffersize. we could actually ask for any size buffer, but in this case we would lose sync with the audio stream

            // my app uses the data to set average mic volume as opacity on an ellipse! neat!
            // mic data will ALWAYS be stored as interleaved float data from [-1,+1]. 
            // opacity on my XAML object in this example is from [0,+1], so we're averaging and then scaling
            float average = 0;
            for (int i=0; i<buffersize; i++)
            {
                // this condition is letting us always consider amplitude as a positive number, because it roughly maps to volume as long as its non zero
                if (audiodata[i] < 0)
                {
                    average += -audiodata[i];
                }
                else
                {
                    average += audiodata[i];
                }
            }
            average /= buffersize;
            MainPage.Instance.SetVolumeMonitor(average);    // the actual call back into the app to change the opacity of the mic monitor 
        };

        // if we pass the last parameter (the LiveMicCallback) as null, the app won't get signalled when a buffer is ready for consumption
        // this is preferable if you're in a polled engine, like a game like Unity, so you can grab mic data whenever the game engine is ready for it
        // if your app is not self-polled, like this XAML app is not, youll want to pass the callback into the plugin so the plugin can drive audio data handling in the LiveMicCallback micSignal above
        public static void StartStream()
        {
            Task.Factory.StartNew(async () =>
            {
                await CreateAudioGraph();
                CheckForErrorOnCall(MicStartStream(keepAllData, true, micSignal));

                buffersize = MicGetDefaultBufferSize(); // we could also do custom sizes, but that's not a great idea for XAML since we're letting the callback drive the audio handling in app
                numChannels = MicGetDefaultNumChannels();
            }
            );
        }

        public static void StartRecording(string fileNameWithExtension)
        {
            Task.Factory.StartNew(async () =>
            {
                await CreateAudioGraph();

                // could NOT pass the callback here (pass as null) if we didnt care about handling the live data and just wanted to record a file
                // could also just skip this call entirely and only record a file without a live data stream back to the app
                CheckForErrorOnCall(MicStartStream(keepAllData, true, micSignal));   

                // can choose to monitor the mic stream here in the second parameter
                CheckForErrorOnCall(MicStartRecording(fileNameWithExtension, true));
            }
            );
        }

        public static void StopMicDevice()
        {
            StringBuilder sb = new StringBuilder(256);
            Task.Factory.StartNew(() =>
            {
                MicStopRecording(sb);
                Debug.WriteLine(sb.ToString());
                CheckForErrorOnCall(MicDestroy());
            }
            );
        }

        // could make a smaller set of calls, like this
        public static void StopRecording()
        {

            StringBuilder sb = new StringBuilder(256);
            Task.Factory.StartNew(() =>
            {
                MicStopRecording(sb);
                Debug.WriteLine(sb.ToString());
            }
            );
        }

        // could make a smaller set of calls depending on your app, like this one
        public static void StopStream()
        {
            Task.Factory.StartNew(() =>
            {
                CheckForErrorOnCall(MicStopStream());
            }
            );
        }

        // this is an unfortunate workaround because we can't start c++ AudioGraph from the UI thread due to its blocking calls. We have to create it here and pass it to the Plugin.
        private static async Task CreateAudioGraph()
        {
            if (graph != null)
            {
                return;
            }
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);    // Create an AudioGraph with default settings
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);             // this graph is bound to this process

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                return; // Cannot create graph
            }
            graph = result.Graph;
            CheckForErrorOnCall(MicInitializeDefaultWithGraph((int)streamtype, graph)); // pass the bound graph to the mic plugin. this lets our current process hear audio. 
        }

        static void CheckForErrorOnCall(int returnCode)
        {
            switch (returnCode)
            {
                
                case (int)ErrorCodes.ALREADY_RECORDING:
                    Debug.WriteLine ("ERROR: Tried to start recording when you were already doing so. You need to stop your previous recording before you can start again.");
                    break;
                case (int)ErrorCodes.ALREADY_RUNNING:
                    Debug.WriteLine ("WARNING: Tried to initialize microphone more than once. Probably not a problem, just letting you know.");
                    break;
                case (int)ErrorCodes.GRAPH_NOT_EXIST:
                    Debug.WriteLine("ERROR: Tried to do microphone things without a properly initialized microphone device. \n " +
                        "Do you have a mic plugged into a functional audio system and did you call MicInitialize()?");
                    break;
                case (int)ErrorCodes.NO_AUDIO_DEVICE:
                    Debug.WriteLine ("ERROR: Tried to start microphone, but you don't appear to have a functional audio device. check your OS audio settings.");
                    break;
                case (int)ErrorCodes.NO_INPUT_DEVICE:
                    Debug.WriteLine ("ERROR: Tried to start microphone, but you don't have one plugged in, OR you didn't enable Microphone capability for app");
                    break;
                case (int)ErrorCodes.CHANNEL_COUNT_MISMATCH:
                    Debug.WriteLine ("ERROR: Microphone had a channel count mismatch internally on device. Try setting different mono/stereo options in OS mic settings.");
                    break;
                case (int)ErrorCodes.FILE_CREATION_PERMISSION_ERROR:
                    Debug.WriteLine ("ERROR: Didn't have access to create file in Music library. Make sure permissions in appxmanifest to write to Music library are granted.");
                    break;
                case (int)ErrorCodes.NOT_ENOUGH_DATA:
                    Debug.WriteLine("WARNING: Not enough data in mic buffer to fulfill request. Wait a bit for data to accumulate or try requesting a smaller frame size.");
                    break;
                case (int)ErrorCodes.NEED_ENABLED_MIC_CAPABILITY:
                    Debug.WriteLine("ERROR: Mic not started because you didn't enable the Microphone capability in the Appxmanifest.");
                    break;
            }
        }
    }
}


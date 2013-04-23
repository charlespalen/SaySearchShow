//------------------------------------------------------------------------------
// <copyright file="SpeechRecognizer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

// This module provides sample code used to demonstrate the use
// of the KinectAudioSource for speech recognition in a game setting.

// IMPORTANT: This sample requires the Speech Platform SDK (v11) to be installed on the developer workstation

namespace FlickrKinectPhotoFun
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Kinect;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.IO;

    public class SpeechRecognizer : IDisposable
    {
        // Event
        public event EventHandler VoiceRecognized;
        public event EventHandler VoiceHypothesized;
        public event EventHandler VoiceRejected;
        public event EventHandler VoiceLevelChange;

        public String latestRecognizedSpeech = "";
        public String latestHypothesizedSpeech = "";
        public int latestAudioLevel = 0;
        public bool usingKinect = false;

        private System.Speech.Recognition.SpeechRecognitionEngine sre;
        private KinectSensor sensor;
        private bool isDisposed;

        public SpeechRecognizer()
        {
            setupSpeechRecognizerOrKinect();
        }

        private void setupSpeechRecognizerOrKinect()
        {

            try
            {
                foreach (var potentialSensor in KinectSensor.KinectSensors)
                {
                    if (potentialSensor.Status == KinectStatus.Connected)
                    {
                        this.sensor = potentialSensor;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Kinect sensor enumeration exception: " + e.Message);
            }

            try {
            if (this.sensor != null)
            {
                try
                {
                    // Start the sensor!
                    this.sensor.Start();

                    System.Speech.Recognition.RecognizerInfo ri =  System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault();
                    sre = new System.Speech.Recognition.SpeechRecognitionEngine(ri.Id);

                }
                catch (IOException)
                {
                    // Some other application is streaming from the same Kinect sensor
                    this.sensor = null;
                }
            }
            } catch (Exception e) {
                Console.WriteLine("Exception setting the kinect sensor as the speach recognition engine: " + e.Message);
            }

            try {
                if (this.sensor == null) {
                    Console.WriteLine("Using default audio device");
                    //Create the speech recognition engine using the default
                    //sre = new Microsoft.Speech.Recognition.SpeechRecognitionEngine();
                    sre = new System.Speech.Recognition.SpeechRecognitionEngine();

                    //Set the audio device to the OS default
                    sre.SetInputToDefaultAudioDevice();
                }
            } catch (Exception e) {
                Console.WriteLine("Exception setting the default autio device:" + e.Message);
            }

            //Reset the Grammars
            sre.UnloadAllGrammars();
            //sre = new SpeechRecognitionEngine("SR_MS_ZXX_Lightweight_v11.0");
            this.sre.SpeechRecognized += this.SreSpeechRecognized;
            this.sre.SpeechHypothesized += this.SreSpeechHypothesized;
            this.sre.SpeechRecognitionRejected += this.SreSpeechRecognitionRejected;
            // Listen to audio levels to display to the user
            this.sre.AudioLevelUpdated += SreLevelsChange;

            // Load in our grammar
            System.Speech.Recognition.Grammar flickerGrammar = FlikrSearchCmd_Grammar();

            this.sre.LoadGrammar(flickerGrammar);
            // Set the audio stream only for kinect
            if (this.sensor != null)
            {
                sensor.AudioSource.AutomaticGainControlEnabled = false;
                sensor.AudioSource.EchoCancellationMode = EchoCancellationMode.None;

                Stream audStream = sensor.AudioSource.Start();
                this.sre.SetInputToAudioStream(audStream, new System.Speech.AudioFormat.SpeechAudioFormatInfo(System.Speech.AudioFormat.EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                usingKinect = true;
            }

            this.sre.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple);
            // Turn on the voice recognizer
            
        }

        private System.Speech.Recognition.Grammar FlikrSearchCmd_Grammar()
        {
            System.Speech.Recognition.Grammar grammar = null;

            System.Speech.Recognition.Choices knXbxCh = new System.Speech.Recognition.Choices("kinect", "xbox");
            System.Speech.Recognition.Choices knXbxCh2 = new System.Speech.Recognition.Choices("show");
            System.Speech.Recognition.GrammarBuilder GMXbxKn = new System.Speech.Recognition.GrammarBuilder();
            GMXbxKn.Append(knXbxCh);
            GMXbxKn.Append(knXbxCh2);
            GMXbxKn.AppendDictation();

            grammar = new System.Speech.Recognition.Grammar(GMXbxKn);
            grammar.Name = "Kinect and xbox grammar";
            return grammar;
        }

        private Microsoft.Speech.Recognition.RecognizerInfo GetKinectRecognizer()
        {
            foreach (Microsoft.Speech.Recognition.RecognizerInfo recognizer in Microsoft.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        private void listAllRecognizers()
        {
            foreach (Microsoft.Speech.Recognition.RecognizerInfo recognizer in Microsoft.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers())
            {
                Console.WriteLine("Recognizer: " + recognizer.Name + " id:" + recognizer.Id);
                foreach (String _key in recognizer.AdditionalInfo.Keys)
                {
                    Console.WriteLine("Info: " + _key + " val:" + recognizer.AdditionalInfo[_key] );
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Stop();

                if (this.sre != null)
                {
                    // NOTE: The SpeechRecognitionEngine can take a long time to dispose
                    // so we will dispose it on a background thread
                    ThreadPool.QueueUserWorkItem(
                        delegate(object state)
                        {
                            IDisposable toDispose = state as IDisposable;
                            if (toDispose != null)
                            {
                                toDispose.Dispose();
                            }
                        },
                            this.sre);
                    this.sre = null;
                }

                this.isDisposed = true;
            }
        }

        public void Stop()
        {
            this.CheckDisposed();

            if (this.sre != null)
            {
                if (this.sensor != null)
                {
                    this.sensor.AudioSource.Stop();
                }
                this.sre.RecognizeAsyncCancel();
                this.sre.RecognizeAsyncStop();

                this.sre.SpeechRecognized -= this.SreSpeechRecognized;
                this.sre.SpeechHypothesized -= this.SreSpeechHypothesized;
                this.sre.SpeechRecognitionRejected -= this.SreSpeechRecognitionRejected;
                this.sre.AudioLevelUpdated -= this.SreLevelsChange;
            }
        }

        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("SpeechRecognizer");
            }
        }

        private void SreSpeechRecognitionRejected(object sender, System.Speech.Recognition.SpeechRecognitionRejectedEventArgs e)
        {
            if (VoiceRejected != null)
            {
                EventArgs evt = new EventArgs();
                VoiceRejected(this, evt);
            }

            Console.WriteLine("\nSpeech Rejected");
        }

        private void SreSpeechHypothesized(object sender, System.Speech.Recognition.SpeechHypothesizedEventArgs e)
        {
            Console.Write("\rSpeech Hypothesized: \t{0}", e.Result.Text + " -> " + e.Result.Confidence);
            latestHypothesizedSpeech = e.Result.Text;
            if (VoiceHypothesized != null)
            {
                EventArgs evt = new EventArgs();
                VoiceHypothesized(this, evt);
            }
        }

        private void SreSpeechRecognized(object sender, System.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            Console.Write("\rSpeech Recognized: \t{0}", e.Result.Text + " Confidence: " + e.Result.Confidence);
            latestRecognizedSpeech = e.Result.Text;

            // remove "kinect show" or "xbox show"
            latestRecognizedSpeech = latestRecognizedSpeech.Replace("kinect show", "");
            latestRecognizedSpeech = latestRecognizedSpeech.Replace("xbox show", "");
            if (VoiceRecognized != null)
            {
                EventArgs evt = new EventArgs();
                VoiceRecognized(this, evt);
            }
        }

        private void SreLevelsChange(object sender, System.Speech.Recognition.AudioLevelUpdatedEventArgs e)
        {
            //Console.WriteLine("Speech Level: " + e.AudioLevel + " dev: ");

            // ===========> 0 to 100
            latestAudioLevel = e.AudioLevel;
            if (VoiceLevelChange != null)
            {
                EventArgs evt = new EventArgs();
                VoiceLevelChange(this, evt);
            }
        }
    }
}
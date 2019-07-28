using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common.Models;
using Common.Enums;
using Common.Utilities;

namespace WinSpeechToText
{
    /// <summary>
    /// Converts record audio file (*.wav) to text
    /// </summary>
    public partial class FormProcessAudio : Form
    {
        private string subscriptionKey = ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionKey"];
        private string subscriptionRegion = ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionRegion"];
        private string fetchUri = "https://" + ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionRegion"] + ".api.cognitive.microsoft.com/sts/v1.0";
        private string requestUri = "https://" + ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionRegion"] + ".stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US";
        private string host = ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionRegion"] + ".stt.speech.microsoft.com";
        private int audioSampleRate = Convert.ToInt32(ConfigurationManager.AppSettings["AudioSampleRate"]);

        private string filePath = ConfigurationManager.AppSettings["RecordingStoragePath"];
        private string processedFilesPath = ConfigurationManager.AppSettings["ProcessedFilesPath"];
        private string failedFilesPath = ConfigurationManager.AppSettings["FailedFilesPath"];
        private string fileNamePrefix = ConfigurationManager.AppSettings["RecordingNamePrefix"];
        private int minRecordSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["MinRecordSeconds"]);
        private int maxRecordSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["MaxRecordSeconds"]);
        private int speechServiceType = Convert.ToInt32(ConfigurationManager.AppSettings["SpeechServiceType"]); //1:SDK, 2:API
        private int enableAutoSaveReponseToFile = Convert.ToInt32(ConfigurationManager.AppSettings["EnableAutoSaveReponseToFile"]);
        private int enablePrint = Convert.ToInt32(ConfigurationManager.AppSettings["EnablePrint"]);

        private Font printFont = new Font("Courier New", 12);
        private string printText = "";
        private DateTime startTime;
        private string audiofileName = "";
        private string audioFileNameWithPath = "";
        private IWaveIn captureDevice;
        private WaveFileWriter writer;
        private SoundPlayer soundPlayer;

        public FormProcessAudio()
        {
            InitializeComponent();            
        }

        /// <summary>
        /// Form Load event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormProcessAudio_Load(object sender, EventArgs e)
        {
            lblError.Text = "";
            lblMinSec.Text = minRecordSeconds > 0 ? ("Min. " + minRecordSeconds + " sec") : "";
            lblMaxSec.Text = maxRecordSeconds > 0 ? ("Max. " + maxRecordSeconds + " sec") : "";
            SetControlsState(0);

            if (string.IsNullOrWhiteSpace(subscriptionKey) || string.IsNullOrWhiteSpace(subscriptionRegion))
            {
                panelMain.Enabled = false;
                string message = "Subscription details are missing in the config file.";
                message = message + "\n" + "Sign up at https://azure.microsoft.com/en-us/try/cognitive-services/ to get subscription key";
                lblError.Text = message;
                //MessageBox.Show(message);
            }
            else
            {
                panelMain.Enabled = true;
                btnPrint.Visible = (enablePrint == 1) ? true : false;
            }
        }

        #region FormControl Events
        /// <summary>
        /// Starts audio recording using NAudio plugin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            try
            {
                StartRecording();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                //MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Saves record audio to physical path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            try
            {
                SaveRecording();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                SetControlsState(0);
                //MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Plays recorded audio file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPlay_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            try
            {
                StopAudioPlayer();

                if (File.Exists(audioFileNameWithPath))
                {
                    soundPlayer = new SoundPlayer(audioFileNameWithPath);
                    soundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                SetControlsState(0);
                //MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Converts recorded audio file to text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnConvert_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            bool isConversionSuccess = false;
            try
            {
                StopAudioPlayer();
                SetControlsState(3);
                pbProcessing.Visible = true;

                if (speechServiceType == (int)SpeechServiceType.API)
                {
                    string token = await FetchToken();
                    isConversionSuccess = ConvertSpeechToText(audioFileNameWithPath, token);
                }
                else
                {
                    isConversionSuccess = await ContinuousRecognitionWithFileAsync(audioFileNameWithPath);
                }

                if (isConversionSuccess)
                {
                    if (enableAutoSaveReponseToFile == 1)
                        SaveConvertedTextToFile();
                }

                SetControlsState(4);
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                SetControlsState(0);
                //MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Cancels playing of audio file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                captureDevice?.StopRecording();
                Cleanup();
                StopAudioPlayer();
                SetControlsState(0);
                lblError.Text = "";

                if (File.Exists(audioFileNameWithPath))
                    File.Delete(audioFileNameWithPath);
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                SetControlsState(0);
                //MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Prints converted text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (printDialog1.ShowDialog() == DialogResult.OK)
            {
                printText = rtbResult.Text;
                printDocument1.Print();
            }

            printText = "";
        }

        /// <summary>
        /// Timer tick event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            var diff = DateTime.Now.Subtract(startTime);
            if (diff.TotalSeconds > 0)
            {
                lblTimer.Text = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                           diff.Hours, diff.Minutes, diff.Seconds);

                if (btnPlay.Enabled == false && Math.Round(Convert.ToDouble(diff.Seconds), 2) >= minRecordSeconds)
                    btnSave.Enabled = true;
            }
        }
        #endregion FormControl Events

        /// <summary>
        /// Starts Timer
        /// </summary>
        private void StartTimer()
        {
            timer1.Enabled = true;
            startTime = DateTime.Now;
            timer1.Tick -= new EventHandler(timer1_Tick);
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Start();
        }

        /// <summary>
        /// Sets Controls state as per provided flag. 0: initial state, 1: recording started state, 2: recording saved state, 3: conversion inprogress, 4: converted state
        /// </summary>
        /// <param name="flag"></param>
        private void SetControlsState(int flag)
        {
            switch (flag)
            {
                case 0: // initial state
                    timer1.Stop();
                    timer1.Enabled = false;

                    btnStart.Enabled = true;
                    btnSave.Enabled = false;
                    btnPlay.Enabled = false;
                    btnConvert.Enabled = false;
                    btnCancel.Enabled = false;
                    lblTimer.Text = "00:00:00";
                    pbProcessing.Visible = false;
                    rtbResult.Text = "";
                    rtbResult.Enabled = false;
                    btnPrint.Enabled = false;
                    break;

                case 1: // recording started
                    btnPlay.Enabled = false;
                    btnConvert.Enabled = false;
                    btnCancel.Enabled = true;
                    lblTimer.Text = "00:00:00";
                    rtbResult.Text = "";
                    rtbResult.Enabled = false;
                    btnPrint.Enabled = false;
                    break;

                case 2: //recording saved
                    timer1.Stop();
                    timer1.Enabled = false;
                    btnSave.Enabled = false;
                    btnPlay.Enabled = true;
                    btnCancel.Enabled = true;
                    btnConvert.Enabled = true;
                    btnPrint.Enabled = false;
                    rtbResult.Text = "";
                    rtbResult.Enabled = false;
                    break;

                case 3: // conversion inProgress
                    timer1.Stop();
                    timer1.Enabled = false;
                    btnSave.Enabled = false;
                    btnPlay.Enabled = true;
                    btnConvert.Enabled = false;
                    btnCancel.Enabled = false;
                    btnPrint.Enabled = false;
                    rtbResult.Enabled = false;
                    break;

                case 4: // converted state
                    timer1.Stop();
                    timer1.Enabled = false;

                    btnStart.Enabled = true;
                    btnSave.Enabled = false;
                    btnPlay.Enabled = false;
                    btnConvert.Enabled = false;
                    lblTimer.Text = "00:00:00";
                    pbProcessing.Visible = false;
                    btnPrint.Enabled = true;
                    rtbResult.Enabled = true;
                    break;

                default:
                    break;
            }

            pbProcessing.Visible = false;

        }

        #region AudioRecording Logic
        /// <summary>
        /// Starts audio recoring using NAudio plugin
        /// </summary>
        private void StartRecording()
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            if (!Directory.Exists(processedFilesPath))
                Directory.CreateDirectory(processedFilesPath);

            if (!Directory.Exists(failedFilesPath))
                Directory.CreateDirectory(failedFilesPath);

            StopAudioPlayer();
            SetControlsState(1);

            // Forcibly turn on the microphone (some programs (Skype) turn it off).
            //var device = (MMDevice)comboWasapiDevices.SelectedItem;
            //device.AudioEndpointVolume.Mute = false;

            audiofileName = fileNamePrefix + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".wav";
            audioFileNameWithPath = filePath + audiofileName;

            if (File.Exists(audioFileNameWithPath))
            {
                SetControlsState(0);
                throw new Exception($"File {audioFileNameWithPath} already exists");        
            }

            if (captureDevice != null)
            {
                Cleanup();
            }

            captureDevice = CreateWaveInDevice();
            writer = new WaveFileWriter(audioFileNameWithPath, captureDevice.WaveFormat);
            captureDevice.StartRecording();
            StartTimer();
        }
        
        /// <summary>
        /// Saves recorded audio file to physical location
        /// </summary>
        private void SaveRecording()
        {
            if (captureDevice != null)
            {
                captureDevice?.StopRecording();
                Cleanup();
            }

            SetControlsState(2);            
        }

        private IWaveIn CreateWaveInDevice()
        {
            var channels = 1;
            IWaveIn newWaveIn = new WaveIn();

            newWaveIn.WaveFormat = new WaveFormat(audioSampleRate, channels);
            newWaveIn.DataAvailable += OnDataAvailable;
            newWaveIn.RecordingStopped += OnRecordingStopped;
            return newWaveIn;
        }
        
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<StoppedEventArgs>(OnRecordingStopped), sender, e);
            }
            else
            {
                FinalizeWaveFile();
            }
        }

        private void Cleanup()
        {
            if (captureDevice != null)
            {
                captureDevice.Dispose();
                captureDevice = null;
            }
            FinalizeWaveFile();
        }

        private void FinalizeWaveFile()
        {
            writer?.Dispose();
            writer = null;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), sender, e);
            }
            else
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                int secondsRecorded = (int)(writer.Length / writer.WaveFormat.AverageBytesPerSecond);
                if (maxRecordSeconds > 0 && secondsRecorded >= maxRecordSeconds)
                {
                    SaveRecording();
                }
            }
        }
        #endregion AudioRecording Logic

        /// <summary>
        /// Stops audio player if it is playing
        /// </summary>
        private void StopAudioPlayer()
        {
            if (soundPlayer != null)
            {
                soundPlayer.Stop();
                soundPlayer.Dispose();
                soundPlayer = null;
            }
        }

        #region ConvertSpeechToText - API
        /// <summary>
        /// Fetches speech api token
        /// </summary>
        /// <returns></returns>
        private async Task<string> FetchToken()
        {
            string response = "";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                UriBuilder uriBuilder = new UriBuilder(fetchUri);
                uriBuilder.Path += "/issueToken";

                var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
                response = await result.Content.ReadAsStringAsync();
            }

            return response;
        }

        /// <summary>
        /// Converts recorded audio file to text using Microoft Speech API
        /// </summary>
        /// <param name="audioFile"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool ConvertSpeechToText(string audioFile, string token)
        {
            bool isSuccess = false;
            string result = "";            

            // Note: Sign up at https://azure.microsoft.com/en-us/try/cognitive-services/ to get a subscription key.  
            // Navigate to the Speech tab and select Bing Speech API. Use the subscription key as Client secret below.

            string contentType = @"audio/wav; codec=""audio/pcm""; samplerate=" + audioSampleRate;
            string responseString;
            FileStream fs = null;

            try
            {                
                HttpWebRequest request = null;
                request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
                request.SendChunked = true;
                request.Accept = @"application/json;text/xml";
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                request.Host = host;
                request.ContentType = contentType;
                request.Headers["Authorization"] = "Bearer " + token;

                using (fs = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
                {
                    /*
                     * Open a request stream and write 1024 byte chunks in the stream one at a time.
                     */
                    byte[] buffer = null;
                    int bytesRead = 0;
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        /*
                         * Read 1024 raw bytes from the input audio file.
                         */
                        buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            requestStream.Write(buffer, 0, bytesRead);
                        }

                        // Flush
                        requestStream.Flush();
                    }

                    /*
                     * Get the response from the service.
                     */
                    using (WebResponse response = request.GetResponse())
                    {
                        using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                        {
                            responseString = sr.ReadToEnd();
                        }
                        result = responseString;

                        APIResponse apiResponse = null;
                        apiResponse = JsonConvert.DeserializeObject<APIResponse>(responseString);
                        if (apiResponse != null && !string.IsNullOrWhiteSpace(apiResponse.DisplayText))
                        {
                            isSuccess = true;
                            result = apiResponse.DisplayText;
                            rtbResult.Text = result;
                        }
                        else
                        {
                            lblError.Text = "Something went wrong, please try again..";
                            //MessageBox.Show("Something went wrong, please try again..");
                        }
                    }
                }

                if (isSuccess)
                {                    
                    File.Move(audioFile, processedFilesPath + "//" + audiofileName);
                }
                else
                {
                    File.Move(audioFile, failedFilesPath + "//" + audiofileName);
                }
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                //MessageBox.Show(ex.Message);
            }
           
            return isSuccess;
        }
        #endregion ConvertSpeechToText -API

        #region ConvertSpeechToText - SDK
        /// <summary>
        /// Converts audio file to text using Microsoft Speech SDK
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public async Task<bool> ContinuousRecognitionWithFileAsync(string audioFile)
        {
            bool isSuccess = true;
            string finalResult = "";
            
            try
            {
                // <recognitionContinuousWithFile>
                // Creates an instance of a speech config with specified subscription key and service region.
                // Replace with your own subscription key and service region (e.g., "westus").
                var config = SpeechConfig.FromSubscription(subscriptionKey, subscriptionRegion);

                string partialResult = "";
                var stopRecognition = new TaskCompletionSource<int>();

                // Creates a speech recognizer using file as audio input.
                // Replace with your own audio file name.
                using (var audioInput = AudioConfig.FromWavFileInput(audioFile))
                {
                    using (var recognizer = new SpeechRecognizer(config, audioInput))
                    {
                        // Subscribes to events.
                        /*recognizer.Recognizing += (s, e) =>
                        {
                            Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                        };*/

                        recognizer.Recognized += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                if (!string.IsNullOrWhiteSpace(e.Result.Text))
                                {
                                    partialResult = e.Result.Text;
                                    finalResult = finalResult + partialResult;

                                    rtbResult.Invoke((MethodInvoker)delegate {
                                        // Running on the UI thread
                                        rtbResult.Text = finalResult;
                                    });
                                }
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                lblError.Invoke((MethodInvoker)delegate {
                                    // Running on the UI thread
                                    lblError.Text = "Some part of speech could not be recognized.";
                                });
                            }
                        };

                        recognizer.Canceled += (s, e) =>
                        {                           

                            if (e.Reason == CancellationReason.Error)
                            {
                                lblError.Invoke((MethodInvoker)delegate {
                                    // Running on the UI thread
                                    lblError.Text = $"ErrorCode={e.ErrorCode} \n ErrorDetails={e.ErrorDetails}";
                                });
                                isSuccess = false;
                            }
                            stopRecognition.TrySetResult(0);
                        };

                        /*recognizer.SessionStarted += (s, e) =>
                        {
                            Console.WriteLine("\n    Session started event.");
                        };*/

                        recognizer.SessionStopped += (s, e) =>
                        {
                            stopRecognition.TrySetResult(0);
                        };

                        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Task.WaitAny(new[] { stopRecognition.Task });

                        // Stops recognition.
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    }
                }
                // </recognitionContinuousWithFile>

                if (isSuccess)
                {
                    File.Move(audioFile, processedFilesPath + "//" + audiofileName);
                }
                else
                {
                    File.Move(audioFile, failedFilesPath + "//" + audiofileName);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                lblError.Text = ex.Message;
                //MessageBox.Show(ex.Message);
            }

            return isSuccess;           
        }
        #endregion ConvertSpeechToText - SDK

        #region SaveConvertedTextToFile
        /// <summary>
        /// Saves converted text to a file
        /// </summary>
        private void SaveConvertedTextToFile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rtbResult.Text))
                    return;

                string textFileNameWithPath = processedFilesPath + Path.GetFileNameWithoutExtension(audioFileNameWithPath) + ".txt";
                if (File.Exists(textFileNameWithPath))
                    File.Delete(textFileNameWithPath);

                using (TextWriter txtWriter = new StreamWriter(textFileNameWithPath))
                {
                    txtWriter.Write(rtbResult.Text);
                    txtWriter.Close();
                }
            }
            catch (Exception ex)
            {
                string exMsg = ex.Message;
            }
        }
        #endregion SaveConvertedTextToFile

        #region Print             
        private void OnPrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(printText))
                    return;

                printText = PrintLib.PrintPage(printText, e.Graphics, printFont, Brushes.Black, e.MarginBounds, TextJustification.Full);

                // Check to see if more pages are to be printed.
                e.HasMorePages = (printText.Length > 0);
            }
            catch (Exception ex)
            {
                string exMsg = ex.Message;
            }
        }
        #endregion Print  
    }

}

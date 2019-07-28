using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
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
using System.Drawing.Printing;

namespace WinSpeechToText
{
    /// <summary>
    /// Converts selected audio file (*.wav) to text
    /// </summary>
    public partial class FormProcessAudioFile : Form
    {
        private string subscriptionKey = ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionKey"];
        private string subscriptionRegion = ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionRegion"];
        private string fetchUri = "https://" + ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionRegion"] + ".api.cognitive.microsoft.com/sts/v1.0";
        private string requestUri = "https://" + ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionRegion"] + ".stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US";
        private string host = ConfigurationManager.AppSettings["MiscrosoftSpeechAPI_SubscriptionRegion"] + ".stt.speech.microsoft.com";
        private int audioSampleRate = Convert.ToInt32(ConfigurationManager.AppSettings["AudioSampleRate"]);
        private int speechServiceType = Convert.ToInt32(ConfigurationManager.AppSettings["SpeechServiceType"]); //1:SDK, 2:API
        private string processedFilesPath = ConfigurationManager.AppSettings["ProcessedFilesPath"];
        private int enableAutoSaveReponseToFile = Convert.ToInt32(ConfigurationManager.AppSettings["EnableAutoSaveReponseToFile"]);
        private int enablePrint = Convert.ToInt32(ConfigurationManager.AppSettings["EnablePrint"]);

        private Font printFont = new Font("Courier New", 12);
        private string printText = "";
        private string audioFileNameWithPath = "";        
        private SoundPlayer soundPlayer;

        public FormProcessAudioFile()
        {
            InitializeComponent();            
        }

        /// <summary>
        /// Form Load event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormProcessAudioFile_Load(object sender, EventArgs e)
        {
            lblError.Text = "";
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
        /// To browse and select an audio file for converting to text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            StopAudioPlayer();

            openFileDialog1.Filter = "wav files (*.wav)|*.wav";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                tbSelectedFile.Text = openFileDialog1.FileName;
                audioFileNameWithPath = openFileDialog1.FileName;
                tbSelectedFile.Select(tbSelectedFile.Text.Length, 0);

                SetControlsState(1);
            }
            else
            {
                SetControlsState(0);
            }
        }

        /// <summary>
        /// Plays selected audio file
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
        /// Converts audio file to text
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
                SetControlsState(2);
                pbProcessing.Visible = true;

                if (speechServiceType == (int)SpeechServiceType.API)
                {
                    string token = await FetchToken();
                    isConversionSuccess = ConvertSpeechToText(audioFileNameWithPath, token);
                }
                else
                {
                    isConversionSuccess = await ContinuousRecognitionWithFileAsync(audioFileNameWithPath);
                    //rtbResult.Text = result.ToString();
                }

                if (isConversionSuccess)
                {
                    if (enableAutoSaveReponseToFile == 1)
                        SaveConvertedTextToFile();
                }

                SetControlsState(3);
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
                StopAudioPlayer();                
                lblError.Text = "";
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                //MessageBox.Show(ex.Message);
            }

            SetControlsState(0);
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
                /*
                // A4 papersize
                PaperSize ps = new PaperSize();
                ps.RawKind = (int)PaperKind.A4;
                printDocument1.DefaultPageSettings.PaperSize = ps;
                */
                // Create a new instance of Margins with 1-inch margins.
                Margins margins = new Margins(100, 100, 100, 100);
                printDocument1.DefaultPageSettings.Margins = margins;

                // potrait mode
                printDocument1.DefaultPageSettings.Landscape = true;
                
                printDocument1.Print();
            }

            printText = "";
        }
        #endregion FormControl Events

        /// <summary>
        /// Sets Controls state as per provided flag. 0: initial state, 1: file selected state, 2: conversion inprogess state, 3: converted state
        /// </summary>
        /// <param name="flag"></param>
        private void SetControlsState(int flag)
        {
            switch (flag)
            {
                case 0: // initial state
                    tbSelectedFile.Text = "";
                    audioFileNameWithPath = "";

                    btnSelectFile.Enabled = true;
                    btnPlay.Enabled = false;
                    btnConvert.Enabled = false;                   
                    pbProcessing.Visible = false;
                    btnPrint.Enabled = false;
                    rtbResult.Text = "";
                    printText = "";
                    break;

                case 1: // file selected
                    btnSelectFile.Enabled = true;
                    btnPlay.Enabled = true;
                    btnConvert.Enabled = true;
                    btnPrint.Enabled = false;
                    rtbResult.Text = "";
                    rtbResult.Enabled = false;
                    break;

                case 2: // conversion inprogess       
                    btnSelectFile.Enabled = false;
                    btnPlay.Enabled = false;
                    btnConvert.Enabled = false;
                    btnPrint.Enabled = false;
                    rtbResult.Enabled = false;
                    break;

                case 3: // converted state
                    tbSelectedFile.Text = "";
                    audioFileNameWithPath = "";

                    btnSelectFile.Enabled = true;
                    btnPlay.Enabled = false;
                    btnConvert.Enabled = false;
                    pbProcessing.Visible = false;
                    btnPrint.Enabled = true;
                    rtbResult.Enabled = true;
                    break;

                default:
                    break;
            }

            pbProcessing.Visible = false;
        }        

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
        /// Converts audio file to text using Microoft Speech API
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
                        }
                        else
                        {
                            lblError.Text = "Something went wrong, please try again..";
                            //MessageBox.Show("Something went wrong, please try again..");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                //MessageBox.Show(ex.Message);
            }

            if (isSuccess)
                rtbResult.Text = result;

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
            catch(Exception ex)
            {
                string exMsg = ex.Message;
            }
        }

        // Draw justified text on the Graphics
        // object in the indicated Rectangle.
        private RectangleF DrawParagraphs(string text, Graphics gr,
            Font font, Brush brush, RectangleF rect,
            TextJustification justification, float line_spacing,
            float indent, float paragraph_spacing)
        {
            // Split the text into paragraphs.
            string[] paragraphs = text.Split('\n');

            // Draw each paragraph.
            foreach (string paragraph in paragraphs)
            {
                // Draw the paragraph keeping track of remaining space.
                rect = DrawParagraph(gr, rect, font, brush,
                    paragraph, justification, line_spacing,
                    indent, paragraph_spacing);

                // See if there's any room left.
                if (rect.Height < font.Size) break;
            }

            return rect;
        }

        // Draw a paragraph by lines inside the Rectangle.
        // Return a RectangleF representing any unused
        // space in the original RectangleF.
        private RectangleF DrawParagraph(Graphics gr, RectangleF rect,
            Font font, Brush brush, string text,
            TextJustification justification, float line_spacing,
            float indent, float extra_paragraph_spacing)
        {
            // Get the coordinates for the first line.
            float y = rect.Top;

            // Break the text into words.
            string[] words = text.Split(' ');
            int start_word = 0;

            // Repeat until we run out of text or room.
            for (; ; )
            {
                // See how many words will fit.
                // Start with just the next word.
                string line = words[start_word];

                // Add more words until the line won't fit.
                int end_word = start_word + 1;
                while (end_word < words.Length)
                {
                    // See if the next word fits.
                    string test_line = line + " " + words[end_word];
                    SizeF line_size = gr.MeasureString(test_line, font);
                    if (line_size.Width > rect.Width)
                    {
                        // The line is too wide. Don't use the last word.
                        end_word--;
                        break;
                    }
                    else
                    {
                        // The word fits. Save the test line.
                        line = test_line;
                    }

                    // Try the next word.
                    end_word++;
                }

                // See if this is the last line in the paragraph.
                if ((end_word == words.Length) &&
                    (justification == TextJustification.Full))
                {
                    // This is the last line. Don't justify it.
                    DrawLine(gr, line, font, brush,
                        rect.Left + indent,
                        y,
                        rect.Width - indent,
                        TextJustification.Left);
                }
                else
                {
                    // This is not the last line. Justify it.
                    DrawLine(gr, line, font, brush,
                        rect.Left + indent,
                        y,
                        rect.Width - indent,
                        justification);
                }

                // Move down to draw the next line.
                y += font.Height * line_spacing;

                // Make sure there's room for another line.
                if (font.Size > rect.Height) break;

                // Start the next line at the next word.
                start_word = end_word + 1;
                if (start_word >= words.Length) break;

                // Don't indent subsequent lines in this paragraph.
                indent = 0;
            }

            // Add a gap after the paragraph.
            y += font.Height * extra_paragraph_spacing;

            // Return a RectangleF representing any unused
            // space in the original RectangleF.
            float height = rect.Bottom - y;
            if (height < 0) height = 0;
            return new RectangleF(rect.X, y, rect.Width, height);
        }
        // Draw a line of text.
        private void DrawLine(Graphics gr, string line, Font font,
            Brush brush, float x, float y, float width,
            TextJustification justification)
        {
            // Make a rectangle to hold the text.
            RectangleF rect = new RectangleF(x, y, width, font.Height);

            // See if we should use full justification.
            if (justification == TextJustification.Full)
            {
                // Justify the text.
                DrawJustifiedLine(gr, rect, font, brush, line);
            }
            else
            {
                // Make a StringFormat to align the text.
                using (StringFormat sf = new StringFormat())
                {
                    // Use the appropriate alignment.
                    switch (justification)
                    {
                        case TextJustification.Left:
                            sf.Alignment = StringAlignment.Near;
                            break;
                        case TextJustification.Right:
                            sf.Alignment = StringAlignment.Far;
                            break;
                        case TextJustification.Center:
                            sf.Alignment = StringAlignment.Center;
                            break;
                    }

                    gr.DrawString(line, font, brush, rect, sf);
                }
            }
        }

        // Draw justified text on the Graphics object
        // in the indicated Rectangle.
        private void DrawJustifiedLine(Graphics gr, RectangleF rect,
            Font font, Brush brush, string text)
        {
            // Break the text into words.
            string[] words = text.Split(' ');

            // Add a space to each word and get their lengths.
            float[] word_width = new float[words.Length];
            float total_width = 0;
            for (int i = 0; i < words.Length; i++)
            {
                // See how wide this word is.
                SizeF size = gr.MeasureString(words[i], font);
                word_width[i] = size.Width;
                total_width += word_width[i];
            }

            // Get the additional spacing between words.
            float extra_space = rect.Width - total_width;
            int num_spaces = words.Length - 1;
            if (words.Length > 1) extra_space /= num_spaces;

            // Draw the words.
            float x = rect.Left;
            float y = rect.Top;
            for (int i = 0; i < words.Length; i++)
            {
                // Draw the word.
                gr.DrawString(words[i], font, brush, x, y);

                // Move right to draw the next word.
                x += word_width[i] + extra_space;
            }
        }
        #endregion Print        
    }
}

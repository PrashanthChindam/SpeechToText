namespace WinSpeechToText
{
    partial class FormProcessAudioFile
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormProcessAudioFile));
            this.panelMain = new System.Windows.Forms.Panel();
            this.btnPrint = new System.Windows.Forms.Button();
            this.tbSelectedFile = new System.Windows.Forms.TextBox();
            this.btnSelectFile = new System.Windows.Forms.Button();
            this.btnPlay = new System.Windows.Forms.Button();
            this.rtbResult = new System.Windows.Forms.RichTextBox();
            this.btnConvert = new System.Windows.Forms.Button();
            this.pbProcessing = new System.Windows.Forms.PictureBox();
            this.lblError = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.printDialog1 = new System.Windows.Forms.PrintDialog();
            this.printDocument1 = new System.Drawing.Printing.PrintDocument();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbProcessing)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.btnPrint);
            this.panelMain.Controls.Add(this.tbSelectedFile);
            this.panelMain.Controls.Add(this.btnSelectFile);
            this.panelMain.Controls.Add(this.btnPlay);
            this.panelMain.Controls.Add(this.rtbResult);
            this.panelMain.Controls.Add(this.btnConvert);
            this.panelMain.Location = new System.Drawing.Point(12, 5);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(403, 295);
            this.panelMain.TabIndex = 11;
            // 
            // btnPrint
            // 
            this.btnPrint.Location = new System.Drawing.Point(10, 267);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(75, 23);
            this.btnPrint.TabIndex = 24;
            this.btnPrint.Text = "Print";
            this.btnPrint.UseVisualStyleBackColor = true;
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // tbSelectedFile
            // 
            this.tbSelectedFile.BackColor = System.Drawing.Color.White;
            this.tbSelectedFile.Enabled = false;
            this.tbSelectedFile.Location = new System.Drawing.Point(10, 36);
            this.tbSelectedFile.Name = "tbSelectedFile";
            this.tbSelectedFile.Size = new System.Drawing.Size(263, 20);
            this.tbSelectedFile.TabIndex = 13;
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Location = new System.Drawing.Point(272, 34);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(120, 23);
            this.btnSelectFile.TabIndex = 13;
            this.btnSelectFile.Text = "Select Audio File..";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(10, 84);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(120, 23);
            this.btnPlay.TabIndex = 22;
            this.btnPlay.Text = "Play Recording";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // rtbResult
            // 
            this.rtbResult.Location = new System.Drawing.Point(10, 148);
            this.rtbResult.Name = "rtbResult";
            this.rtbResult.Size = new System.Drawing.Size(382, 115);
            this.rtbResult.TabIndex = 16;
            this.rtbResult.Text = "";
            // 
            // btnConvert
            // 
            this.btnConvert.Location = new System.Drawing.Point(272, 84);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(120, 23);
            this.btnConvert.TabIndex = 15;
            this.btnConvert.Text = "Convert To Text";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            // 
            // pbProcessing
            // 
            this.pbProcessing.BackColor = System.Drawing.Color.Transparent;
            this.pbProcessing.Image = ((System.Drawing.Image)(resources.GetObject("pbProcessing.Image")));
            this.pbProcessing.InitialImage = null;
            this.pbProcessing.Location = new System.Drawing.Point(187, 5);
            this.pbProcessing.Name = "pbProcessing";
            this.pbProcessing.Size = new System.Drawing.Size(36, 28);
            this.pbProcessing.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbProcessing.TabIndex = 21;
            this.pbProcessing.TabStop = false;
            // 
            // lblError
            // 
            this.lblError.AutoSize = true;
            this.lblError.ForeColor = System.Drawing.Color.Red;
            this.lblError.Location = new System.Drawing.Point(4, 4);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(73, 13);
            this.lblError.TabIndex = 22;
            this.lblError.Text = "error message";
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.lblError);
            this.panel1.Controls.Add(this.pbProcessing);
            this.panel1.Location = new System.Drawing.Point(13, 301);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(403, 54);
            this.panel1.TabIndex = 12;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // printDialog1
            // 
            this.printDialog1.Document = this.printDocument1;
            this.printDialog1.UseEXDialog = true;
            // 
            // printDocument1
            // 
            this.printDocument1.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.OnPrintPage);
            // 
            // FormProcessAudioFile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(426, 357);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panelMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FormProcessAudioFile";
            this.Text = "SpeechToText";
            this.Load += new System.EventHandler(this.FormProcessAudioFile_Load);
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbProcessing)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.PictureBox pbProcessing;
        private System.Windows.Forms.RichTextBox rtbResult;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.TextBox tbSelectedFile;
        private System.Windows.Forms.PrintDialog printDialog1;
        private System.Drawing.Printing.PrintDocument printDocument1;
        private System.Windows.Forms.Button btnPrint;
    }
}


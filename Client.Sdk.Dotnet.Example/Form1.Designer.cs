namespace Client.Sdk.Dotnet.Example
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            webrtcPanel = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)webrtcPanel).BeginInit();
            SuspendLayout();
            // 
            // webrtcPanel
            // 
            webrtcPanel.BackColor = Color.Black;
            webrtcPanel.Dock = DockStyle.Fill;
            webrtcPanel.Location = new Point(0, 0);
            webrtcPanel.Name = "webrtcPanel";
            webrtcPanel.Size = new Size(800, 450);
            webrtcPanel.TabIndex = 0;
            webrtcPanel.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(webrtcPanel);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)webrtcPanel).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox webrtcPanel;
    }
}

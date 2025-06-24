namespace Client.Sdk.Dotnet.Example
{
    partial class Form2
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
            panelVideoContainer = new TableLayoutPanel();
            groupBox1 = new GroupBox();
            openAudio = new Button();
            openVideo = new Button();
            panel1 = new Panel();
            webcamera = new PictureBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            speaker = new Label();
            microphone = new Label();
            Identity = new Label();
            panelVideoContainer.SuspendLayout();
            groupBox1.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webcamera).BeginInit();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // panelVideoContainer
            // 
            panelVideoContainer.ColumnCount = 2;
            panelVideoContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panelVideoContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panelVideoContainer.Controls.Add(groupBox1, 1, 2);
            panelVideoContainer.Controls.Add(panel1, 0, 2);
            panelVideoContainer.Dock = DockStyle.Fill;
            panelVideoContainer.Location = new Point(0, 0);
            panelVideoContainer.Name = "panelVideoContainer";
            panelVideoContainer.RowCount = 3;
            panelVideoContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.Size = new Size(784, 561);
            panelVideoContainer.TabIndex = 1;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(openAudio);
            groupBox1.Controls.Add(openVideo);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Location = new Point(395, 377);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(386, 181);
            groupBox1.TabIndex = 3;
            groupBox1.TabStop = false;
            groupBox1.Text = "Action";
            // 
            // openAudio
            // 
            openAudio.Location = new Point(99, 22);
            openAudio.Name = "openAudio";
            openAudio.Size = new Size(93, 34);
            openAudio.TabIndex = 1;
            openAudio.Text = "openAudio";
            openAudio.UseVisualStyleBackColor = true;
            openAudio.Click += openAudio_Click;
            // 
            // openVideo
            // 
            openVideo.Location = new Point(6, 22);
            openVideo.Name = "openVideo";
            openVideo.Size = new Size(87, 34);
            openVideo.TabIndex = 0;
            openVideo.Text = "openVideo";
            openVideo.UseVisualStyleBackColor = true;
            openVideo.Click += openVideo_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(webcamera);
            panel1.Controls.Add(tableLayoutPanel1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 377);
            panel1.Name = "panel1";
            panel1.Size = new Size(386, 181);
            panel1.TabIndex = 6;
            // 
            // webcamera
            // 
            webcamera.Dock = DockStyle.Fill;
            webcamera.Location = new Point(0, 0);
            webcamera.Name = "webcamera";
            webcamera.Size = new Size(386, 138);
            webcamera.TabIndex = 4;
            webcamera.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 4;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.Controls.Add(speaker, 3, 0);
            tableLayoutPanel1.Controls.Add(microphone, 1, 0);
            tableLayoutPanel1.Controls.Add(Identity, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Bottom;
            tableLayoutPanel1.Location = new Point(0, 138);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(386, 43);
            tableLayoutPanel1.TabIndex = 5;
            // 
            // speaker
            // 
            speaker.AutoSize = true;
            speaker.Dock = DockStyle.Fill;
            speaker.Location = new Point(291, 0);
            speaker.Name = "speaker";
            speaker.Size = new Size(92, 43);
            speaker.TabIndex = 5;
            speaker.Text = "speaking:false";
            speaker.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // microphone
            // 
            microphone.AutoSize = true;
            microphone.Dock = DockStyle.Fill;
            microphone.Location = new Point(99, 0);
            microphone.Name = "microphone";
            microphone.Size = new Size(90, 43);
            microphone.TabIndex = 2;
            microphone.Text = "micro:Closed";
            microphone.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Identity
            // 
            Identity.AutoSize = true;
            Identity.Dock = DockStyle.Fill;
            Identity.Location = new Point(3, 0);
            Identity.Name = "Identity";
            Identity.Size = new Size(90, 43);
            Identity.TabIndex = 1;
            Identity.Text = "Identity";
            Identity.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(panelVideoContainer);
            Name = "Form2";
            Text = "Form2";
            panelVideoContainer.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webcamera).EndInit();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel panelVideoContainer;
        private Button openVideo;
        private GroupBox groupBox1;
        private Button openAudio;
        private PictureBox webcamera;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private Label Identity;
        private Label microphone;
        private Label speaker;
    }
}
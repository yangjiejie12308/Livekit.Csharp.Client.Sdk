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
            panelVideoContainer = new TableLayoutPanel();
            SuspendLayout();
            // 
            // panelVideoContainer
            // 
            panelVideoContainer.ColumnCount = 3;
            panelVideoContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.Dock = DockStyle.Fill;
            panelVideoContainer.Location = new Point(0, 0);
            panelVideoContainer.Name = "panelVideoContainer";
            panelVideoContainer.RowCount = 3;
            panelVideoContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            panelVideoContainer.Size = new Size(935, 502);
            panelVideoContainer.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(935, 502);
            Controls.Add(panelVideoContainer);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel panelVideoContainer;
    }
}

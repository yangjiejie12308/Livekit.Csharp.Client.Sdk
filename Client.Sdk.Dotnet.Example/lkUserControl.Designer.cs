namespace Client.Sdk.Dotnet.Example
{
    partial class lkUserControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            con = new TableLayoutPanel();
            Quality = new Label();
            microphone = new Label();
            Identity = new Label();
            videoPanels = new TableLayoutPanel();
            speaker = new Label();
            con.SuspendLayout();
            SuspendLayout();
            // 
            // con
            // 
            con.ColumnCount = 4;
            con.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            con.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            con.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            con.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            con.Controls.Add(speaker, 3, 0);
            con.Controls.Add(Quality, 2, 0);
            con.Controls.Add(microphone, 1, 0);
            con.Controls.Add(Identity, 0, 0);
            con.Dock = DockStyle.Bottom;
            con.Location = new Point(0, 378);
            con.Name = "con";
            con.RowCount = 1;
            con.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            con.Size = new Size(441, 41);
            con.TabIndex = 3;
            // 
            // Quality
            // 
            Quality.AutoSize = true;
            Quality.Dock = DockStyle.Fill;
            Quality.Location = new Point(223, 0);
            Quality.Name = "Quality";
            Quality.Size = new Size(104, 41);
            Quality.TabIndex = 2;
            Quality.Text = "quality:0";
            Quality.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // microphone
            // 
            microphone.AutoSize = true;
            microphone.Dock = DockStyle.Fill;
            microphone.Location = new Point(113, 0);
            microphone.Name = "microphone";
            microphone.Size = new Size(104, 41);
            microphone.TabIndex = 1;
            microphone.Text = "micro:Closed";
            microphone.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Identity
            // 
            Identity.AutoSize = true;
            Identity.Dock = DockStyle.Fill;
            Identity.Location = new Point(3, 0);
            Identity.Name = "Identity";
            Identity.Size = new Size(104, 41);
            Identity.TabIndex = 0;
            Identity.Text = "Identity";
            Identity.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // videoPanels
            // 
            videoPanels.ColumnCount = 2;
            videoPanels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            videoPanels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            videoPanels.Dock = DockStyle.Fill;
            videoPanels.Location = new Point(0, 0);
            videoPanels.Name = "videoPanels";
            videoPanels.RowCount = 2;
            videoPanels.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            videoPanels.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            videoPanels.Size = new Size(441, 378);
            videoPanels.TabIndex = 4;
            // 
            // speaker
            // 
            speaker.AutoSize = true;
            speaker.Dock = DockStyle.Fill;
            speaker.Location = new Point(333, 0);
            speaker.Name = "speaker";
            speaker.Size = new Size(105, 41);
            speaker.TabIndex = 3;
            speaker.Text = "speaking:false";
            speaker.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lkUserControl
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(videoPanels);
            Controls.Add(con);
            Name = "lkUserControl";
            Size = new Size(441, 419);
            con.ResumeLayout(false);
            con.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel con;
        private Label Identity;
        private TableLayoutPanel videoPanels;
        private Label microphone;
        private Label Quality;
        private Label speaker;
    }
}

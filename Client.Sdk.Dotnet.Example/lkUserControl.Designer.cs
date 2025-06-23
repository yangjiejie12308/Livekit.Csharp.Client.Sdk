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
            tableLayoutPanel2 = new TableLayoutPanel();
            microphone = new Label();
            Identity = new Label();
            videoPanels = new TableLayoutPanel();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 3;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel2.Controls.Add(microphone, 1, 0);
            tableLayoutPanel2.Controls.Add(Identity, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Bottom;
            tableLayoutPanel2.Location = new Point(0, 378);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(441, 41);
            tableLayoutPanel2.TabIndex = 3;
            // 
            // microphone
            // 
            microphone.AutoSize = true;
            microphone.Dock = DockStyle.Fill;
            microphone.Location = new Point(149, 0);
            microphone.Name = "microphone";
            microphone.Size = new Size(140, 41);
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
            Identity.Size = new Size(140, 41);
            Identity.TabIndex = 0;
            Identity.Text = "Identity";
            Identity.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // videoPanels
            // 
            videoPanels.ColumnCount = 3;
            videoPanels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            videoPanels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            videoPanels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            videoPanels.Dock = DockStyle.Fill;
            videoPanels.Location = new Point(0, 0);
            videoPanels.Name = "videoPanels";
            videoPanels.RowCount = 3;
            videoPanels.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            videoPanels.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            videoPanels.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            videoPanels.Size = new Size(441, 378);
            videoPanels.TabIndex = 4;
            // 
            // lkUserControl
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(videoPanels);
            Controls.Add(tableLayoutPanel2);
            Name = "lkUserControl";
            Size = new Size(441, 419);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel2;
        private Label Identity;
        private TableLayoutPanel videoPanels;
        private Label microphone;
    }
}

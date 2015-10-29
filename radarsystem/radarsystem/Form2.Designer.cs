namespace radarsystem
{
    partial class FrequencyForm
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
            this.frequentpanel = new System.Windows.Forms.Panel();
            this.xpanel = new System.Windows.Forms.Panel();
            this.ypanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // frequentpanel
            // 
            this.frequentpanel.BackColor = System.Drawing.Color.LightGreen;
            this.frequentpanel.Location = new System.Drawing.Point(54, 42);
            this.frequentpanel.Name = "frequentpanel";
            this.frequentpanel.Size = new System.Drawing.Size(502, 460);
            this.frequentpanel.TabIndex = 0;
            this.frequentpanel.Paint += new System.Windows.Forms.PaintEventHandler(this.frequentpanel_Paint);
            // 
            // xpanel
            // 
            this.xpanel.Location = new System.Drawing.Point(54, 5);
            this.xpanel.Name = "xpanel";
            this.xpanel.Size = new System.Drawing.Size(502, 31);
            this.xpanel.TabIndex = 1;
            this.xpanel.Paint += new System.Windows.Forms.PaintEventHandler(this.xpanel_Paint);
            // 
            // ypanel
            // 
            this.ypanel.Location = new System.Drawing.Point(12, 42);
            this.ypanel.Name = "ypanel";
            this.ypanel.Size = new System.Drawing.Size(36, 460);
            this.ypanel.TabIndex = 0;
            this.ypanel.Paint += new System.Windows.Forms.PaintEventHandler(this.ypanel_Paint);
            // 
            // FrequencyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(568, 514);
            this.Controls.Add(this.ypanel);
            this.Controls.Add(this.xpanel);
            this.Controls.Add(this.frequentpanel);
            this.Name = "FrequencyForm";
            this.Text = "频率分析";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel frequentpanel;
        private System.Windows.Forms.Panel xpanel;
        private System.Windows.Forms.Panel ypanel;
    }
}
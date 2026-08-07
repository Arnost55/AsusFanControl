namespace AsusFanControlGUI
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = Theme.Window;
            this.ClientSize = new System.Drawing.Size(1280, 840);
            this.Font = Theme.BodyFont;
            this.ForeColor = Theme.Text;
            var iconPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "propeller.ico");
            this.Icon = System.IO.File.Exists(iconPath)
                ? new System.Drawing.Icon(iconPath)
                : System.Drawing.SystemIcons.Application;
            this.MinimumSize = new System.Drawing.Size(980, 720);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Asus Fan Control";
            this.ResumeLayout(false);
        }
    }
}

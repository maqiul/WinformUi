namespace WinformUi.Demo
{
    partial class MainForm
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.fluentMenuStrip1 = new WinformUi.Controls.FluentMenuStrip();
            this.fluentNavigationView1 = new WinformUi.Controls.FluentNavigationView();
            ((System.ComponentModel.ISupportInitialize)this.bindingSource1).BeginInit();
            this.SuspendLayout();
            //
            // fluentMenuStrip1
            //
            this.fluentMenuStrip1.Location = new System.Drawing.Point(0, 0);
            this.fluentMenuStrip1.Name = "fluentMenuStrip1";
            this.fluentMenuStrip1.Size = new System.Drawing.Size(1160, 26);
            this.fluentMenuStrip1.TabIndex = 0;
            //
            // fluentNavigationView1
            //
            this.fluentNavigationView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fluentNavigationView1.Location = new System.Drawing.Point(0, 26);
            this.fluentNavigationView1.Name = "fluentNavigationView1";
            this.fluentNavigationView1.PaneWidth = 190;
            this.fluentNavigationView1.Size = new System.Drawing.Size(1160, 772);
            this.fluentNavigationView1.TabIndex = 1;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(1160, 820);
            this.Controls.Add(this.fluentNavigationView1);
            this.Controls.Add(this.fluentMenuStrip1);
            this.MainMenuStrip = this.fluentMenuStrip1;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WinformUi Demo (.NET 4.6)";
            ((System.ComponentModel.ISupportInitialize)this.bindingSource1).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private WinformUi.Controls.FluentMenuStrip fluentMenuStrip1;
        private WinformUi.Controls.FluentNavigationView fluentNavigationView1;
        private System.Windows.Forms.BindingSource bindingSource1;
    }
}

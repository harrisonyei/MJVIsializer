namespace WindowsFormsApplication1
{
    partial class EditPatternForm
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.AddProperty = new System.Windows.Forms.Button();
            this.DeleteProp = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(12, 12);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 20);
            this.comboBox1.TabIndex = 0;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 39);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(564, 102);
            this.flowLayoutPanel1.TabIndex = 1;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // AddProperty
            // 
            this.AddProperty.Location = new System.Drawing.Point(140, 12);
            this.AddProperty.Name = "AddProperty";
            this.AddProperty.Size = new System.Drawing.Size(75, 23);
            this.AddProperty.TabIndex = 2;
            this.AddProperty.Text = "AddNew";
            this.AddProperty.UseVisualStyleBackColor = true;
            this.AddProperty.Click += new System.EventHandler(this.AddProperty_Click);
            // 
            // DeleteProp
            // 
            this.DeleteProp.Location = new System.Drawing.Point(221, 12);
            this.DeleteProp.Name = "DeleteProp";
            this.DeleteProp.Size = new System.Drawing.Size(75, 23);
            this.DeleteProp.TabIndex = 3;
            this.DeleteProp.Text = "Delete";
            this.DeleteProp.UseVisualStyleBackColor = true;
            this.DeleteProp.Click += new System.EventHandler(this.DeleteProp_Click);
            // 
            // EditPatternForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(588, 153);
            this.Controls.Add(this.DeleteProp);
            this.Controls.Add(this.AddProperty);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.comboBox1);
            this.Name = "EditPatternForm";
            this.Text = "EditPatternForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button AddProperty;
        private System.Windows.Forms.Button DeleteProp;
    }
}
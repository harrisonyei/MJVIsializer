using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    enum mjprops{
        A3,
        B3,
        C3,
        A123,
        B123,
        C123,
        CFB,
        None
    }
    public partial class EditPatternForm : Form
    {
        Image[] images = new Image[] { Properties.Resources.A3, Properties.Resources.B3, Properties.Resources.C3,
        Properties.Resources.A123,Properties.Resources.B123,Properties.Resources.C123,Properties.Resources.CFB};
        FlowLayoutPanel propsPanel;
        List<int>[] propsArr;
        List<int> curProps;
        Size btnSize;
        public EditPatternForm()
        {
            InitializeComponent();
            propsPanel = flowLayoutPanel1;
            btnSize = new Size(75,81);
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        public void UpdateFormData(string[] combNames,List<int>[] _propsArr)
        {
            comboBox1.Items.Clear();
            foreach(string name in combNames)
            {
                comboBox1.Items.Add(name);
            }
            propsArr = _propsArr;
            comboBox1.SelectedIndex = 0;
        }

        private void AddProperty_Click(object sender, EventArgs e)
        {
            Button newbtn = new Button();
            newbtn.Click += Button_Click;
            newbtn.Size = btnSize;
            propsPanel.Controls.Add(newbtn);
            curProps.Add(0);
            newbtn.BackgroundImageLayout = ImageLayout.Stretch;
            newbtn.BackgroundImage = images[0];
        }
        private void DeleteProp_Click(object sender, EventArgs e)
        {
            if (propsPanel.Controls.Count > 1)
            {
                propsPanel.Controls.RemoveAt(propsPanel.Controls.Count - 1);
                curProps.RemoveAt(curProps.Count- 1);
            }
        }

        void Button_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int idx = propsPanel.Controls.IndexOf(btn);
            if(idx >= 0)
            {
                int pIdx = ((curProps[idx] + 1) % (int)mjprops.None);
                curProps[idx] = pIdx;
                btn.BackgroundImage = images[pIdx];
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx  = comboBox1.SelectedIndex;
            curProps = propsArr[idx];
            RefreshFromProps();
        }

        void RefreshFromProps()
        {
            propsPanel.Controls.Clear();
            foreach (int p in curProps)
            {
                Button newbtn = new Button();
                newbtn.Click += Button_Click;
                newbtn.Size   = btnSize;
                propsPanel.Controls.Add(newbtn);
                newbtn.BackgroundImageLayout = ImageLayout.Stretch;
                newbtn.BackgroundImage = images[p];
            }
        }
    }
}

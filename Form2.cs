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
    public partial class Form2 : Form
    {
        const int NUM_SPRITES   = 42;
        private Image[] sprites = new Image[NUM_SPRITES];
        private PictureBox[] picBoxes;
        public Form2()
        {
            InitializeComponent();
            picBoxes = Controls.OfType<PictureBox>().OrderBy(box => box.Name).ToArray();
            LoadSprites("./Src/mjSpriteSheet612x446.jpg");
            label1.Text = "";
            label2.Text = "";
        }

        List<int[]> datas = null;
        int idx           = 0;
        int player        = 0;

        public void UpdateData(List<int[]> _datas,int _idx,int _player,int d,int t,float p)
        {
            datas  = _datas;
            idx    = _idx;
            player = _player;

            label1.Text = "Player " + player + "  dist: "+d+"  tai: "+t+"  p: "+p;
            label2.Text = "Round "  + idx;
            updateImages(idx);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            
        }

        void updateImages(int _idx)
        {
            int count = 0;
            for(int i = 0; i < 3; i++)
            {
                int cards = datas[_idx][i];
                for (int j = 0; j < 9; j++)
                {
                    int c_count = ((cards >> (j * 3)) & 7);
                    for(int c =0;c< c_count; c++)
                    {
                        picBoxes[count].Image = sprites[i*9+j];
                        count += 1;
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // left
            idx = (idx + datas.Count - 1) % datas.Count;
            label2.Text = "Round " + idx;
            updateImages(idx);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // right
            idx = (idx + 1) % datas.Count;
            label2.Text = "Round " + idx;
            updateImages(idx);
        }
        private bool LoadSprites(string fileName)
        {
            try
            {
                Image image = Bitmap.FromFile(fileName);
                idx = 0;
                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i] = new Bitmap(pictureBox01.Width, pictureBox01.Height);
                    using (Graphics g = Graphics.FromImage(sprites[i]))
                        g.DrawImage(image, new Rectangle(0, 0, pictureBox01.Width, pictureBox01.Height),
                            new Rectangle((i%9)* pictureBox01.Width, (i/9)* pictureBox01.Height, 
                            pictureBox01.Width, pictureBox01.Height), GraphicsUnit.Pixel);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading file." + Environment.NewLine + ex.Message, "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
        }
    }
}

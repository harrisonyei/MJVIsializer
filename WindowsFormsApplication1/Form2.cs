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
        const int NUM_SPRITES   = 45;
        private Image[] sprites = new Image[NUM_SPRITES];
        private PictureBox[] picBoxes;
        public Form2()
        {
            InitializeComponent();
            picBoxes = Controls.OfType<PictureBox>().OrderBy(box => box.Name).ToArray();
            LoadSprites();
            label1.Text = "";
            label2.Text = "";
        }

        List<int[]> s_datas = null;
        List<int[]> e_datas = null;
        int idx           = 0;
        int player        = 0;

        public void UpdateData(List<int[]> _s_datas, List<int[]> _e_datas, int _idx,int _player,int d,int weightCount,char[][] probs,char[][] volts)
        {
            s_datas = _s_datas;
            e_datas = _e_datas;
            idx    = _idx;
            player = _player;

            label1.Text = "Player: " + player + " dist: " + d;
            for(int i = 0; i < weightCount; i++)
            {
                label1.Text += ",  p" + i + ":  " + Math.Round(probs[player][i] / 100.0, 1);
            }

            for (int i = 0; i < weightCount; i++)
            {
                label1.Text += ",  v" + i + ":  " + Math.Round(probs[player][i] / 100.0, 1);
            }

            label2.Text = "Round "  + idx;
            updateImages(idx);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            
        }

        void updateImages(int _idx)
        {
            if(_idx < 0 || _idx >= s_datas.Count)
            {
                return;
            }

            int count = 0;
            int[] arr = new int[4] {0,1,2,4};

            for(int color = 0; color < 4; color++)
            {
                int i      = arr[color];
                int cards  = s_datas[_idx][color];
                for (int j = 0; j < 9; j++)
                {
                    int c_count = ((cards >> (j * 3)) & 7);
                    for (int c = 0; c < c_count; c++)
                    {
                        picBoxes[count].Image = sprites[i * 9 + j];
                        count += 1;
                    }
                }
            }

            for (int color = 0; color < 4; color++)
            {
                int i     = arr[color];
                int cards = e_datas[_idx][color];
                for (int j = 0; j < 9; j++)
                {
                    int c_count = ((cards >> (j * 3)) & 7);
                    for (int c = 0; c < c_count; c++)
                    {
                        picBoxes[count].Image = sprites[i * 9 + j];
                        count += 1;
                    }
                }
            }

            for(int i = count;i < 38; i++)
            {
                picBoxes[i].Image = null;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // left
            idx = (idx + s_datas.Count - 1) % s_datas.Count;
            label2.Text = "Round " + idx;
            updateImages(idx);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // right
            idx = (idx + 1) % s_datas.Count;
            label2.Text = "Round " + idx;
            updateImages(idx);
        }

        private bool LoadSprites()
        {
            try
            {
                Image image = Properties.Resources.mjSpriteSheet612x446;
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

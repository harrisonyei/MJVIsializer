using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        class Player
        {
            public List<float> huRounds;
            public List<float> huTais;
            public List<float> huProbs;
            public List<int[]> hands;

            public Player()
            {
                huRounds = new List<float>();
                huTais = new List<float>();
                huProbs = new List<float>();
                hands = new List<int[]>();
            }

            public void Init()
            {
                huRounds.Clear();
                huTais.Clear();
                huProbs.Clear();
            }

            public void Append(int r, int t, float p)
            {
                huRounds.Add(r);
                huTais.Add(t);
                huProbs.Add(p);
            }

            public void AppendHand(int h0,int h1,int h2)
            {
                int[] hand = new int[3] { h0, h1, h2 };
                hands.Add(hand);
            }

            public float GetDataType(int datatype, int idx)
            {
                float result = 0;
                switch (datatype)
                {
                    case 0:
                        result = (huRounds[idx]) < 25 ? huRounds[idx] : 20;
                        break;
                    case 1:
                        result = huTais[idx];
                        break;
                    case 2:
                        result = huProbs[idx];
                        break;
                    case 3:
                        result = (huRounds[idx]) < 25 ? 1 : 0;
                        break;
                }
                return result;
            }
        }
        class LogData
        {
            public Series s;
            public float max;
            public float min;
            public LogData(Series _s,float _max,float _min)
            {
                s = _s;
                max = _max;
                min = _min;
            }
        }
        [DllImport("./exportcsharpDll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Simulation(ref int progress,string str,int rounds, int dtype, char d0, char d1, char d2, char d3, char t0, char t1, char t2, char t3, float p0, float p1, float p2, float p3);
        int rounds    = 100;
        int batchSize = 1;
        char[] d  = new char[4];
        char[] t  = new char[4];
        float[] p = new float[4];
        int playerIdx = 0;
        int curplayerIdx = 0;
        int dataType  = 0;
        int dataMode  = 0;
        int dealMode  = 0;
        List<float> displayData;
        Dictionary<string, LogData> cacheLogs;
        Player[] players;
        public Form1()
        {
            InitializeComponent();
            chart1.Series.Clear();
            chart1.Titles.Clear();
            displayData = new List<float>();
            players     = new Player[4];
            for(int i = 0; i < 4; i++)
            {
                players[i] = new Player();
            }
            cacheLogs = new Dictionary<string, LogData>();
            tooltip     = new ToolTip();
            tooltip.SetToolTip(chart1,"");
            tooltip.OwnerDraw = true;
            tooltip.Popup += new PopupEventHandler(toolTip1_Popup);
            tooltip.Draw += new DrawToolTipEventHandler(toolTip1_Draw);
            updateFormView();
            label8.Text = "Initilized!";
            progress = 0;
        }
        void LoadLogData(string fileName)
        {
            cacheLogs.Clear();
            using (BinaryReader binReader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
            {
                // Set Position to the beginning of the stream.
                binReader.BaseStream.Position = 0;
                // rounds int
                rounds   = binReader.ReadInt32();
                dealMode = binReader.ReadInt32();
                //player 1 ~ 4 ( dists, tais, probs)char char float
                for (int i = 0; i < 4; i++)
                {
                    players[i].Init();
                    d[i] = (char)binReader.ReadByte();
                    t[i] = (char)binReader.ReadByte();
                    p[i] = binReader.ReadSingle();
                }
                //per round
                //     player 1 ~ 4 ( huround, hutai, huprob)char char float
                for(int i = 0; i < rounds; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        int hand0 = binReader.ReadInt32();
                        int hand1 = binReader.ReadInt32();
                        int hand2 = binReader.ReadInt32();
                        players[j].AppendHand(hand0, hand1, hand2);
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        char huround = (char)binReader.ReadByte();
                        char hutai   = (char)binReader.ReadByte();
                        float huprob = binReader.ReadSingle();
                        players[j].Append(huround, hutai, huprob);
                    }
                }
                updateFormView();
                ReloadChart();
            }
        }
        // 0:all 1:player0..., 0:Round 1:Tai 2:Prob, 0:val 1:avg 2:std
        void ReloadChart()
        {
            curplayerIdx = playerIdx;
            string str = "";
            str += playerIdx;
            str += dataType;
            str += dataMode;
            str += batchSize;
            LogData item;
            chart1.Series.Clear();
            chart1.Titles.Clear();
            if (!cacheLogs.TryGetValue(str, out item))
            {
                float max;
                float min;
                updateDisplayData(out max,out min);
                chart1.ChartAreas[0].AxisY.Maximum = Math.Round(1.1*max,3);
                chart1.ChartAreas[0].AxisY.Minimum = Math.Round(0.9*min,3);
                chart1.ChartAreas[0].AxisX.Minimum = 0;
                //標題 最大數值
                Series series1 = new Series("數列");
                series1.YValueType = ChartValueType.Single;
                series1.XValueType = ChartValueType.Single;
                //設定線條顏色
                series1.Color = Color.Blue;
                //設定字型
                series1.Font = new Font("新細明體", 14);
                //折線圖
                series1.ChartType = SeriesChartType.FastLine;
                //將數值顯示在線上
                series1.IsValueShownAsLabel = false;
                //將數值新增至序列
                for (int index = 0; index < displayData.Count; index++)
                {
                    series1.Points.AddXY(index, displayData[index]);
                }
                //將序列新增到圖上
                chart1.Series.Add(series1);
                cacheLogs[str] = new LogData(series1,max,min);
            }
            else
            {
                LogData log = cacheLogs[str];
                chart1.ChartAreas[0].AxisY.Maximum = Math.Round(1.1 * log.max, 3);
                chart1.ChartAreas[0].AxisY.Minimum = Math.Round(0.9 * log.min, 3);
                chart1.ChartAreas[0].AxisX.Minimum = 0;
                chart1.Series.Add(log.s);
            }
        }

        float dataProcessMode(int mode,List<float> dataBatch)
        {
            float result = 0;
            if (dataBatch.Count() > 0)
            {
                if (mode == 0)
                {
                    result = dataBatch[dataBatch.Count-1];
                }
                else if(mode == 1)
                {
                    result = dataBatch.Average();
                }
                else if(mode == 2)
                {
                    //Compute the Average      
                    float avg = dataBatch.Average();
                    //Perform the Sum of (value-avg)_2_2      
                    double sum = dataBatch.Sum(d => Math.Pow(d - avg, 2));
                    //Put it all together      
                    result = (float)Math.Sqrt((sum) / (dataBatch.Count - 1));
                }
            }

            return result;
        }

        void updateDisplayData(out float max,out float min)
        {
            max = 0;
            min = 100;
            displayData.Clear();
            List<float> dataBatch = new List<float>();
            int total = players[0].huRounds.Count;
            for (int i = 0; i < total; i++)
            {
                float sum = 0;
                if (playerIdx == 0)
                {
                    for(int p = 0; p < 4; p++)
                    {
                        float r = players[p].GetDataType(dataType, i);
                        if(r >= 0)
                        {
                            sum += r;
                        }
                    }
                    sum /= 4;
                }
                else
                {
                    sum = players[playerIdx-1].GetDataType(dataType, i);
                }
                dataBatch.Add(sum);
                if ((i+1)%batchSize==0)
                {
                    float val = dataProcessMode(dataMode, dataBatch);
                    displayData.Add(val);
                    if(val > max)
                    {
                        max = val;
                    }
                    if(val < min)
                    {
                        min = val;
                    }
                }
            }
        }
        int progress = new int();
        string resultFileName = "";
        int runSim()
        {
            int err = Simulation(ref progress, resultFileName, rounds, dealMode, d[0], d[1], d[2], d[3], t[0], t[1], t[2], t[3], p[0], p[1], p[2], p[3]);
            if (err == -1)
            {
                resultFileName = "Error !";
            }
            return err;
        }

        int Progress()
        {
            while (progress <= 99)
            {
                if(progressBar1.Value < progress)
                {
                    progressBar1.Invoke(new Action(() =>
                    {
                        progressBar1.Value = progress;
                    }));
                }
            }
            return 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(progress != 0)
            {
                return;
            }
            if (rounds <= 0)
            { 
                return;
            }
            resultFileName = "MJ-" + DateTime.Now.ToString("MMddHHmmss") + ".MJlog";
            SimStart();
        }

        async void SimStart()
        {
            var mainTask    = new Task<int>(runSim);
            var progBarTask = new Task<int>(Progress);
            ChangeEnabled(false);
            progress = 0;
            progressBar1.Value = 0;
            mainTask.Start();
            progBarTask.Start();
            await progBarTask;
            await mainTask;
            progressBar1.Value = 100;
            progress = 0;
            LoadLogData(resultFileName);
            await Task.Delay(1000);
            ChangeEnabled(true);
            progressBar1.Value = 0;
        }

        void ChangeEnabled(bool enabled)
        {
            foreach (Control c in this.Controls)
            {
                c.Enabled = enabled;
            }
        }

        int lastIndex = 0;
        private void chart1_Click(object sender, EventArgs e)
        {
            if (curplayerIdx > 0)
            {
                Form2 form2 = new Form2();
                form2.UpdateData(players[curplayerIdx - 1].hands, lastIndex, curplayerIdx - 1, d[curplayerIdx - 1],t[curplayerIdx - 1],p[curplayerIdx - 1]);
                form2.Show();
            }
        }
        Point? prevPosition = null;
        ToolTip tooltip;
        private void toolTip1_Draw(object sender, DrawToolTipEventArgs e)
        {
            StringFormat sf = new StringFormat();
            sf.LineAlignment =
            StringAlignment.Center;
            sf.Alignment =
            StringAlignment.Center;
            e.DrawBackground();
            e.DrawBorder();
            e.Graphics.DrawString(e.ToolTipText, new Font("Arial", 15.0f), Brushes.Black, e.Bounds, sf);

        }
        void toolTip1_Popup(object sender, PopupEventArgs e)
        {
            // on popip set the size of tool tip
            e.ToolTipSize = TextRenderer.MeasureText("999.999", new Font("Arial", 15.0f));
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            if (chart1.Series.Count == 0)
                return;
            var pos = e.Location;
            if (prevPosition.HasValue && pos == prevPosition.Value)
                return;
            tooltip.RemoveAll();
            prevPosition = pos;
            
            var results = chart1.HitTest(pos.X, pos.Y, false,
                                            ChartElementType.DataPoint);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var prop = result.Object as DataPoint;
                    if (prop != null)
                    {
                        var pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        var pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);
                        if (Math.Abs(pos.X - pointXPixel) < 40 && Math.Abs(pos.Y - pointYPixel) < 40)
                        {
                            tooltip.Show(Math.Round(prop.YValues[0],3).ToString(), this.chart1, pos.X-5, pos.Y + 15,1200);
                            lastIndex = (int)prop.XValue;
                        }
                    }
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // chart data type change
            dataType = comboBox1.SelectedIndex;
        }

        private void configurationsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveConfigsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // save config
            SaveFileDialog sfDialog = new SaveFileDialog();
            sfDialog.Filter = "MJ config|*.MJconfig";
            sfDialog.Title  = "Save Config Data";
            sfDialog.ShowDialog();
            // If the file name is not an empty string open it for saving.  
            if (sfDialog.FileName != "")
            {
                // Saves the Image via a FileStream created by the OpenFile method.  
                using (System.IO.FileStream fs = (System.IO.FileStream)sfDialog.OpenFile())
                {
                    using (var fw = new StreamWriter(fs))
                    {
                        fw.WriteLine(rounds);
                        fw.WriteLine(dealMode);
                        fw.WriteLine(batchSize);
                        fw.WriteLine(playerIdx);
                        fw.WriteLine(dataType);
                        fw.WriteLine(dataMode);
                        for (int i = 0; i < 4; i++)
                        {
                            string line = "";
                            line += ((int)d[i]).ToString();
                            line += ",";
                            line += ((int)t[i]).ToString();
                            line += ",";
                            line += p[i].ToString();
                            fw.WriteLine(line);
                        }
                        fw.Flush(); // Added
                    }
                }
            }
        }

        private void loadConfigsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // load config
            OpenFileDialog ofDialog = new OpenFileDialog();
            ofDialog.Filter = "MJ config|*.MJconfig";
            ofDialog.Title = "Load Config Data";
            ofDialog.ShowDialog();
            // If the file name is not an empty string open it for saving.  
            if (ofDialog.FileName != "")
            {
                // Saves the Image via a FileStream created by the OpenFile method.  
                using (System.IO.FileStream fs = (System.IO.FileStream)ofDialog.OpenFile())
                {
                    using (var fr = new StreamReader(fs))
                    {
                        rounds   = int.Parse(fr.ReadLine());
                        dealMode = int.Parse(fr.ReadLine());
                        batchSize = int.Parse(fr.ReadLine());
                        playerIdx = int.Parse(fr.ReadLine());
                        dataType = int.Parse(fr.ReadLine());
                        dataMode = int.Parse(fr.ReadLine());
                        for (int i = 0; i < 4; i++)
                        {
                            string line = fr.ReadLine();
                            string[] playerConfig = line.Split(',');
                            d[i] = (char)int.Parse(playerConfig[0]);
                            t[i] = (char)int.Parse(playerConfig[1]);
                            p[i] = float.Parse(playerConfig[2]);
                        }
                    }
                }
                updateFormView();
            }
        }

        void updateFormView()
        {
            comboBox1.SelectedIndex = dataType;
            comboBox2.SelectedIndex = playerIdx;
            comboBox3.SelectedIndex = dataMode;

            textBox1.Text = ((int)d[0]).ToString();
            textBox6.Text = ((int)d[1]).ToString();
            textBox9.Text = ((int)d[2]).ToString();
            textBox12.Text = ((int)d[3]).ToString();

            textBox2.Text = ((int)t[0]).ToString();
            textBox5.Text = ((int)t[1]).ToString();
            textBox8.Text = ((int)t[2]).ToString();
            textBox11.Text = ((int)t[3]).ToString();

            textBox3.Text = (p[0]).ToString();
            textBox4.Text = (p[1]).ToString();
            textBox7.Text = (p[2]).ToString();
            textBox10.Text =(p[3]).ToString();

            textBox13.Text = rounds.ToString();
            textBox14.Text = batchSize.ToString();
            comboBox4.SelectedIndex = dealMode;
        }

        private void openLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // open simulation data
            // load config
            OpenFileDialog ofDialog = new OpenFileDialog();
            ofDialog.Filter = "MJ Log|*.MJlog";
            ofDialog.Title = "Load Simulation log";
            ofDialog.ShowDialog();
            // If the file name is not an empty string open it for saving.  
            if (ofDialog.FileName != "")
            {
                LoadLogData(ofDialog.FileName);
            }
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // chart player change
            playerIdx = comboBox2.SelectedIndex;
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // player 0 dist
            int dist = 0;
            d[0] = (char)0;
            if (int.TryParse(textBox1.Text,out dist))
            {
                if(dist > 0)
                {
                    d[0] = (char)dist;
                }
            }
        }
        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            // player 1 dist
            int dist = 0;
            d[1] = (char)0;
            if (int.TryParse(textBox6.Text, out dist))
            {
                if (dist > 0)
                {
                    d[1] = (char)dist;
                }
            }
        }
        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            // player 2 dist
            int dist = 0;
            if (int.TryParse(textBox9.Text, out dist))
            {
                if (dist > 0)
                {
                    d[2] = (char)dist;
                }
            }
        }
        private void textBox12_TextChanged(object sender, EventArgs e)
        {
            // player 3 dist
            int dist = 0;
            if (int.TryParse(textBox12.Text, out dist))
            {
                if (dist > 0)
                {
                    d[3] = (char)dist;
                }
            }
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            // player 0 tai
            int tai = 0;
            if (int.TryParse(textBox2.Text, out tai))
            {
                if (tai > 0)
                {
                    t[0] = (char)tai;
                }
            }
        }
        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            // player 1 tai
            int tai = 0;
            if (int.TryParse(textBox5.Text, out tai))
            {
                if (tai > 0)
                {
                    t[1] = (char)tai;
                }
            }
        }
        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            // player 2 tai
            int tai = 0;
            if (int.TryParse(textBox8.Text, out tai))
            {
                if (tai > 0)
                {
                    t[2] = (char)tai;
                }
            }
        }
        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            // player 3 tai
            int tai = 0;
            if (int.TryParse(textBox11.Text, out tai))
            {
                if (tai > 0)
                {
                    t[3] = (char)tai;
                }
            }
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            // player 0 prob
            float prob = 0;
            if (float.TryParse(textBox3.Text, out prob))
            {
                if (prob > 0)
                {
                    p[0] = prob;
                }
            }
        }
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            // player 1 prob
            float prob = 0;
            if (float.TryParse(textBox4.Text, out prob))
            {
                if (prob > 0)
                {
                    p[1] = prob;
                }
            }
        }
        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            // player 2 prob
            float prob = 0;
            if (float.TryParse(textBox7.Text, out prob))
            {
                if (prob > 0)
                {
                    p[2] = prob;
                }
            }
        }
        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            // player 3 prob
            float prob = 0;
            if (float.TryParse(textBox10.Text, out prob))
            {
                if (prob > 0)
                {
                    p[3] = prob;
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            // chart data mode change
            dataMode = comboBox3.SelectedIndex;
        }

        private void textBox13_TextChanged(object sender, EventArgs e)
        {
            // rounds
            int rnds = 0;
            if (int.TryParse(textBox13.Text, out rnds))
            {
                if (rnds > 0)
                {
                    rounds = rnds;
                }

                if(rnds > 100000)
                {
                    rounds = 100000;
                }
            }
        }

        private void textBox14_TextChanged(object sender, EventArgs e)
        {
            // batchSize
            int bts   = 1;
            if (int.TryParse(textBox14.Text, out bts))
            {
                if (bts > 0)
                {
                    batchSize = bts;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ReloadChart();
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            dealMode = comboBox4.SelectedIndex;
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Close(object sender, EventArgs e)
        {
            GC.KeepAlive(progress);
        }
    }
}

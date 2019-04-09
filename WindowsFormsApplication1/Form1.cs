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
            public List<int[]> hands;

            public Player()
            {
                huRounds = new List<float>();
                huTais   = new List<float>();
                hands    = new List<int[]>();
            }

            public void Init()
            {
                huRounds.Clear();
                huTais.Clear();
            }

            public void Append(int r, int t)
            {
                huRounds.Add(r);
                huTais.Add(t);
            }

            public void AppendHand(int h0,int h1,int h2,int h3)
            {
                int[] hand = new int[4] { h0, h1, h2 ,h3};
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
                        result = huTais[idx];
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
        [DllImport("./M16SimDll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Simulation(ref int p,string str,int rounds, int dtype,int patternSize, char[] dis, char[] tweight, char[] pweight,int[] patternSizes,int[] patterns);
        int rounds    = 100;
        int batchSize = 1;
        char[] d  = new char[4];
        char[][] t = new char[4][];
        char[][] p = new char[4][];
        int playerIdx = 0;
        int curplayerIdx = 0;
        int dataType  = 0;
        int dataMode  = 0;
        int dealMode  = 0;
        List <float> displayData;
        List<int>[] patterns;
        Dictionary<string, LogData> cacheLogs;
        Player[] players;
        const int WEIGHT_COUNT = 8;
        double WEIGHT_RADIUS = 0;
        string[] WEIGHT_TEXTS = new string[8] { "a","b","c","d","e","f", "g","h"};
        public Form1()
        {
            for(int i = 0; i < 4; i++)
            {
                t[i] = new char[WEIGHT_COUNT];
                p[i] = new char[WEIGHT_COUNT];
            }
            patterns = new List<int>[WEIGHT_COUNT];
            for(int i = 0; i < WEIGHT_COUNT; i++)
            {
                patterns[i] = new List<int>() {0};
            }
            InitializeComponent();
            chart1.Series.Clear();
            chart1.Titles.Clear();
            displayData = new List<float>();
            players     = new Player[4];
            weights     = new float[WEIGHT_COUNT];
            for(int i = 0;i<weights.Length;i++)
            {
                weights[i] = 50;
            }
            weightPoints = new PointF[WEIGHT_COUNT];
            background  = CreateCirclePoints(WEIGHT_COUNT, 100, pictureBox1.Width * .5f, pictureBox1.Height * .5f);
            background2 = CreateCirclePoints(WEIGHT_COUNT, 50, pictureBox1.Width * .5f, pictureBox1.Height * .5f);
            WEIGHT_RADIUS = 100;
            for (int i = 0; i < 4; i++)
            {
                players[i] = new Player();
            }
            cacheLogs = new Dictionary<string, LogData>();
            tooltip   = new ToolTip();
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
                rounds   = binReader.ReadInt32() * 16;
                dealMode = binReader.ReadInt32();
                for(int i = 0;i < WEIGHT_COUNT; i++)
                {
                    int n = binReader.ReadInt32();
                    patterns[i].Clear();
                    for (int j = 0; j < n; j++)
                    {
                        int pat = binReader.ReadInt32();
                        patterns[i].Add(pat);
                    }
                }
                //player 1 ~ 4 ( dists, tais, probs)char char float
                for (int i = 0; i < 4; i++)
                {
                    players[i].Init();
                    d[i] = (char)binReader.ReadByte();
                    for(int j = 0; j < WEIGHT_COUNT; j++)
                    {
                        t[i][j] = (char)binReader.ReadByte();
                    }
                    for (int j = 0; j < WEIGHT_COUNT; j++)
                    {
                        p[i][j] = (char)binReader.ReadByte();
                    }
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
                        int hand3 = binReader.ReadInt32();
                        players[j].AppendHand(hand0, hand1, hand2, hand3);
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        char huround = (char)binReader.ReadByte();
                        char hutai   = (char)binReader.ReadByte();
                        players[j].Append(huround, hutai);
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
                //標題 最大數值
                Series series1 = new Series("數列");
                series1.YValueType = ChartValueType.Single;
                series1.XValueType = ChartValueType.Single;
                //設定線條顏色
                series1.Color = Color.Blue;
                //設定字型
                series1.Font = new Font("新細明體", 14);
                updateDisplayData(out max,out min);
                chart1.ChartAreas[0].AxisY.Minimum = Math.Round(0.9 * min, 3);
                chart1.ChartAreas[0].AxisX.Minimum = 0;
                //折線圖
                if (dataType == 2)
                {
                    series1.ChartType = SeriesChartType.Column;
                    series1.IsValueShownAsLabel = true;
                    int[] ts = new int[(int)max+2];
                    int newmax = 0;
                    for (int index = 0; index < displayData.Count; index++)
                    {
                        int i = (int)displayData[index];
                        ts[i] += 1;
                        if (ts[i] > newmax)
                        {
                            newmax = ts[i];
                        }
                    }
                    newmax += 1;
                    chart1.ChartAreas[0].AxisY.Maximum = newmax;
                    for (int i = 0; i < ts.Length; i++)
                    {
                        series1.Points.AddXY(i, ts[i]);
                    }
                    max = newmax;
                    min = 0;
                }
                else
                {
                    series1.ChartType = SeriesChartType.FastLine;
                    //將數值顯示在線上
                    series1.IsValueShownAsLabel = false;
                    //將數值新增至序列
                    chart1.ChartAreas[0].AxisY.Maximum = Math.Round(max, 3);
                    for (int index = 0; index < displayData.Count; index++)
                    {
                        series1.Points.AddXY(index, displayData[index]);
                    }
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
            max = 0.5f;
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
        int progress = 0;
        string resultFileName = "";
        int runSim()
        {
            int[] patSizes;
            List<int> pats = new List<int>();
            List<char> tweights = new List<char>();
            List<char> pweights = new List<char>();
            patSizes = new int[patterns.Length];
            for(int i = 0; i < 4; i++)
            {
                for(int j = 0; j < WEIGHT_COUNT; j++)
                {
                    tweights.Add(t[i][j]);
                    pweights.Add(p[i][j]);
                }
            }
            for(int i = 0; i < patterns.Length; i++)
            {
                patSizes[i] =  patterns[i].Count;
                for(int j = 0;j< patSizes[i]; j++)
                {
                    pats.Add(patterns[i][j]);
                }
            }
            int err = Simulation(ref progress, resultFileName, rounds, dealMode,WEIGHT_COUNT, d, tweights.ToArray(), pweights.ToArray(), patSizes, pats.ToArray());
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
                form2.UpdateData(players[curplayerIdx - 1].hands, lastIndex, curplayerIdx - 1, d[curplayerIdx - 1]);
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
                        fw.WriteLine(WEIGHT_COUNT);
                        for (int i = 0;i< WEIGHT_COUNT; i++)
                        {
                            string line = "";
                            for(int j = 0; j < patterns[i].Count; j++)
                            {
                                line += patterns[i][j].ToString();
                                line += ",";
                            }
                            fw.WriteLine(line);
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            fw.WriteLine((int)d[i]);
                            string line = "";
                            for (int j = 0; j < WEIGHT_COUNT; j++)
                            {
                                line += ((int)t[i][j]).ToString();
                                line += ",";
                            }
                            fw.WriteLine(line);
                            line = "";
                            for (int j = 0; j < WEIGHT_COUNT; j++)
                            {
                                line += ((int)p[i][j]).ToString();
                                line += ",";
                            }
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
                        int weightDim = int.Parse(fr.ReadLine());
                        for (int i = 0; i < WEIGHT_COUNT; i++)
                        {
                            string line = fr.ReadLine();
                            string[] playerConfig = line.Split(',');
                            patterns[i].Clear();
                            for (int j = 0; j < playerConfig.Length; j++)
                            {
                                if(playerConfig[j] != "")
                                {
                                    patterns[i].Add(int.Parse(playerConfig[j]));
                                }
                            }
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            d[i] = (char)int.Parse(fr.ReadLine());
                            string line = fr.ReadLine();
                            string[] playerConfig = line.Split(',');
                            for (int j = 0; j < WEIGHT_COUNT; j++)
                            { 
                                t[i][j] = (char)int.Parse(playerConfig[j]);
                            }
                            line = fr.ReadLine();
                            playerConfig = line.Split(',');
                            for (int j = 0; j < WEIGHT_COUNT; j++)
                            {
                                p[i][j] = (char)int.Parse(playerConfig[j]);
                            }
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
            curEditWeight = playerIdx-1;
            UpdateWeights();
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

        }

        PointF[] CreateCirclePoints(int n,float r, float offset_x, float offset_y)
        {
            PointF[] res = new PointF[n];
            double delta = 2 * Math.PI / n;
            double theta = Math.PI * .5;
            for (int i = 0; i < n; i++)
            {
                float x = ((float)Math.Cos(theta) * r);
                float y = -((float)Math.Sin(theta) * r);
                res[i] = new PointF(x+offset_x, y+offset_y);
                theta += delta;
            }
            return res;
        }
        float[] weights;
        PointF[] background;
        PointF[] background2;
        PointF[] weightPoints;
        int curWeightIndex = -1;
        private void groupBox2_Paint(object sender, PaintEventArgs e)
        {
            if (curEditWeight < 0)
                return;
            Graphics g = pictureBox1.CreateGraphics();
            g.Clear(Color.White);
            float w = pictureBox1.Width;
            float h = pictureBox1.Height;
            float r = 100;
            float ow = pictureBox1.Width * .5f;
            float oh = pictureBox1.Height * .5f;
            g.DrawPolygon(Pens.Black, background);
            g.DrawPolygon(Pens.Black, background2);
            foreach (var b in background)
            {
                g.DrawLine(Pens.Black, 1.1f*(b.X-ow)+ow, 1.1f*(b.Y-oh)+oh, ow, oh);
            }
            for (int i = 0; i < weights.Length; i++)
                weightPoints[i] = new PointF(weights[i] * (background[i].X - ow) / r + ow, weights[i] * (background[i].Y- oh) / r + oh);
            SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(128, 250, 0, 0));
            g.FillPolygon(semiTransBrush, weightPoints);
            g.DrawPolygon(Pens.Red, weightPoints);
            Font f = new Font("Arial",12, FontStyle.Bold);
            for (int i = 0; i < weights.Length; i++)
            {
                g.FillEllipse(Brushes.White, weightPoints[i].X-3, weightPoints[i].Y-3, 6, 6);
                g.DrawEllipse(Pens.Red, weightPoints[i].X-3, weightPoints[i].Y-3, 6, 6);
                if(curEditWeightMode == 0)
                {
                    g.DrawString(WEIGHT_TEXTS[i]+":"+Math.Round(t[curEditWeight][i]/100.0,2),f,Brushes.Black, 1.2f * (background[i].X - ow) + ow-22, 1.2f * (background[i].Y - oh) + oh-7);
                }
                else
                {
                    g.DrawString(WEIGHT_TEXTS[i] + ":" + Math.Round(p[curEditWeight][i] / 100.0, 2), f, Brushes.Black, 1.2f * (background[i].X - ow) + ow - 22, 1.2f * (background[i].Y - oh) + oh - 7);
                }
            }
            g.DrawString("P" + curEditWeight + (curEditWeightMode==0?" probability":" volatility"), f, Brushes.Black,2,4);
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (curEditWeight < 0)
                return;
            if (curWeightIndex >= 0)
            {
                float ow = pictureBox1.Width * .5f;
                float oh = pictureBox1.Height * .5f;
                PointF a = new PointF(background[curWeightIndex].X - ow, background[curWeightIndex].Y - oh);
                PointF b = new PointF(e.X - ow, e.Y - oh);
                float r = 100;
                double delta = WEIGHT_RADIUS / 20;
                double bLen = Math.Sqrt(b.X * b.X + b.Y * b.Y);
                if (bLen == 0)
                {
                    weights[curWeightIndex] = 0;
                }
                else
                {
                    double dot = (a.X * b.X + a.Y * b.Y) / WEIGHT_RADIUS;
                    weights[curWeightIndex] = (float)(Math.Floor(dot / delta) * delta);
                }
                if(weights[curWeightIndex] > WEIGHT_RADIUS * 1.1f)
                {
                    weights[curWeightIndex] = (float)(WEIGHT_RADIUS) *1.1f;
                }
                else if(weights[curWeightIndex] < 0)
                {
                    weights[curWeightIndex] = 0;
                }
                groupBox2.Invalidate();
                UpdateFromWeights();
            }
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            curWeightIndex = -1;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            curWeightIndex = -1;
        }
        float dot(PointF a, PointF b)
        {
            return a.X * b.X + a.Y * b.Y;
        }
        float DistanceLine(PointF p, PointF a, PointF b)
        {
            PointF ap = new PointF(p.X - a.X, p.Y - a.Y);
            PointF ab = new PointF(b.X - a.X, b.Y - a.Y);
            float t = dot(ap,ab) / dot(ab,ap);
            if (t < 0)
                t = 0;
            else if (t > 1)
                t = 1;
            PointF c = new PointF(a.X + t * ab.X-p.X, a.Y + t * ab.Y-p.Y);
            return (float)Math.Sqrt(c.X*c.X+c.Y*c.Y);
        }
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (curEditWeight < 0)
                return;
            float min = float.PositiveInfinity;
            int idx = -1;
            float ow = pictureBox1.Width * .5f;
            float oh = pictureBox1.Height * .5f;
            float r = 100;
            PointF center = new PointF(ow,oh);
            for (int i = 0; i < weights.Length; i++)
            {
                float d = DistanceLine(e.Location, center, background[i]);
                if (d < min)
                {
                    min = d;
                    idx = i;
                }
            }
            curWeightIndex = idx;
        }
        int curEditWeight = -1;
        int curEditWeightMode = 0;
        void UpdateFromWeights()
        {
            if (curEditWeight < 0)
                return;
            if (curEditWeightMode == 0)
            {
                for (int i = 0; i < WEIGHT_COUNT; i++)
                {
                    t[curEditWeight][i] = (char)(weights[i] * 100 / WEIGHT_RADIUS);
                }
            }
            else
            {
                for (int i = 0; i < WEIGHT_COUNT; i++)
                {
                    p[curEditWeight][i] = (char)(weights[i] * 100 / WEIGHT_RADIUS);
                }
            }
        }
        void UpdateWeights()
        {
            if (curEditWeight < 0)
                return;
            if(curEditWeightMode == 0)
            {
                for (int i = 0; i < WEIGHT_COUNT; i++)
                {
                    weights[i] = (float)(t[curEditWeight][i] * WEIGHT_RADIUS / 100.0);
                }
            }
            else
            {
                for (int i = 0; i < WEIGHT_COUNT; i++)
                {
                    weights[i] = (float)(p[curEditWeight][i] * WEIGHT_RADIUS / 100.0);
                }
            }
            groupBox2.Invalidate();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            curEditWeight = 0;
            curEditWeightMode = 0;
            UpdateWeights();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            curEditWeight = 1;
            curEditWeightMode = 0;
            UpdateWeights();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            curEditWeight = 2;
            curEditWeightMode = 0;
            UpdateWeights();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            curEditWeight = 3;
            curEditWeightMode = 0;
            UpdateWeights();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            curEditWeight = 0;
            curEditWeightMode = 1;
            UpdateWeights();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            curEditWeight = 1;
            curEditWeightMode = 1;
            UpdateWeights();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            curEditWeight = 2;
            curEditWeightMode = 1;
            UpdateWeights();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            curEditWeight = 3;
            curEditWeightMode = 1;
            UpdateWeights();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            EditPatternForm form = new EditPatternForm();
            form.Show();
            form.UpdateFormData(WEIGHT_TEXTS, patterns);
        }
    }
}

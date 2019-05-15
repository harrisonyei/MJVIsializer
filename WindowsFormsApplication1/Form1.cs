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
            public List<int[]> endHands;

            public Player()
            {
                huRounds = new List<float>();
                huTais   = new List<float>();
                hands    = new List<int[]>();
                endHands = new List<int[]>();
            }

            public void Init()
            {
                huRounds.Clear();
                huTais.Clear();
                hands.Clear();
                endHands.Clear();
            }

            public void Append(int r, int probs)
            {
                if(r < 0)
                {
                    r = 20;
                }
                huRounds.Add(r);
                huTais.Add(probs);
            }

            public void AppendHand(int h0,int h1,int h2,int h3)
            {
                int[] hand = new int[4] { h0, h1, h2 ,h3};
                hands.Add(hand);
            }

            public void AppendEndHand(int h0, int h1, int h2, int h3)
            {
                int[] hand = new int[4] { h0, h1, h2, h3 };
                endHands.Add(hand);
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
        [DllImport("./MyM16Headless.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Simulation(string configFile,string logFile,int simRounds);

        [DllImport("./MyM16Headless.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetProgressUpdate();

        [DllImport("./LogAnalyzer.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LogAnalysis(string logName, string subTitle);

        int rounds    = 100;
        int logRounds = 100;
        int batchSize = 1;
        char[] d  = new char[4];
        char[][] probs = new char[4][];
        char[][] volts = new char[4][];
        int playerIdx = 0;
        int curplayerIdx = 0;
        int dataType  = 0;
        int dataMode  = 0;
        List <float> displayData;
        List<int>[] patterns;
        Dictionary<string, LogData> cacheLogs;
        Player[] players;
        const int WEIGHT_COUNT = 8;
        double WEIGHT_RADIUS = 0;
        string[] WEIGHT_TEXTS = new string[8] { "Color", "a","b","c","d","e","f", "g"};
        public Form1()
        {
            for(int i = 0; i < 4; i++)
            {
                probs[i] = new char[WEIGHT_COUNT];
                volts[i] = new char[WEIGHT_COUNT];
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

            weights     = new float[2][];

            for(int i = 0; i < 2; i++)
            {
                weights[i] = new float[WEIGHT_COUNT];
                for (int j = 0; j < WEIGHT_COUNT; j++)
                {
                    weights[i][j] = 0;
                }
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

        void AnalysisLogData(string fileName)
        {
            string[] splits = fileName.Split('.');

            LogAnalysis(splits[0], splits[1]);

            string resultFile = splits[0] + "-result.csv";

            string logFile = splits[0] + ".MJlog";

            GenerateMJLogFromAnalysisResult(resultFile, logFile);

            LoadLogData(logFile);
        }

        void GenerateMJLogFromAnalysisResult(string fileName,string output)
        {
            using (StreamReader file = new StreamReader(new FileStream(fileName, FileMode.Open)))
            {
                using (BinaryWriter binWritter = new BinaryWriter(new FileStream(output, FileMode.OpenOrCreate)))
                {
                    //場數
                    string[] splits = file.ReadLine().Split(',');

                    int totalRounds = int.Parse(splits[1]);

                    binWritter.Write(totalRounds);

                    // 標題
                    file.ReadLine();
                    // cur round 0,huround 1, hutai 9, sh 14, eh 18
                    while (!file.EndOfStream)
                    {
                        splits = file.ReadLine().Split(',');

                        for (int i = 0; i < 4; i++)
                        {
                            int baseIdx = i * 21;

                            int huround = int.Parse(splits[baseIdx + 1]);
                            binWritter.Write(huround);

                            int hutai = int.Parse(splits[baseIdx + 9]);
                            binWritter.Write(hutai);

                            int sh0 = int.Parse(splits[baseIdx + 14]);
                            binWritter.Write(sh0);
                            int sh1 = int.Parse(splits[baseIdx + 15]);
                            binWritter.Write(sh1);
                            int sh2 = int.Parse(splits[baseIdx + 16]);
                            binWritter.Write(sh2);
                            int sh3 = int.Parse(splits[baseIdx + 17]);
                            binWritter.Write(sh3);

                            int eh0 = int.Parse(splits[baseIdx + 18]);
                            binWritter.Write(eh0);
                            int eh1 = int.Parse(splits[baseIdx + 19]);
                            binWritter.Write(eh1);
                            int eh2 = int.Parse(splits[baseIdx+20]);
                            binWritter.Write(eh2);
                            int eh3 = int.Parse(splits[baseIdx + 21]);
                            binWritter.Write(eh3);
                        }
                    }
                }
            }
        }

        void LoadLogData(string fileName)
        {
            cacheLogs.Clear();

            using (BinaryReader binReader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
            {
                // Set Position to the beginning of the stream.
                binReader.BaseStream.Position = 0;

                logRounds = binReader.ReadInt32();

                for (int i = 0; i < logRounds; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        int huround = binReader.ReadInt32();
                        int hutai = binReader.ReadInt32();
                        players[j].Append(huround, hutai);

                        // 起始手排
                        int hand0 = binReader.ReadInt32();
                        int hand1 = binReader.ReadInt32();
                        int hand2 = binReader.ReadInt32();
                        int hand3 = binReader.ReadInt32();
                        players[j].AppendHand(hand0, hand1, hand2, hand3);

                        // 結束時手牌
                        hand0 = binReader.ReadInt32();
                        hand1 = binReader.ReadInt32();
                        hand2 = binReader.ReadInt32();
                        hand3 = binReader.ReadInt32();
                        players[j].AppendEndHand(hand0, hand1, hand2, hand3);
                    }

                }

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

            ChartArea chartArea = chart1.ChartAreas[0];

            chartArea.AxisX.Title = "Round";

            //chartArea.AxisY.Title = "yyy";

            chartArea.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chartArea.AxisX.Minimum = 0;

            if (!cacheLogs.TryGetValue(str, out item))
            {
                float max;
                float min;
                //標題 最大數值
                Series series1 = new Series("");

                series1.YValueType = ChartValueType.Single;
                series1.XValueType = ChartValueType.Single;
                //設定線條顏色
                series1.Color = Color.Blue;
                //設定字型
                series1.Font = new Font("新細明體", 14);
                updateDisplayData(out max,out min);

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

                    chartArea.AxisY.Maximum = newmax;

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
                    chartArea.AxisY.Maximum = Math.Round(max, 3);
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
                chartArea.AxisY.Maximum = Math.Round(log.max, 3);
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
                    for(int volts = 0; volts < 4; volts++)
                    {
                        float r = players[volts].GetDataType(dataType, i);
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
        string resultLogFileName = "";
        string resultConfigFileName = "";

        int runSim()
        {
            ExportConfiguration(resultConfigFileName);
            int err = Simulation(resultConfigFileName, resultLogFileName, rounds);

            if (err == -1)
            {
                resultLogFileName = "Error !";
            }

            return err;
        }

        int Progress()
        {
            while (progress <= 99)
            {
                Task.Delay(20);
                progressBar1.Invoke(new Action(() =>
                {
                    progress = GetProgressUpdate();
                    if(progress < 0)
                    {
                        progress = 0;
                    }
                    progressBar1.Value = progress;
                }));
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

            string timeStamp = "MJ-" + DateTime.Now.ToString("MMddHHmmss");

            resultLogFileName = timeStamp + ".csv";
            resultConfigFileName = timeStamp + ".MJconfig";

            SimStart();
            cacheLogs.Clear();
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

            AnalysisLogData(resultLogFileName);

            await Task.Delay(2000);
            progressBar1.Value = 0;
            ChangeEnabled(true);
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
                form2.UpdateData(players[curplayerIdx - 1].hands, lastIndex, curplayerIdx - 1, d[curplayerIdx - 1],WEIGHT_COUNT,probs,volts);
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
            ReloadChart();
        }

        private void configurationsToolStripMenuItem_Click(object sender, EventArgs e)
        {

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
            ReloadChart();
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
            ReloadChart();
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

        float[][] weights;
        PointF[] background;
        PointF[] background2;
        PointF[] weightPoints;
        int curWeightIndex = -1;
        private void paintGraph(int mode)
        {
            Graphics g = null;

            if (mode == 0)
            {
                g = pictureBox1.CreateGraphics();
            }
            else
            {
                g = pictureBox2.CreateGraphics();
            }

            g.Clear(Color.White);

            float r = 100;

            float w = pictureBox1.Width;
            float h = pictureBox1.Height;

            float ow = pictureBox1.Width * .5f;
            float oh = pictureBox1.Height * .5f;

            g.DrawPolygon(Pens.Black, background);
            g.DrawPolygon(Pens.Black, background2);
            foreach (var b in background)
            {
                g.DrawLine(Pens.Black, 1.1f * (b.X - ow) + ow, 1.1f * (b.Y - oh) + oh, ow, oh);
            }

            var _weights = weights[mode];

            for (int i = 0; i < _weights.Length; i++)
                weightPoints[i] = new PointF(_weights[i] * (background[i].X - ow) / r + ow, _weights[i] * (background[i].Y - oh) / r + oh);

            SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(128, 250, 0, 0));

            g.FillPolygon(semiTransBrush, weightPoints);

            g.DrawPolygon(Pens.Red, weightPoints);

            Font f = new Font("Arial", 12, FontStyle.Bold);

            for (int i = 0; i < _weights.Length; i++)
            {
                g.FillEllipse(Brushes.White, weightPoints[i].X - 3, weightPoints[i].Y - 3, 6, 6);
                g.DrawEllipse(Pens.Red, weightPoints[i].X - 3, weightPoints[i].Y - 3, 6, 6);

                if (mode == 0)
                {
                    g.DrawString(WEIGHT_TEXTS[i] + ":" + Math.Round(probs[curEditWeight][i] / 100.0, 2), f,
                        Brushes.Black, 1.2f * (background[i].X - ow) + ow - 22, 1.2f * (background[i].Y - oh) + oh - 7);
                }
                else
                {
                    g.DrawString(WEIGHT_TEXTS[i] + ":" + Math.Round((1.0f - volts[curEditWeight][i] / 100.0), 2), f,
                        Brushes.Black, 1.2f * (background[i].X - ow) + ow - 22, 1.2f * (background[i].Y - oh) + oh - 7);
                }
            }

            g.DrawString("P" + curEditWeight + (mode == 0 ? " probability" : " volatility"), f, Brushes.Black, 2, 4);
        }

        private void groupBox2_Paint(object sender, PaintEventArgs e)
        {
            if (curEditWeight < 0)
                return;

            if(curEditWeightMode == 0)
            {
                paintGraph(0);
            }
            else if(curEditWeightMode == 1)
            {
                paintGraph(1);
            }
            else
            {
                paintGraph(0);
                paintGraph(1);
            }

        }

        private void update_weightGraph(float x,float y)
        {
            var _weights = weights[curEditWeightMode];

            float ow = pictureBox1.Width * .5f;
            float oh = pictureBox1.Height * .5f;

            PointF a = new PointF(background[curWeightIndex].X - ow, background[curWeightIndex].Y - oh);
            PointF b = new PointF(x - ow, y - oh);

            double delta = WEIGHT_RADIUS / 20;
            double bLen = Math.Sqrt(b.X * b.X + b.Y * b.Y);

            if (bLen == 0)
            {
                _weights[curWeightIndex] = 0;
            }
            else
            {
                double dot = (a.X * b.X + a.Y * b.Y) / WEIGHT_RADIUS;
                _weights[curWeightIndex] = (float)(Math.Floor(dot / delta) * delta);
            }
            if (_weights[curWeightIndex] > WEIGHT_RADIUS)
            {
                _weights[curWeightIndex] = (float)(WEIGHT_RADIUS);
            }
            else if (_weights[curWeightIndex] < 0)
            {
                _weights[curWeightIndex] = 0;
            }

            groupBox2.Invalidate();
            UpdateFromWeights();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (curEditWeight < 0)
                return;

            if (curWeightIndex >= 0)
            {
                curEditWeightMode = 0;
                update_weightGraph(e.X,e.Y);
            }
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (curEditWeight < 0)
                return;

            if (curWeightIndex >= 0)
            {
                curEditWeightMode = 1;
                update_weightGraph(e.X, e.Y);
            }
        }

        private void pictureBox_MouseLeave(object sender, EventArgs e)
        {
            curWeightIndex = -1;
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            curWeightIndex = -1;
        }

        float dot(PointF a, PointF b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        float DistanceLine(PointF volts, PointF a, PointF b)
        {
            PointF ap = new PointF(volts.X - a.X, volts.Y - a.Y);
            PointF ab = new PointF(b.X - a.X, b.Y - a.Y);
            float probs = dot(ap,ab) / dot(ab,ap);
            if (probs < 0)
                probs = 0;
            else if (probs > 1)
                probs = 1;
            PointF c = new PointF(a.X + probs * ab.X-volts.X, a.Y + probs * ab.Y-volts.Y);
            return (float)Math.Sqrt(c.X*c.X+c.Y*c.Y);
        }

        private void weightGraph_MouseDown(MouseEventArgs e)
        {
            float min = float.PositiveInfinity;
            int idx = -1;
            float ow = pictureBox1.Width * .5f;
            float oh = pictureBox1.Height * .5f;
            float r = 100;

            PointF center = new PointF(ow, oh);

            for (int i = 0; i < weights[curEditWeightMode].Length; i++)
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

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (curEditWeight < 0)
                return;

            curEditWeightMode = 0;

            weightGraph_MouseDown(e);
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (curEditWeight < 0)
                return;

            curEditWeightMode = 1;

            weightGraph_MouseDown(e);
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
                    probs[curEditWeight][i] = (char)(weights[0][i] * 100 / WEIGHT_RADIUS);
                }
            }
            else
            {
                for (int i = 0; i < WEIGHT_COUNT; i++)
                {
                    volts[curEditWeight][i] = (char)(100 - (weights[1][i] * 100 / WEIGHT_RADIUS));
                }
            }
        }

        void UpdateWeights()
        {
            if (curEditWeight < 0)
                return;

            for (int i = 0; i < WEIGHT_COUNT; i++)
            {
                weights[0][i] = (float)(probs[curEditWeight][i] * WEIGHT_RADIUS / 100.0);
                weights[1][i] = (float)(100.0 - (volts[curEditWeight][i] * WEIGHT_RADIUS / 100.0));
            }

            curEditWeightMode = 2;
            groupBox2.Invalidate();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            curEditWeight = 0;
            UpdateWeights();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            curEditWeight = 1;
            UpdateWeights();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            curEditWeight = 2;
            UpdateWeights();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            curEditWeight = 3;
            UpdateWeights();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            curEditWeight = 0;
            UpdateWeights();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            curEditWeight = 1;
            UpdateWeights();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            curEditWeight = 2;
            UpdateWeights();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            curEditWeight = 3;
            UpdateWeights();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            EditPatternForm form = new EditPatternForm();
            form.Show();
            form.UpdateFormData(WEIGHT_TEXTS, patterns);
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofDialog = new OpenFileDialog();
            ofDialog.Filter = "MJ Config|*.MJconfig";
            ofDialog.Title = "Load Configuration file";
            ofDialog.ShowDialog();

            if (ofDialog.FileName != "")
            {
                using (BinaryReader binReader = new BinaryReader(new FileStream(ofDialog.FileName, FileMode.Open)))
                {

                    int weightCount =  binReader.ReadInt32();

                    patterns = new List<int>[weightCount];

                    for (int i = 0; i < weightCount; i++)
                    {

                        int patSize = binReader.ReadInt32();

                        patterns[i] = new List<int>();

                        for (int j = 0; j < patSize; j++)
                        {
                            patterns[i].Add(binReader.ReadInt32());
                        }

                    }

                    // distances probabilities volatilities
                    d     = new char[4];
                    probs = new char[4][];
                    volts = new char[4][];

                    for (int i = 0; i < 4; i++)
                    {
                        d[i] = (char)binReader.ReadByte();

                        probs[i] = new char[weightCount];
                        volts[i] = new char[weightCount];

                        for (int j = 0; j < weightCount; j++)
                        {
                            probs[i][j] = (char)binReader.ReadByte();
                        }
                        for (int j = 0; j < weightCount; j++)
                        {
                            volts[i][j] = (char)binReader.ReadByte();
                        }
                    }

                }


                updateFormView();
                UpdateWeights();
            }
        }

        void ExportConfiguration(string fileName)
        {
            using (BinaryWriter binWritter = new BinaryWriter(new FileStream(fileName, FileMode.OpenOrCreate)))
            {

                int weightCount = WEIGHT_COUNT;

                binWritter.Write(weightCount);

                for (int i = 0; i < weightCount; i++)
                {

                    binWritter.Write(patterns[i].Count);

                    for (int j = 0; j < patterns[i].Count; j++)
                    {
                        binWritter.Write(patterns[i][j]);
                    }

                }

                // distances probabilities volatilities
                for (int i = 0; i < 4; i++)
                {
                    binWritter.Write((byte)d[i]);

                    for (int j = 0; j < weightCount; j++)
                    {
                        binWritter.Write((byte)probs[i][j]);
                    }
                    for (int j = 0; j < weightCount; j++)
                    {
                        binWritter.Write((byte)volts[i][j]);
                    }
                }
            }
        }

        private void exrpotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfDialog = new SaveFileDialog();
            sfDialog.Filter = "MJ Config|*.MJconfig";
            sfDialog.Title = "Save Configuration file";
            sfDialog.ShowDialog();

            if (sfDialog.FileName != "")
            {
                ExportConfiguration(sfDialog.FileName);
            }

        }


    }
}

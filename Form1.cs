using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;

namespace 串口管理程序
{
    public partial class Form1 : Form
    {
        //串口收发功能变量声明
        private SerialPort serialPorts;                                                 //串口通信类
        private string[] portNames = null;                                              //字符串数组，存储可用串口名
        private byte[] portBuffer;                                                      //收发数据的二进制缓存区
        private int sendNumber = 0;                                                     //数据发送总量
        private int revNumber = 0;                                                      //数据接收总量

        //模拟量动态图显示变量声明
        private Queue<double> dataQueue = new Queue<double>(1000);

        //判断接收的是开关量还是模拟量
        private int isAnalog = 1;
        private int isSwitchingValue = 0;
        //
        private int nNum = 1;

        //界面构建
        public Form1()
        {
            InitializeComponent();

            //动态图初始化
            InitChart();

            //这个类中我们不检查跨线程的调用是否合法
            //因为.net 2.0以后加强了安全机制,，不允许在winform中直接跨线程访问控件的属性
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        //窗体初始化
        private void Form1_Load(object sender, EventArgs e)
        {
            //变量定义
            serialPorts = new SerialPort();
            this.portBuffer = new byte[10000];
            //SerialPort.GetPortNames返回当前计算机的串口端口名数组
            this.portNames = SerialPort.GetPortNames();

            //将可用串口显示在串口列表中
            if (this.portNames.Length > 0)
            {
                this.comboBox1.Items.AddRange(this.portNames);
            }
            else
            {
                MessageBox.Show("未检测到当前计算机的串口！", "提示");
            }

        }

        //按下打开串口按钮
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.comboBox1.Text == "")
            {

                MessageBox.Show("请先选择串口号！","提示");
                
                return;
            }
            try
            {
                this.serialPorts.PortName = this.comboBox1.Text.ToString();
                if (!this.serialPorts.IsOpen)
                {
                    this.serialPorts.BaudRate = int.Parse(comboBox2.Text);              //波特率
                    this.serialPorts.DataBits = int.Parse(comboBox3.Text);              //数据位
                    this.serialPorts.StopBits = (StopBits)int.Parse(comboBox4.Text);    //停止位
                    //this.serialPorts.Parity = (Parity)string.Parse(comboBox5.Text);   //校验位
                    //this.serialPorts.Parity = (Parity)int.Parse(comboBox5.Text);      //校验位
                    //this.serialPorts.Handshake
                    this.serialPorts.Open();
                    this.label2.Text = "串口状态：已打开";
                    this.button1.Enabled = false;
                    this.button2.Enabled = true;
                    this.button3.Enabled = true;
                    this.button5.Enabled = true;
                    this.button6.Enabled = true;
                    this.button8.Enabled = true;
                    this.comboBox1.Enabled = false;
                    this.comboBox2.Enabled = false;
                    this.comboBox3.Enabled = false;
                    this.comboBox4.Enabled = false;
                    this.comboBox5.Enabled = false;
                    this.comboBox6.Enabled = false;

                    //timer计时器开始
                    this.timer1.Start();
                    
                    //串口接收处理函数
                    serialPorts.DataReceived += new SerialDataReceivedEventHandler(serialport_DataReceived);

                }

            }
            catch (IOException eio)
            {
                MessageBox.Show("打开串口异常：" + eio);
            }
        }

        //按下关闭串口按钮
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.serialPorts.IsOpen)
                {
                    this.serialPorts.Close();
                    this.label2.Text = "串口状态：未打开";
                    this.button1.Enabled = true;
                    this.button2.Enabled = false;
                    this.button3.Enabled = false;
                    this.button5.Enabled = false;
                    this.button6.Enabled = false;
                    this.button8.Enabled = false;
                    this.comboBox1.Enabled = true;
                    this.comboBox2.Enabled = true;
                    this.comboBox3.Enabled = true;
                    this.comboBox4.Enabled = true;
                    this.comboBox5.Enabled = true;
                    this.comboBox6.Enabled = true;

                    //timer计时器结束
                    this.timer1.Stop();

                }
            }
            catch (IOException eio)
            {
                MessageBox.Show("打开串口异常：" + eio);
            }
        }

        //按下发送按钮
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.serialPorts.IsOpen)
                {

                    string sendContent = this.textBox2.Text.ToString();

                    //字符串形式发送
                    if (this.radioButton1.Checked)
                    {
                        this.serialPorts.Write(sendContent);
                        this.sendNumber += sendContent.Length;                          //记录发送量
                    }

                    //十六进制形式发送(待测试)
                    //将输入的字符串按照空格逗号分组
                    else
                    {
                        string sendNoNull = sendContent.Trim();
                        string sendNoComma = sendNoNull.Replace(',', ' ');
                        string sendNoComma1 = sendNoComma.Replace("0x", "");
                        string sendNoComma2 = sendNoComma1.Replace("0X", "");

                    }
                    /*else
                    {
                        Byte[] sendContent = new Byte[1];
                        sendContent[0] = Byte.Parse(this.textBox2.Text);
                        this.serialPorts.Write(sendContent,0,1);
                    }
                    */
                    this.label8.Text = "数据发送总量：" + this.sendNumber;
                }
                else
                {
                    MessageBox.Show("请先打开串口","提示");
                }

            }
            catch (IOException eio)
            {
                MessageBox.Show("串口发送异常：" + eio);
            }
        }
        /* SerialPort::Write()函数MSDN：
         * Write(Byte[],Int32,Inte32)：使用缓冲区中的数据将指定数量的字节写入串行端口
         * Write(char[],Int32,Inte32)：使用缓冲区中的数据将指定数量的字节写入串行端口
         * Write(String)：将指定的字符串写入串行端口
         */

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (serialPorts.CtsHolding)
            {
                label10.Text = "CTS：1";
            }
            else
            {
                label10.Text = "CTS：0";
            }

            if (serialPorts.DsrHolding)
            {
                label11.Text = "DSR：1";
            }
            else
            {
                label11.Text = "DSR：0";
            }

        }

        //串口接收处理函数
        private void serialport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //二进制接收数据
            if (radioButton4.Checked)
            {
                int receivedContent = serialPorts.ReadByte();
                this.revNumber += 1;
                this.textBox1.Text += receivedContent.ToString("X2");
                this.textBox1.Text += " ";

                //如果开启动态图显示功能且为奇数个收到的数据
                if (this.button6.Text == "结束绘图" && this.revNumber % 2 == 1)
                {
                    //大于100个数先出列达到动态图效果
                    if (this.dataQueue.Count > 30)
                    {
                        this.dataQueue.Dequeue();
                    }
                    //添加采集数
                    this.dataQueue.Enqueue(receivedContent);

                    this.chart1.Series[0].Points.Clear();
                    for (int i = 0; i < this.dataQueue.Count; i++)
                    {
                        this.chart1.Series[0].Points.AddXY((i + 1), this.dataQueue.ElementAt(i));
                    }

                    //isAnalog置零
                    //this.isAnalog = 0;
                }

                //如果开启动态图显示功能且为偶数个收到的数据
                if (this.button6.Text == "结束绘图" && this.revNumber % 2 == 0)
                {
                    //
                    showLight(receivedContent);
                    //isSwitchingValue置零
                    //this.isSwitchingValue = 0;
                    //this.isAnalog = 1;
                }

                //如果开启了采集功能
                if(this.button5.Text == "结束采集")
                {
                    this.textBox3.Text = receivedContent.ToString();
                    int returnNum = receivedContent / 2;
                    this.textBox4.Text = returnNum.ToString();
                    this.serialPorts.Write(Convert.ToString((char)returnNum));

                }

            }

            //字符串接收数据
            else
            {
                string receivedContent = serialPorts.ReadExisting();                    //字符串接收
                this.revNumber += 1;
                this.textBox1.Text += receivedContent;
                this.textBox1.Text += " ";
            }

            this.label16.Text = "数据接收总量：" + this.revNumber;

            
        }

        //按下清屏按钮
        private void button4_Click(object sender, EventArgs e)
        {
            this.textBox1.Text = "";
        }

        //按下采集按钮
        private void button5_Click(object sender, EventArgs e)
        {
            if(this.button5.Text == "开始采集")
            {
                if (!radioButton4.Checked)
                {
                    MessageBox.Show("本功能只在二进制接收数据下开启", "提示");
                }
                else
                {
                    this.button5.Text = "结束采集";
                    this.serialPorts.Write("@2.");
                }
            }
            else
            {
                this.button5.Text = "开始采集";
            }
        }


        /// <summary>
        /// 初始化动态图
        /// </summary>
        private void InitChart()
        {
            //定义图表区域
            this.chart1.ChartAreas.Clear();
            ChartArea chartArea1 = new ChartArea("C1");
            this.chart1.ChartAreas.Add(chartArea1);
            //定义存储和显示点的容器
            this.chart1.Series.Clear();
            Series series1 = new Series(" ");
            series1.ChartArea = "C1";
            this.chart1.Series.Add(series1);
            //设置图表显示样式
            this.chart1.ChartAreas[0].AxisY.Minimum = 0;
            this.chart1.ChartAreas[0].AxisY.Maximum = 255;
            this.chart1.ChartAreas[0].AxisX.Interval = 5;
            this.chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.Silver;
            this.chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.Silver;
            //设置标题
            this.chart1.Titles.Clear();
            this.chart1.Titles.Add("S01");
            this.chart1.Titles[0].Text = "模拟量显示";
            this.chart1.Titles[0].ForeColor = Color.Blue;
            this.chart1.Titles[0].Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            //设置图表显示样式
            this.chart1.Series[0].Color = Color.RoyalBlue;

            this.chart1.Titles[0].Text = "动态图显示";
            this.chart1.Series[0].ChartType = SeriesChartType.Line;

            /*
            if (rb1.Checked)
            {
                this.chart1.Titles[0].Text = string.Format("模拟量 {0} 显示", rb1.Text);
                this.chart1.Series[0].ChartType = SeriesChartType.Line;
            }
            //if (rb2.Checked) {
            //    this.chart1.Titles[0].Text = string.Format("XXX {0} 显示", rb2.Text);
            //    this.chart1.Series[0].ChartType = SeriesChartType.Spline;
            //}
            */
            this.chart1.Series[0].Points.Clear();
        }

        //点击开始绘图按钮
        private void button6_Click(object sender, EventArgs e)
        {
            if (this.button6.Text == "开始绘图")
            {
                if (!radioButton4.Checked)
                {
                    MessageBox.Show("本功能只在二进制接收数据下开启", "提示");
                }
                else
                {
                    this.button6.Text = "结束绘图";
                    this.serialPorts.Write("@1.");
                }
            }
            else
            {
                this.button6.Text = "开始绘图";
            }

            
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        //按下停止按钮
        private void button8_Click(object sender, EventArgs e)
        {
            //发送停止命令，命令可用自定义
            this.serialPorts.Write("s");
        }

        private void showLight(int lightState)
        {
            lightState = lightState / 16;

            if (lightState % 2 == 0)
            {
                checkBox4.Checked = false;
            }
            else
            {
                checkBox4.Checked = true;
            }

            lightState = lightState / 2;
            if (lightState % 2 == 0)
            {
                checkBox3.Checked = false;
            }
            else
            {
                checkBox3.Checked = true;
            }

            lightState = lightState / 2;
            if (lightState % 2 == 0)
            {
                checkBox2.Checked = false;
            }
            else
            {
                checkBox2.Checked = true;
            }

            lightState = lightState / 2;
            if (lightState % 2 == 0)
            {
                checkBox1.Checked = false;
            }
            else
            {
                checkBox1.Checked = true;
            }
        }



    }
}

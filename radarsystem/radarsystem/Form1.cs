using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Data.OleDb;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Drawing;

namespace radarsystem
{
    
    public partial class Form1 : Form
    {     
       
        List<Point>[] list_trace_update = new List<Point>[20];  //剧情设置有变化，运动类型包括匀加速，匀速，匀减速，
                                                                //圆周运动等典型运动，目标最多20个，_update指的是10月20号以后的版本
        List<PointD>[] list_detect_distance_update = new List<PointD>[20];//波形图上画真实轨迹数据来源
                                 //最终用这个数组,用这个数组添加噪声，画波形图上噪音轨迹，以及特征分析            
        ArrayList arr_tar=new ArrayList() ;  //目标ID数组
        Color[] color = new Color[50];//轨迹颜色数组
        //表示选中那个场景
        Scene scene;         
        //存储添加噪音后的轨迹点
        List<PointD>[] guassianList = new List<PointD>[50];
        List<PointD>[] poissonList = new List<PointD>[50];
        List<PointD>[] uniformList = new List<PointD>[50];

        ////存储添加噪音并且判断了距离的轨迹点
        List<PointD>[] guassianList_final = new List<PointD>[50];
        List<PointD>[] poissonList_final = new List<PointD>[50];
        List<PointD>[] uniformList_final = new List<PointD>[50];

        ///存储进行傅立叶和反傅立叶变换后的数据
        List<Point> fftList = new List<Point>();
        List<Point> ifftList = new List<Point>();

        //定义两个数组存储指挥控制中两个雷达检测的轨迹点
        List<PointD>[] command_listone = new List<PointD>[50];
        List<PointD>[] command_listtwo = new List<PointD>[50];
                                  //直接操作这两个command_listone，two，最后令其保存的是各自添加了噪声后的轨迹点
        List<PointD>[] command_listmix = new List<PointD>[50];      //存储融合后的数据点
        
        //数据库操作
        DBInterface dbInterface = new DBInterface();
        //标识是否添加了噪音
        NoiseEnum noiseFlag = NoiseEnum.NoNoise;
        Point screenpoint_pic4;
        private bool isDragging = false; //拖中
        private int currentX = 0, currentY = 0; //原来鼠标X,Y坐标
        //bool flag_thread2 = false;
        //bool flag_thread1 = false;
        bool flag_editchange = false;  //对应的配置文件文本框内容发生改变
        bool flag_init_editchange = false; //第一次加载时候，文本框内容会发生改变
        bool flag_command=  false;

        bool isClearCombox = false;     //
     
         //用pictureBox4 的左上角坐标表示雷达的中心点坐标
   
        //在指挥控制中确定已经选择了雷达的个数
        int hasChoosedRadar = 0;    
        public Form1()
        {
            InitializeComponent();
            comboBox_ToolList.SelectedIndex = 2;
            textBox_doppler.Visible = false;
            button_goback.Visible = false;
            pictureBox4.Visible = false;
            button_update_config.Visible = false;
            label_sel_radartype.Visible = false;
            buttonDectecModeling.Visible = false;
            buttonModelDone.Visible = false;
            groupBox2.Visible = false;
            groupBox3.Visible = false;
            button_text_update.Visible = false;

            textBox_juli.Visible = false;
            textBox_zaipin.Visible = false;
            textBox_chongpin.Visible = false;
            textBox_maikuan.Visible = false;
            textBox_maifu.Visible = false;
            textBox_saomiao.Visible = false;
            textBox_jiebianliang.Visible = false;
            textBox_doudongliang.Visible = false;

            ///指挥控制中用到如下的控件
            this.label5.Visible = false;

            this.dopplercheckBox.Visible = false;
            this.multpBasecheckBox.Visible = false;
            this.bvrcheckBox.Visible = false;

            this.groupBox4.Visible = false;
            this.groupBox5.Visible = false;
            this.groupBox6.Visible = false;

            this.pictureBox3.Visible = false;
            
            this.pictureBox5.Visible = false;
            this.mixtrailButton.Visible = false;
            ///end
                       
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //连接数据库
            long id, tar_ID;
            int index;
            string conStr = string.Format(@"Provider=Microsoft.Jet.OLEDB.4.0;
                            Data source=" + Application.StartupPath + "\\database\\whut\\RecognitionAid.mdb");

            DataSet ds = dbInterface.query(conStr, "select * from TargetTrailPoints", "目标轨迹");

           
            for (int i = 0; i < 20;i++ )
            {
                list_trace_update[i] = new List<Point>();
                list_detect_distance_update[i] = new List<PointD>();

                //list_detect_distance_final_update[i] = new List<PointD>();
                color[i] = System.Drawing.Color.FromArgb((227 * i) % 255, (45 * i) % 255, (153 * i) % 255);

                guassianList[i] = new List<PointD>();
                guassianList_final[i] = new List<PointD>();
                poissonList[i] = new List<PointD>();
                poissonList_final[i] = new List<PointD>();
                uniformList[i] = new List<PointD>();
                uniformList_final[i] = new List<PointD>();
            }

            for (int i = 0; i < 20; i++)
            {
                command_listone[i] = new List<PointD>();
                command_listtwo[i] = new List<PointD>();
                command_listmix[i] = new List<PointD>();
            }             
            screenpoint_pic4 = PointToScreen(pictureBox4.Location);        
         }   
        private void draw_monitor_trace()
        {           
            for (int i = 0; i < arr_tar.Count; i++)
            {
                //QueueUserWorkItem()方法：将工作任务排入线程池。
                ThreadPool.QueueUserWorkItem(new WaitCallback(thread2), i);
                // Thread2 表示要执行的方法(与WaitCallback委托的声明必须一致) 
            }          
        }
        //在波形图中绘制指挥控制融合后的轨迹        
        public void thread2(object obj)
        {
            
            //List<PointD> points = (List<PointD>)o;
            int flag = (int)obj;
            List<Point> list_trace = new List<Point>();
            double distance1, distance2;
            distance1 = 7 * panel1.Width / 20;
            Graphics g;
            SolidBrush myBrush = new SolidBrush(color[flag]);//画刷
            Pen p = new Pen(color[flag], 2);
            g = panel1.CreateGraphics();
            Point point;
            Point point_diff;
            Point cir_Point = new Point(0, 0);
            Point one = new Point(0, 0);
            Point two = new Point(0, 0);
            cir_Point.X = panel1.Width / 10 * 5;
            cir_Point.Y = panel1.Height / 10 * 5;           
            if (radioButton7.Checked == true)       //高斯噪声
            {
                for (int i = 0; i < guassianList_final[flag].Count ; i++)
                {
                    //double类型的坐标转换成int
                    list_trace.Add(new Point((int)guassianList_final[flag][i].X, (int)guassianList_final[flag][i].Y));
                    if (checkBox_udpSocket.Checked == true)
                    {
                        sendData(guassianList_final[flag][i].X, guassianList_final[flag][i].Y);
                    }
                    //g.DrawString();
                }
            }
            else if (radioButton8.Checked == true) //泊松噪声
            {
                for (int i = 0; i < poissonList_final[flag].Count ; i++)
                {
                    //double类型的坐标转换成int
                    list_trace.Add(new Point((int)poissonList_final[flag][i].X, (int)poissonList_final[flag][i].Y));
                    //g.DrawString();
                }
            }

            else if (radioButton9.Checked == true) //平均噪声
            {
                for (int i = 0; i <uniformList_final[flag].Count ; i++)
                {
                    //double类型的坐标转换成int
                    list_trace.Add(new Point((int)uniformList_final[flag][i].X, (int)uniformList_final[flag][i].Y));
                    //g.DrawString();
                }
            }
       
            if (list_trace.Count == 1)
            {
                one.X = list_trace[0].X - pictureBox4.Left + cir_Point.X;
                one.Y = list_trace[0].Y - pictureBox4.Left + cir_Point.Y;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                  one.Y - 3, 6, 6));//画实心椭圆
            }
            else if(list_trace.Count>1)
            {
                for (int i = 0; i < list_trace.Count - 1; i++)
                {

                    point = list_trace[i];
                    point_diff = point;
                    point_diff.X = point.X - pictureBox4.Left;
                    point_diff.Y = point.Y - pictureBox4.Top;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 > 0)
                        continue;
                    //        g.FillEllipse(myBrush, new Rectangle(cir_Point.X + point_diff.X - 3, cir_Point.Y + point_diff.Y - 3, 3, 3));//画实心椭圆
                    //    g.DrawLine(new Pen(Color.Red), point_diff.X, point_diff.Y, point_diff.X, point_diff.Y);
                    //    g.DrawLine(new Pen(Color.Red), 200, 200,210, 210);
                    one.X = cir_Point.X + point_diff.X;
                    one.Y = cir_Point.Y + point_diff.Y;
                    two.X = list_trace[i + 1].X - pictureBox4.Left + cir_Point.X;
                    two.Y = list_trace[i + 1].Y - pictureBox4.Top + cir_Point.Y;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;                    
                    g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                       one.Y - 3, 6, 6));//画实心椭圆
                    g.FillEllipse(myBrush, new Rectangle(two.X - 3,
                        two.Y - 3, 6, 6));//画实心椭圆
                    g.DrawLine(p, one, two);
                    System.Threading.Thread.Sleep(200);
                }
            }               
         }
        private void sendData(Double X, Double Y)
        {
            if (checkBox_udpSocket.Checked == true)  //udp报文复选框选中，且此时雷达扫描到该目标
            //返回数据给发送端，//即数据发送模块
            {
                radarsystem.udpSocket.StructDemo structSend = new radarsystem.udpSocket.StructDemo();
                structSend.srcTgtTrk.dLat = (float)X;
                structSend.srcTgtTrk.dLon = (float)Y;
                int port = 11000;
                IPAddress HostIP = IPAddress.Parse("127.0.0.1");
                IPEndPoint host;
                host = new IPEndPoint(HostIP, port);
                UdpClient udpClient = new UdpClient(host);
                byte[] bytes = radarsystem.udpSocket.StructToBytes(structSend, 264);
                udpClient.Send(bytes, bytes.Length, host);
                udpClient.Close();
            }
        }
        public void draw_mix_trail()
        {
            for (int i = 0; i < arr_tar.Count; i++)
                ThreadPool.QueueUserWorkItem(new WaitCallback(mixTrailThread), i);
        }
        public void mixTrailThread(object obj)
        {
            int flag = (int)obj;
            List<Point> list_trace = new List<Point>();
            double distance1, distance2;
            distance1 = 7 * panel1.Width / 20;
            Graphics g;
            SolidBrush myBrush = new SolidBrush(color[flag]);//画刷
            Pen p = new Pen(color[flag], 2);
            g = panel1.CreateGraphics();
            Point point;
            Point point_diff;
            Point cir_Point = new Point(0, 0);
            Point one = new Point(0, 0);
            Point two = new Point(0, 0);
            cir_Point.X = panel1.Width / 10 * 5;
            cir_Point.Y = panel1.Height / 10 * 5;

            for (int i = 0; i < command_listmix[flag].Count; i++)
            {
                //double类型的坐标转换成int
                //    MessageBox.Show(i.ToString());
                list_trace.Add(new Point((int)command_listmix[flag][i].X, (int)command_listmix[flag][i].Y));
                //g.DrawString();
            }
            if (list_trace.Count == 1)
            {
                one.X = list_trace[0].X - pictureBox4.Left + cir_Point.X;
                one.Y = list_trace[0].Y - pictureBox4.Left + cir_Point.Y;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                  one.Y - 3, 6, 6));//画实心椭圆
            }
            else
            {
                for (int i = 0; i < list_trace.Count - 1; i++)
                {

                    point = list_trace[i];
                    point_diff = point;
                    point_diff.X = point.X - pictureBox4.Left;
                    point_diff.Y = point.Y - pictureBox4.Top;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 > 0)
                        continue;
                    //        g.FillEllipse(myBrush, new Rectangle(cir_Point.X + point_diff.X - 3, cir_Point.Y + point_diff.Y - 3, 3, 3));//画实心椭圆
                    //    g.DrawLine(new Pen(Color.Red), point_diff.X, point_diff.Y, point_diff.X, point_diff.Y);
                    //    g.DrawLine(new Pen(Color.Red), 200, 200,210, 210);
                    one.X = cir_Point.X + point_diff.X;
                    one.Y = cir_Point.Y + point_diff.Y;
                    two.X = list_trace[i + 1].X - pictureBox4.Left + cir_Point.X;
                    two.Y = list_trace[i + 1].Y - pictureBox4.Top + cir_Point.Y;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


                    g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                       one.Y - 3, 6, 6));//画实心椭圆
                    g.FillEllipse(myBrush, new Rectangle(two.X - 3,
                      two.Y - 3, 6, 6));//画实心椭圆

                    g.DrawLine(p, one, two);
                    System.Threading.Thread.Sleep(200);
                }
            }         
        }
        /// <summary>
        /// 专门画雷达波形图上真实轨迹的，，类似于地图上的轨迹用线程testmethod
        /// </summary>
        private void draw_monitor_trace_realtrace()       
        {
            for (int i = 0; i < arr_tar.Count; i++)
            {
                //QueueUserWorkItem()方法：将工作任务排入线程池。
                ThreadPool.QueueUserWorkItem(new WaitCallback(thread3), i);
                // Thread2 表示要执行的方法(与WaitCallback委托的声明必须一致)。                
            }
        }
        public void thread3(object obj)
        {
            int flag = (int)obj;
            List<Point> list_trace = new List<Point>();
            //double distance1, distance2;
            //distance1 = 7 * panel1.Width / 20;
            Graphics g;
            Pen p = new Pen(color[flag], 2);
            g = panel1.CreateGraphics();
            SolidBrush myBrush = new SolidBrush(color[flag]);//画刷
            Point point;
            Point point_diff;
            Point cir_Point = new Point(0, 0);
            Point one = new Point(0, 0);
            Point two = new Point(0, 0);
            cir_Point.X = panel1.Width / 10 * 5;
            cir_Point.Y = panel1.Height / 10 * 5;
            for (int i = 0; i < list_detect_distance_update[flag].Count ; i++)
            {
                //double类型的坐标转换成int
                list_trace.Add(new Point((int)list_detect_distance_update[flag][i].X,
                    (int)list_detect_distance_update[flag][i].Y));
                //g.DrawString();
            }
            if (list_trace.Count == 1)
            {
                one.X = list_trace[0].X - pictureBox4.Left + cir_Point.X;
                one.Y = list_trace[0].Y - pictureBox4.Left + cir_Point.Y;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                  one.Y - 3, 6, 6));//画实心椭圆
            }
            else
            {
                for (int i = 0; i < list_trace.Count - 1; i++)
                {
                    point = list_trace[i];
                    point_diff = point;
                    point_diff.X = point.X - pictureBox4.Left;
                    point_diff.Y = point.Y - pictureBox4.Top;                 
                    one.X = cir_Point.X + point_diff.X;
                    one.Y = cir_Point.Y + point_diff.Y;
                    two.X = list_trace[i + 1].X - pictureBox4.Left + cir_Point.X;
                    two.Y = list_trace[i + 1].Y - pictureBox4.Top + cir_Point.Y;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                       one.Y - 3, 6, 6));//画实心椭圆
                    g.FillEllipse(myBrush, new Rectangle(two.X - 3,
                      two.Y - 3, 6, 6));//画实心椭圆
                    g.DrawLine(p, one, two);
                    System.Threading.Thread.Sleep(200);
                }
            }           
        }
        /// <summary>
        /// 返回按钮响应事件
        /// </summary>     
        private void button_goback_Click(object sender, EventArgs e)
        {          
            textBox_doppler.Visible = false;
            button_goback.Visible = false;
            label_sel_radartype.Visible = false;
            button_update_config.Visible = false;
            buttonDectecModeling.Visible = false;
            buttonModelDone.Visible = false;
            groupBox1.Visible = true;
            groupBox2.Visible = false;
            groupBox3.Visible = false;

            textBox_juli.Visible = false;
            textBox_zaipin.Visible = false;
            textBox_chongpin.Visible = false;
            textBox_maikuan.Visible = false;
            textBox_maifu.Visible = false;
            textBox_saomiao.Visible = false;
            textBox_jiebianliang.Visible = false;
            textBox_doudongliang.Visible = false;
            button_text_update.Visible = false;

            //从指挥控制中返回
            this.label5.Visible = false;
            this.dopplercheckBox.Visible = false;
            this.multpBasecheckBox.Visible = false;
            this.bvrcheckBox.Visible = false;
            this.groupBox6.Visible = false;
            this.groupBox4.Visible = false;
            this.groupBox5.Visible = false;
            
            this.mixtrailButton.Visible = false;

            noiseFlag = NoiseEnum.NoNoise;

            pictureBox3.Visible = false;
            pictureBox4.Visible = false;
            pictureBox5.Visible = false;
            //返回到最开始，什么都没选择
            this.radioButton1.Checked = false;
            this.radioButton2.Checked = false;
            this.radioButton3.Checked = false;
            this.radioButton4.Checked = false;
            this.radioButton5.Checked = false;
            this.radioButton6.Checked = false;
            this.radioButton13.Checked = false;
            this.radioButton14.Checked = false;
            this.radioButton15.Checked = false;
            this.radioButton16.Checked = false;
            this.radioButton17.Checked = false;
            this.radioButton18.Checked = false;
            this.radioButton19.Checked = false;
            this.radioButton20.Checked = false;
            this.radioButton21.Checked = false;
            this.radioButton22.Checked = false;
            this.button_goback.Visible = false;            
        }    

        private void btn_Finish_Click(object sender, EventArgs e)
        {
            if (strCollected == string.Empty)
                MessageBox.Show("您未添加任何噪声！");
            else MessageBox.Show("您选择添加了" + strCollected, "提示");
        }

        /// <summary>
        /// 当时电子对抗的时候，给listview中添加如下额外的特征量
        /// </summary>           
        private void addfeature_to_listview(int position)
        {
            //从输入框中读取配置特征量       
            featurelistView.Items.Add("探测距离");
            if (textBox_juli.Text == "")
                textBox_juli.Text = "220";
            featurelistView.Items[++position].SubItems.Add(textBox_juli.Text );
            featurelistView.Items[position].SubItems.Add(textBox_juli.Text );

            featurelistView.Items.Add("载频");
            if (textBox_zaipin.Text == "")
                textBox_zaipin.Text = "100";
            featurelistView.Items[++position].SubItems.Add(textBox_zaipin.Text );
            featurelistView.Items[position].SubItems.Add(textBox_zaipin.Text );

            featurelistView.Items.Add("重频");
            if (textBox_chongpin.Text == "")
                textBox_chongpin.Text = "50";
            featurelistView.Items[++position].SubItems.Add(textBox_chongpin.Text );
            featurelistView.Items[position].SubItems.Add(textBox_chongpin.Text );

            featurelistView.Items.Add("脉宽");
            if (textBox_maikuan.Text == "")
                textBox_maikuan.Text = "20";
            featurelistView.Items[++position].SubItems.Add(textBox_maikuan.Text );
            featurelistView.Items[position].SubItems.Add(textBox_maikuan.Text );

            featurelistView.Items.Add("脉幅");
            if (textBox_maifu.Text == "")
                textBox_maifu.Text = "50";
            featurelistView.Items[++position].SubItems.Add(textBox_maifu.Text);
            featurelistView.Items[position].SubItems.Add(textBox_maifu.Text);

            featurelistView.Items.Add("天线扫描周期");
            if (textBox_saomiao.Text == "")
                textBox_saomiao.Text = "2.50";
            featurelistView.Items[++position].SubItems.Add(textBox_saomiao.Text);
            featurelistView.Items[position].SubItems.Add(textBox_saomiao.Text);

            featurelistView.Items.Add("载频捷变量");
            if (textBox_jiebianliang.Text == "")
                textBox_jiebianliang.Text = "0.5";
            featurelistView.Items[++position].SubItems.Add(textBox_jiebianliang.Text);
            featurelistView.Items[position].SubItems.Add(textBox_jiebianliang.Text);

            featurelistView.Items.Add("重频抖变量");
            if (textBox_doudongliang.Text == "")
                textBox_doudongliang.Text = "±1%~±20%";
            featurelistView.Items[++position].SubItems.Add(textBox_doudongliang.Text);
            featurelistView.Items[position].SubItems.Add(textBox_doudongliang.Text);
        }       

        /// <summary>
        ///  特性分析中下拉框状态改变响应函数
        /// </summary>      
        private void featurecomboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            string selectedText = featurecomboBox1.SelectedItem.ToString();

            int index = arr_tar.IndexOf(selectedText);
            //int index =(int) selectedId;
            FeatureModel feature = new FeatureModel();
            Dictionary<String, double> featDicX;
            Dictionary<String, double> featDicY;

            if (scene == Scene.COMMAND)
            {
               
                featDicX = feature.getTimeAndSpaceFeatureX(command_listmix[index], 13);
                featDicY = feature.getTimeAndSpaceFeatureY(command_listmix[index], 13);

                fftList = feature.getFrequentFFTFeature(command_listmix[index]);
                ifftList = feature.getFrequentIFFTFeature(command_listmix[index]);
            }
            else
            {
                if (noiseFlag == NoiseEnum.NoNoise)
                    return;


                if (noiseFlag == NoiseEnum.GUASSIAN)
                {
                    featDicX = feature.getTimeAndSpaceFeatureX(guassianList_final[index], 13);
                    featDicY = feature.getTimeAndSpaceFeatureY(guassianList_final[index], 13);

                    //频率分析的轨迹点
                    fftList = feature.getFrequentFFTFeature(guassianList_final[index]);
                    ifftList = feature.getFrequentIFFTFeature(guassianList_final[index]);
                }
                else if (noiseFlag == NoiseEnum.POISSON)
                {
                    featDicX = feature.getTimeAndSpaceFeatureX(poissonList_final[index], 13);
                    featDicY = feature.getTimeAndSpaceFeatureY(poissonList_final[index], 13);

                    //频率分析
                    fftList = feature.getFrequentFFTFeature(poissonList_final[index]);
                    ifftList = feature.getFrequentIFFTFeature(poissonList_final[index]);
               }
            
            else
            {
                featDicX = feature.getTimeAndSpaceFeatureX(uniformList_final[index], 13);
                featDicY = feature.getTimeAndSpaceFeatureY(uniformList_final[index], 13);
                    
                //频率分析
                fftList = feature.getFrequentFFTFeature(poissonList_final[index]);
                ifftList = feature.getFrequentIFFTFeature(poissonList_final[index]);
            }

               
            }
            String[] featName = new String[13];
            int i = 0;
            foreach (String key in featDicX.Keys)
            {
                featName[i++] = key;
            }

            featurelistView.BeginUpdate();


            featurelistView.Clear();
            ColumnHeader header1 = new ColumnHeader();
            header1.Text = " ";
            header1.Width = 85;
            ColumnHeader header2 = new ColumnHeader();
            header2.Text = "X";
            header2.Width = 90;
            ColumnHeader header3 = new ColumnHeader();
            header3.Text = "Y";
            header3.Width = 90;

            featurelistView.Columns.AddRange(new ColumnHeader[] { header1, header2, header3 });
            featurelistView.FullRowSelect = true;
            //listview 中添加数据
            //featurelistView.Items.Add(" ");
            featurelistView.Items.Add("算法");

            //listItem.SubItems.Add("数值分析");
            featurelistView.Items[0].SubItems.Add("数值分析");
            featurelistView.Items[0].SubItems.Add("数值分析");
            ListViewItem listItem = new ListViewItem();
            for (i = 0; i < 13; i++)
            {
                featurelistView.Items.Add("" + featName[i]);
                featurelistView.Items[i + 1].SubItems.Add("" + featDicX[featName[i]]);
                featurelistView.Items[i + 1].SubItems.Add("" + featDicY[featName[i]]);

            }
            //如果场景是声呐（主动）
            if (scene == Scene.ACT_SONAR)
            {
                featurelistView.Items.Add("探测距离");
                featurelistView.Items[++i].SubItems.Add(">40km");
                featurelistView.Items[i].SubItems.Add(">40km");

                featurelistView.Items.Add("方位");
                featurelistView.Items[++i].SubItems.Add("0~2*pi");
                featurelistView.Items[i].SubItems.Add("0~2*pi");
            }
            else if (scene == Scene.PAS_SONAR)   //声呐被动
            {
                featurelistView.Items.Add("探测距离");
                featurelistView.Items[++i].SubItems.Add("<20km");
                featurelistView.Items[i].SubItems.Add("<20km");

                featurelistView.Items.Add("方位");
                featurelistView.Items[++i].SubItems.Add("0~2*pi");
                featurelistView.Items[i].SubItems.Add("0~2*pi");
            }
            else if (scene == Scene.ELEC_VS)    //电子对抗
            {
                addfeature_to_listview(i);
            }

            featurelistView.View = System.Windows.Forms.View.Details;
            featurelistView.GridLines = true;
            featurelistView.EndUpdate();

            //弹出新窗口，绘制频率分析轨迹
            FrequencyForm frequentForm = new FrequencyForm();
            frequentForm.Show();
            frequentForm.draw_fft_trail(fftList, selectedText);
            frequentForm.draw_ifft_trail(ifftList, selectedText);
                
          
        }

        /// <summary>
        /// 画特性分析中间面板的坐标和圆
        /// </summary>    
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            
            //创建画板
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Black, 1/2);
            //g.DrawLine(pen, 0, 0, 335, 0);

            int factor = panel1.Width/10;
            for ( int i = 0; i < 10; i++)
            {
                //画水平线
                g.DrawLine(pen, 0, i*factor, panel1.Width, i*factor);
                //画竖直线
                g.DrawLine(pen, i*factor, 0, factor*i, panel1.Height);
            }

            //画圆
            for(int j = 1;j<5;j++)
            {
                g.DrawEllipse(pen, 4*factor-(j-1)*factor, 4*factor-(j-1)*factor, j * 2*factor, j * 2*factor);
                //g.DrawEllipse(panel1.Width/10*4,)
            }

            
            if (scene == Scene.COMMAND)
            {
                //如果是指挥控制跳转到特性分析，并且融合后轨迹有结果，那么就在波形图中添加轨迹标识
                if (this.tabControl1.SelectedIndex == 1)
                {
                    for (int i = 0; i < command_listmix.Length; i++)
                    {
                        if (command_listmix[i] == null)
                            continue;
                        if (command_listmix[i].Count != 0)
                        {
                            Pen p = new Pen(color[i], 3);

                            //画线
                            g.DrawLine(p, 430, 24 * (i + 1), 450, 24 * (i + 1));
                            //写轨迹标识
                            g.DrawString(arr_tar[i].ToString(), new Font(FontFamily.GenericMonospace, 11f), Brushes.Black, new PointF(453, 22 * (i + 1)));

                        }
                    }
                }
            }
            else
            {
                //如果添加了噪音，有轨迹，就在波形图右上角添加轨迹标识
                if (noiseFlag == NoiseEnum.GUASSIAN)
                {
                    for (int i = 0; i < arr_tar.Count; i++)
                    {
                        if (guassianList_final[i].Count != 0)
                        {
                            Pen p = new Pen(color[i], 3);

                            //画线
                            g.DrawLine(p, 430, 24 * (i + 1), 450, 24 * (i + 1));
                            //写轨迹标识
                            g.DrawString(arr_tar[i].ToString(), new Font(FontFamily.GenericMonospace, 11f), Brushes.Black, new PointF(453, 22 * (i + 1)));

                        }
                    }
                }
                else if (noiseFlag == NoiseEnum.POISSON)
                {
                    for (int i = 0; i < arr_tar.Count; i++)
                    {
                        if (poissonList_final[i].Count != 0)
                        {
                            Pen p = new Pen(color[i], 3);

                            //画线
                            g.DrawLine(p, 430, 24 * (i + 1), 450, 24 * (i + 1));
                            //写轨迹标识
                            g.DrawString(arr_tar[i].ToString(), new Font(FontFamily.GenericMonospace, 11f), Brushes.Black, new PointF(453, 22 * (i + 1)));

                        }
                    }
                }
                else if (noiseFlag == NoiseEnum.UNIFORM)
                {
                    for (int i = 0; i < arr_tar.Count; i++)
                    {
                        if (uniformList_final[i].Count != 0)
                        {
                            Pen p = new Pen(color[i], 3);

                            //画线
                            g.DrawLine(p, 430, 24 * (i + 1), 450, 24 * (i + 1));
                            //写轨迹标识
                            g.DrawString(arr_tar[i].ToString(), new Font(FontFamily.GenericMonospace, 11f), Brushes.Black, new PointF(453, 22 * (i + 1)));

                        }
                    }
                }
            }      
        }         

        /// <summary>
        /// 画地图上四条轨迹，匀加，匀减，匀速，圆周运动
        /// </summary>
        private void drawtrace_update()     //
        {
           
            for (int i = 0; i < 3; i++)    //直接生成4个线程，
            {
                //QueueUserWorkItem()方法：将工作任务排入线程池。
                ThreadPool.QueueUserWorkItem(new WaitCallback(thread_drawtrace), i);
                // TestMethod 表示要执行的方法(与WaitCallback委托的声明必须一致)            
            }          
        }
        public void thread_drawtrace(object obj)
        {
            Graphics g;
            int flag = (int)obj;
            g = axMap1.CreateGraphics();
            Pen p = new Pen(color[flag], 2);

            Point one, two;
            for (int i = 0; i < list_trace_update[flag].Count - 1; i++)
            {
                one = list_trace_update[flag][i];
                two = list_trace_update[flag][i + 1];
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                SolidBrush myBrush = new SolidBrush(color[flag]);//画刷
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                 one.Y - 3, 6, 6));//画实心椭圆
                g.FillEllipse(myBrush, new Rectangle(two.X - 3,
                  two.Y - 3, 6, 6));//画实心椭圆
                g.DrawLine(p, one, two);
              
                System.Threading.Thread.Sleep(200);
            }
            g.Dispose();
        }
        /// <summary>
        /// 生成匀速运动的数据点，保存到list_trace_update[0],起点为100，30，方向角为顺时针0 rad，
        /// </summary>
        private void constantSpeed()    //最好多生成一些点
        {
            list_trace_update[0].Clear();
            arr_tar.Clear();
            Point p=new Point();
            p.X=100;        
            p.Y=30;    
            list_trace_update[0].Add(p);          
            arr_tar.Add("0");
            for (int i = 1; i < 30; i++)
            {
                Point p1 = new Point();
                p1.X += p.X + i * 20;
                p1.Y = p.Y;                       
                list_trace_update[0].Add(p1);
            }      
        }
        /// <summary>
        /// 匀加速运动，保存到list_trace_update[1],方向角为顺时针x度
        /// </summary>
        private void constantAcceleration()     
        {
            list_trace_update[1].Clear();
            Point p=new Point();
            p.X=120;        
            p.Y=50;    //匀加速运动起点,经纬度 各为120，50
            int a=4;  //加速度为4
            list_trace_update[1].Add(p);           
            arr_tar.Add("1");
            for (int i = 1; i < 30; i++)
            {
                Point p1 = new Point();
                p1.X = p.X +(int)(2*i*i*0.9);   //cos（x）=0.9,x=?
                p1.Y = p.Y+(int)(2*i*i*0.4);     //sin（x）
                list_trace_update[1].Add(p1);
            }
        }
        /// <summary>
        ///  //匀减速运动，保存到list_trace_update[2],方向角为顺时针53度
        /// </summary>
        private void constantSlowDown()    
        {
            list_trace_update[2].Clear();
            Point p = new Point();
            p.X = 100;
            p.Y = 70;    //匀减速运动起点,经纬度 各为120，70
            int a = 4;  //加速度为4
            int v0 = 50;
            int vt = v0;
            list_trace_update[2].Add(p);          
            arr_tar.Add("2");
            for (int i = 1; i < 12; i++)
            {
                Point p1 = new Point();
                p1.X = p.X + v0*i+(int)(-2 * i * i * 0.6);   //cos（53）=0.6,
                p1.Y = p.Y +  v0*i+ (int)(-2 * i * i * 0.8);     //sin（53）
                vt = v0 - 2 * i;
                if(vt>=0)
                    list_trace_update[2].Add(p1);
            }
        }
        /// <summary>
        ///  圆周运动，周期为PI，保存到list_trace_update[3]
        /// </summary>
        private void circleMotion()        
        {
            list_trace_update[3].Clear();
            Point p = new Point();
            p.X = 200;
            p.Y = 200;    //圆周运动起点,经纬度 各为200，200
           
            int T=20;    //周期为20
            int r = 50;
            double pi=3.1415;          
            arr_tar.Add("3");
            Graphics g;
            SolidBrush myBrush = new SolidBrush(color[3]);//画刷
            Pen pen = new Pen(color[3],2);
            g = panel1.CreateGraphics();
        //    Rect rect;
         //   g.DrawEllipse();
            g = axMap1.CreateGraphics();
            g.DrawEllipse(pen,150,100,200,200);

            Point []p1 = new Point[9];

            p1[0].X=  250; p1[0].Y=  100;
            p1[1].X = 300; p1[1].Y = 115;
            p1[2].X = 350; p1[2].Y = 200;            
            p1[3].X = 300; p1[3].Y = 285;
            p1[4].X = 250; p1[4].Y = 300;
            p1[5].X = 200; p1[5].Y = 285;
            p1[6].X = 150; p1[6].Y = 200;
            p1[7].X = 190; p1[7].Y = 115;
            p1[8].X = p1[0].X; p1[8].Y = p1[0].Y;
            for (int i = 0; i < 9; i++)
            {
                list_trace_update[3].Add(p1[i]);
                //System.Threading.Thread.Sleep(50);
            }
                

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int j = 0; j < 8;j++ )
                g.FillEllipse(myBrush, new Rectangle(p1[j].X - 3,
                   p1[j].Y - 3, 6, 6));//画实心椭圆      
        }
      
        /// <summary>
        /// OnMouseDown,以及OnMouseUp，OnMouseMove三个鼠标事件，分别是鼠标左键按下事件，
        /// 鼠标左键放开事件，鼠标移动事件，都是针对的雷达图标（即picturebox4）.
        /// </summary>   
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;  //可以拖动
            currentX = e.X;
            currentY = e.Y;
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                pictureBox4.Top = pictureBox4.Top + (e.Y - currentY);
                pictureBox4.Left = pictureBox4.Left + (e.X - currentX);
              //  System.Threading.Thread.Sleep(1000);
            }
            if (pictureBox4.Top >= (tabControl1.Size.Height - 50) || 
                pictureBox4.Left >= (tabControl1.Size.Width - 270)||pictureBox4.Top<-20
                ||pictureBox4.Left<-20)
            {
                double MapX = 103, mapY = 36;  //精度 ，纬度
                float screenX = 0, screenY = 0; //屏幕坐标
                axMap1.ConvertCoord(ref screenX, ref screenY, ref MapX, ref mapY,
                                    MapXLib.ConversionConstants.miMapToScreen);  //已知经纬度 转换为屏幕坐标
                pictureBox4.Top = (int)screenY;
                pictureBox4.Left =(int) screenX;
            }
            isDragging = false;        
            if (checkBox_udpSocket.Checked == false)
            {                
                circleMotion();                 
            }            
            drawtrace_update();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            isDragging = true;  //可以拖动
            float X, Y;
            X = pictureBox4.Left;  //SrnPt为鼠标点
            Y = pictureBox4.Top;
            double mapX1 = 0, mapY1 = 0;
            axMap1.ConvertCoord(ref X, ref Y, ref mapX1, ref mapY1, MapXLib.ConversionConstants.miScreenToMap);
            textBox_longitude.Text = mapX1.ToString();  
            textBox_latitude.Text = mapY1.ToString();          
        }  
        //tab切换响应
        private void Feature_SelectedIndexChanged(object sender, EventArgs e)   //这段代码是有冗余的，
        {           
            if (this.tabControl1.SelectedIndex == 0)
            {          
               drawtrace_update();               
            }
            else if (tabControl1.SelectedIndex == 1)  
            {
                checkBox1.Checked = false;
                if (scene == Scene.COMMAND)
                {
                    draw_mix_trail();
                    for (int i = 0; i < arr_tar.Count; i++)
                    {
                        if (command_listmix[i].Count != 0)
                            if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)  //去重
                                this.featurecomboBox1.Items.Add("" + arr_tar[i]);

                    }
                }
                else
                {
                    if (noiseFlag == NoiseEnum.GUASSIAN)
                    {
                        //显示添加高斯噪音的轨迹
                        
                        draw_monitor_trace();
                        for (int i = 0; i < arr_tar.Count; i++)
                        {
                            if (guassianList_final[i].Count != 0)
                                if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)  //去重
                                    this.featurecomboBox1.Items.Add("" + arr_tar[i]);
                           
                        }
                        //       });
                    }
                    else if (noiseFlag == NoiseEnum.POISSON)
                    {                     
                        draw_monitor_trace();
                        for (int i = 0; i < arr_tar.Count; i++)
                        {
                            if (poissonList_final[i].Count != 0)
                                if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)
                                    this.featurecomboBox1.Items.Add("" + arr_tar[i]);                          
                        }
                    }
                    else if (noiseFlag == NoiseEnum.UNIFORM)
                    {
                        draw_monitor_trace();
                        for (int i = 0; i < arr_tar.Count; i++)
                        {
                            if (uniformList_final[i].Count != 0)
                                if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)
                                    this.featurecomboBox1.Items.Add("" + arr_tar[i]);                           
                        }
                    }
                    else if (noiseFlag == NoiseEnum.NoNoise)
                    {
                        MessageBox.Show("未添加任何噪声，请先建模！");
                    }
                }
            }          
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {           
        }        
        /// <summary>
        /// 显示特性分析中X坐标轴的刻度         
        /// </summary>      
        private void Xpanel_Paint(object sender, PaintEventArgs e)
        {
            
            Graphics g = Xpanel.CreateGraphics();
            double start = -1;
            int sLoc = 0;
            int addition = 50;
            for (int i = 0; i < 11; i++)
            {
                g.DrawString((start + 0.2 * i).ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(sLoc + addition * i, 0));
            }
        }

      /// <summary>
      /// 显示特性分析中Y轴坐标刻度         
      /// </summary>    
        private void Ypanel_Paint(object sender, PaintEventArgs e)
        {
            
            Graphics g = Ypanel.CreateGraphics();
            double start = 1;
            int sLoc = 0;
            int addition = 50;
            for (int i = 0; i < 11; i++)
            {
                g.DrawString((start - 0.2 * i).ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(0, sLoc + addition * i));
            }
        }
        /// <summary>
        /// 为list_detect_distance_update数组填充数据
        /// </summary>
        private void prepareforListDetectDis()
        {
            for (int i = 0; i < arr_tar.Count; i++)
                list_detect_distance_update[i].Clear();
            double distance1, distance2;
            distance1 = 7 * panel1.Width / 20;
            PointD point = new PointD();
            PointD point_diff = new PointD();

            for (int i = 0; i < arr_tar.Count; i++)
            {
                for (int j = 0; j < list_trace_update[i].Count; j++)
                {
                    point.X = list_trace_update[i][j].X;
                    point.Y = list_trace_update[i][j].Y;

                    point_diff.X = point.X;
                    point_diff.Y = point.Y;
                    double x, y;
                    x = System.Math.Abs(point.X - pictureBox4.Left);
                    y = System.Math.Abs(point.Y - pictureBox4.Top);                  
                    point_diff.X = x;
                    point_diff.Y = y;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        list_detect_distance_update[i].Add(point_save);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
        /// <summary>
        /// 清空list_detect_distance_update数组
        /// </summary>
        private void clearforListDetectDis()        
        {
            for (int i = 0; i < arr_tar.Count; i++)
            {
                list_trace_update[i].Clear();
                list_detect_distance_update[i].Clear();
            }
        }
        /// <summary>
        ///  清空guassList_final,poissionList_final,uniformList_final数组
        /// </summary>
        private void clearfornoiseListfinal()       
        {
            for (int i = 0; i < arr_tar.Count; i++)
            {                
                guassianList[i].Clear();
                poissonList[i].Clear();
                uniformList[i].Clear();
                guassianList_final[i].Clear();
                poissonList_final[i].Clear();
                uniformList_final[i].Clear();
            }               
        }
        /// <summary>
        /// 第一组groupbox1中的radiobutton,都对应了这个事件
        /// </summary>    
        private void radioButton1_CheckedChanged(object sender, EventArgs e)  
        {
            clearforListDetectDis();  //切换到不同雷达，清空list_detect_distance_update 数组
            clearfornoiseListfinal(); //切换到不同雷达， 清空guassList,guassList_final,等噪音数组
            arr_tar.Clear();
            if (checkBox_udpSocket.Checked == false)
            {
                constantSpeed();
                constantAcceleration();
                constantSlowDown();
                circleMotion();               
                drawtrace_update();               
            }      
            if (radioButton1.Checked == true || radioButton2.Checked == true || radioButton3.Checked == true)
            {
                textBox_juli.Visible = true;
                textBox_zaipin.Visible = true;
                textBox_chongpin.Visible = true;
                textBox_maikuan.Visible = true;
                textBox_maifu.Visible = true;
                textBox_saomiao.Visible = true;
                textBox_jiebianliang.Visible = true;
                textBox_doudongliang.Visible = true;
                button_text_update.Visible = true;
            }
            double MapX = 103, mapY = 36;  //精度 ，纬度
            float screenX = 0, screenY = 0; //屏幕坐标
            axMap1.ConvertCoord(ref screenX, ref screenY, ref MapX, ref mapY,
                                MapXLib.ConversionConstants.miMapToScreen);  //已知经纬度 转换为屏幕坐标
            if (radioButton1.Checked == true)  //选中了第一个单选按钮，即选择了多普勒雷达
            {                            
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.duopule;
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "多普勒雷达";
                groupBox1.Visible = false;
                //设置当期场景为多普勒
                scene = Scene.DOPPLER;
                //清空combobox的值
                this.featurecomboBox1.Items.Clear();              
                pictureBox4.Left =(int) screenX;
                pictureBox4.Top = (int)screenY;                     
                flag_init_editchange = true;
                readTxt();               
            }
            if (radioButton2.Checked == true)  //选中了第二个单选按钮，即选择了多基地雷达
            {            
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.radarpic; 
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "多基地雷达";
                groupBox1.Visible = false;              
                scene = Scene.MUTLIBASE;               
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY;             
                readTxt();

            }
            if (radioButton3.Checked == true)  //选中了第3个单选按钮，即选择了超视距雷达
            {                            
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.HVR;  
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "超视距雷达";
                groupBox1.Visible = false;               
                scene = Scene.BVR;                
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY;           
                readTxt();

            }
            if (radioButton4.Checked == true)  //选中了第4个单选按钮，即选择了声呐（主动）
            {                              
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.SONAR;  
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "声呐主动";
                groupBox1.Visible = false;
                scene = Scene.ACT_SONAR;             
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY;        

            }
            if(radioButton13.Checked == true) //选中了声呐（被动）
            {
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.SONAR;  //还没替换为多基地雷达图标
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "声呐主动";
                groupBox1.Visible = false;
                scene = Scene.PAS_SONAR;               
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY; 
            }
            if (radioButton5.Checked == true)  //选中了第5个单选按钮，即选择了电子对抗
            {
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.radarpic;  //还没替换为多基地雷达图标
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "电子对抗";
                groupBox1.Visible = false;
                scene = Scene.ELEC_VS;               
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY;               
            }
            if (radioButton6.Checked == true)  //选中了第6个单选按钮，即选择了指挥控制
            {             
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "指挥控制";
                groupBox1.Visible = false;
                scene = Scene.COMMAND;            
                this.featurecomboBox1.Items.Clear();
                this.label5.Visible = true;
                this.label5.Location = new System.Drawing.Point(775, 40);

                this.dopplercheckBox.Visible = true;
                this.dopplercheckBox.Location = new System.Drawing.Point(785,60);
                this.multpBasecheckBox.Visible = true;
                this.multpBasecheckBox.Location = new System.Drawing.Point(785, 150);
                this.bvrcheckBox.Visible = true;
                this.bvrcheckBox.Location = new System.Drawing.Point(785, 240);

                this.dopplercheckBox.Checked = false;
                this.multpBasecheckBox.Checked = false;
                this.bvrcheckBox.Checked = false;

                this.mixtrailButton.Location = new System.Drawing.Point(775, 370);
                this.mixtrailButton.Visible = true;         
             }
            if (radioButton13.Checked == true)  //选中了第7个单选按钮，即选择了声呐（被动）
            {
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.SONAR;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "声呐被动";
                groupBox1.Visible = false;
              
                scene = Scene.PAS_SONAR;               
                this.featurecomboBox1.Items.Clear();           
             }         
            button_text_update.Enabled = false;
            button_goback.Visible = true;
        }
        /// <summary>
        /// 读配置文件公共接口
        /// </summary>    
        private void read_interface(int line_num)      
        {
            String path = Application.StartupPath + "\\configure.txt";
         //   StreamReader sr = new StreamReader(path, Encoding.Default);
            string[] content_read = System.IO.File.ReadAllText(path).Split(new char[] { '\r', '\n' },
               StringSplitOptions.RemoveEmptyEntries);
            int count = 1;
            int line_count = 0;
            while (count < 9)
            {
                textBox_doppler.Text += content_read[line_num * 10 + count].Split('\t')[0];
                textBox_doppler.Text += "\r\n\r\n";
                if ((line_count+1) % 3 == 0)
                {
                    textBox_doppler.Text += "\r\n"; //多加一个换行，保持美观
                    //line_count = 0;
                }
                if(line_count==4)
                    textBox_doppler.Text += "\r\n"; //多加一个换行，保持美观
                if (count == 1)
                    textBox_juli.Text = content_read[line_num * 10 + count].Split('\t')[1];
                else if (count == 2)
                    textBox_zaipin.Text = content_read[line_num * 10 + count].Split('\t')[1];
                else if (count == 3)
                    textBox_chongpin.Text = content_read[line_num * 10 + count].Split('\t')[1];
                else if (count == 4)
                    textBox_maikuan.Text = content_read[line_num * 10 + count].Split('\t')[1];
                else if (count == 5)
                    textBox_maifu.Text = content_read[line_num * 10 + count].Split('\t')[1];
                else if (count == 6)
                    textBox_saomiao.Text = content_read[line_num * 10 + count].Split('\t')[1];
                else if (count == 7)
                    textBox_jiebianliang.Text = content_read[line_num * 10 + count].Split('\t')[1];
                else if (count == 8)
                    textBox_doudongliang.Text = content_read[line_num * 10 + count].Split('\t')[1];
                count++;
                line_count++;
            }
        }
        private void readTxt()
        {
            textBox_doppler.Visible = true;
            button_update_config.Visible = true;            
            textBox_doppler.Text = "";                  
            int line_num = 0;            
            if (radioButton1.Checked == true)
            {
                line_num = 0;               
            }
            if (radioButton2.Checked == true)
            {
                line_num = 1;               
            }
            if (radioButton3.Checked == true)
            {
                line_num = 2;               
            }
            read_interface(line_num);           
        }
        /// <summary>
        /// 选择文件更新 按钮响应事件
        /// </summary>  
        private void OnButtonUpdateConfigClick(object sender, EventArgs e)  
        {
            buttonDectecModeling.Enabled = true;
                OpenFileDialog openFileDialog = new OpenFileDialog();
                String path1 = Application.StartupPath + "\\configure.txt";
                openFileDialog.InitialDirectory = path1;
                openFileDialog.Filter = "文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.FilterIndex = 1;
                string path = "";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    path = openFileDialog.FileName;
                }
                if(path!="")
                    System.Diagnostics.Process.Start(path);     
        }
        /// <summary>
        /// 特征量所做修改写入到配置文件
        /// </summary>
        private void wrTxt()
        {         
            int line_num=0; //行号
            String path = Application.StartupPath + "\\configure.txt";            
            string[] content = System.IO.File.ReadAllText(path).Split(new char[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            if (radioButton1.Checked == true)   //多普勒雷达被选中
            {
                line_num = 0;  //指定修改哪些行，最后一次整个文件都会更新
            }
            else if (radioButton2.Checked == true)
            {
                line_num = 10;
            }
            else if (radioButton3.Checked == true)
            {
                line_num = 20;
            }                             
            content[line_num* 1 + 1] = "探测距离(km)"+"\t"+textBox_juli.Text;
            content[line_num * 1 + 2] = "载频(GHZ)"+"\t"+textBox_zaipin.Text;
            content[line_num * 1 + 3] = "重频(GHZ)" + "\t" + textBox_chongpin.Text;
            content[line_num * 1 + 4] = "脉宽(us)" + "\t" + textBox_maikuan.Text;
            content[line_num * 1 + 5] = "脉幅（db）" + "\t" + textBox_maifu.Text;
            content[line_num * 1 + 6] = "天线扫描周期（s）" + "\t" + textBox_saomiao.Text;
            content[line_num * 1 + 7] = "载频捷变量" + "\t" + textBox_jiebianliang.Text;
            content[line_num * 1 + 8] = "重频抖动量" + "\t" + textBox_doudongliang.Text;      
            System.IO.File.WriteAllText(path, string.Join("\r\n", content),Encoding.Unicode);
        }
        /// <summary>
        /// 探测建模按钮响应事件
        /// </summary>      
        private void OnButtonDetectModeling(object sender, EventArgs e)  
        {
            buttonModelDone.Enabled = false;
            textBox_juli.Visible = false;
            textBox_zaipin.Visible = false;
            textBox_chongpin.Visible = false;
            textBox_maikuan.Visible = false;
            textBox_maifu.Visible = false;
            textBox_saomiao.Visible = false;
            textBox_jiebianliang.Visible = false;
            textBox_doudongliang.Visible = false;
            button_text_update.Visible = false;
            if (radioButton6.Checked == false)  //不是指挥控制
            {
                groupBox1.Visible = false;
                buttonDectecModeling.Visible = false;
                button_update_config.Visible = false;
                textBox_doppler.Visible = false;
                groupBox2.Visible = true;
                buttonModelDone.Visible = true;
                button_goback.Visible = true;     //需要在程序最前面 说明每个控件的名字代表的意义，方便阅读代码
                button_goback.Enabled = true;
                radioButton7.Checked = false;     //清除单选按钮选中状态
                radioButton8.Checked = false;
                radioButton9.Checked = false;
            }
            else
            {
                groupBox1.Visible = false;
                buttonDectecModeling.Visible = false;
                button_update_config.Visible = false;
                textBox_doppler.Visible = false;
                groupBox2.Visible = true;
                groupBox3.Visible = true;
                buttonModelDone.Visible = true;
                button_goback.Visible = true;
                button_goback.Enabled = true;
                radioButton7.Checked = false;     //清除单选按钮选中状态
                radioButton8.Checked = false;
                radioButton9.Checked = false;
            }
        }
        /// <summary>
        /// 计算平均值和方差
        /// </summary>
        /// <param name="list"></param>
        /// <param name="xMean"></param>
        /// <param name="xVariance"></param>
        /// <param name="yMean"></param>
        /// <param name="yVariance"></param>
        public void computeMeanVar(List<PointD> list, out double xMean, out double xVariance, 
            out double yMean, out double yVariance)
        {
            xMean = 0.0; xVariance = 0.0;
            yMean = 0.0; yVariance = 0.0;
            for (int i = 0; i < list.Count; i++)
            {
                xMean += list[i].X;
                yMean += list[i].Y;

            }
            xMean /= list.Count;
            yMean /= list.Count;
            //方差
            for (int j = 0; j < list.Count; j++)
            {
                xVariance += Math.Pow((list[j].X - xMean), 2);
                yVariance += Math.Pow((list[j].Y - yMean), 2);
            }
            xVariance /= list.Count;
            xVariance = Math.Pow(xVariance, 1 / 2);
            yVariance /= list.Count;
            yVariance = Math.Pow(yVariance, 1 / 2);
        }
        /// <summary>
        /// 为guassList_final准备数据
        /// </summary>
        private void prepareforguassListFinal()     
        {
            for (int i = 0; i < arr_tar.Count; i++)
                guassianList_final[i].Clear();
            double distance1, distance2;
            distance1 = 7 * panel1.Width / 20;
            PointD point = new PointD();
            PointD point_diff = new PointD();
            for (int i = 0; i < arr_tar.Count; i++)
            {           
                for (int j = 0; j < guassianList[i].Count; j++)
                {
                    point.X = guassianList[i][j].X;
                    point.Y = guassianList[i][j].Y;

                    point_diff.X = point.X;
                    point_diff.Y = point.Y;

                    point_diff.X = point.X - pictureBox4.Left;
                    point_diff.Y = point.Y - pictureBox4.Top;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {                       
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;                        
                        guassianList_final[i].Add(point_save);                        
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
        /// <summary>
        /// 为poissonList_final准备数据
        /// </summary>
        private void prepareforpoissonListFinal()     
        {
            for (int i = 0; i < arr_tar.Count; i++)
                poissonList_final[i].Clear();
            double distance1, distance2;
            distance1 = 7 * panel1.Width / 20;
            PointD point = new PointD();
            PointD point_diff = new PointD();
            for (int i = 0; i < arr_tar.Count; i++)
            {
               for (int j = 0; j < poissonList[i].Count; j++)
               {
                    point.X = poissonList[i][j].X;
                    point.Y = poissonList[i][j].Y;

                    point_diff.X = point.X;
                    point_diff.Y = point.Y;

                    point_diff.X = point.X - pictureBox4.Left;
                    point_diff.Y = point.Y - pictureBox4.Top;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;                        
                        poissonList_final[i].Add(point_save);                        
                    }
                    else
                    {
                        continue;
                    }
                    
                }
            }
        }
        /// <summary>
        ///  为guniformList_final准备数据
        /// </summary>       
        private void prepareforuniformListfinal()
        {
            for (int i = 0; i < arr_tar.Count; i++)
                uniformList_final[i].Clear();
            double distance1, distance2;
            distance1 = 7 * panel1.Width / 20;
            PointD point = new PointD();
            PointD point_diff = new PointD();
            for (int i = 0; i < arr_tar.Count; i++)
            {
                for (int j = 0; j < uniformList[i].Count; j++)
                {
                    point.X = uniformList[i][j].X;
                    point.Y = uniformList[i][j].Y;
                    point_diff.X = point.X;
                    point_diff.Y = point.Y;
                    point_diff.X = point.X - pictureBox4.Left;
                    point_diff.Y = point.Y - pictureBox4.Top;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;                   
                        uniformList_final[i].Add(point_save);                        
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
        /// <summary>
        /// 建模完成按钮响应事件
        /// </summary>        
        private void OnButtonModelDone(object sender, EventArgs e)
        {
            prepareforListDetectDis();
            //prepareforListDetectDisFin();   //准备数据，为数组list_detect_distance_final,注意：每次切换一种雷达时候，

            this.featurecomboBox1.Items.Clear();//清空combobox内的值
            this.featurelistView.Items.Clear();
            
            //需要先清空list_detect_distance_final
            if (radioButton7.Checked == true)
            {
                //添加高斯噪声
                //均值
                double xMean = 0, xVariance = 0;
                double yMean = 0, yVariance = 0;

                //计算均值和方差
                for (int i = 0; i < arr_tar.Count; i++)     //得到guassList数组
                {
                    computeMeanVar(list_detect_distance_update[i], out xMean, out xVariance, out yMean, out yVariance);
                    guassianList[i] = new List<PointD>(Noise.addGuassianNoise(list_detect_distance_update[i].ToArray(),
                        xMean, xVariance, yMean, yVariance));
                }
                prepareforguassListFinal();
                button_goback.Enabled = true;
                if (DialogResult.OK == MessageBox.Show("congratulations! 添加噪声完毕，你选择添加了高斯白噪声"))
                {
                    noiseFlag = NoiseEnum.GUASSIAN;
                    //将当前选中的tab页设为特性分析
                    this.tabControl1.SelectedIndex = 1;
                }
            }
            else if (radioButton8.Checked == true)
            {
                //添加泊松噪音
                for (int i = 0; i < arr_tar.Count; i++)
                {
                    poissonList[i] = new List<PointD>(Noise.addPoissonNoise(list_detect_distance_update[i].ToArray(),
                        (panel1.Width / 10) * 7, (panel1.Width / 10) * 7));
                }
                prepareforpoissonListFinal();
                button_goback.Enabled = true;
                if (DialogResult.OK == MessageBox.Show("congratulations! 添加噪声完毕，你选择添加了泊松噪声"))
                {                    
                    noiseFlag = NoiseEnum.POISSON;
                    //将当前的页面切换成特性分析
                    this.tabControl1.SelectedIndex = 1;
                }              
            }
            else if (radioButton9.Checked == true)
            {
                //添加均匀噪声
                //计算均匀分布的a和b
                double xMean = 0, xVariance = 0;
                double yMean = 0, yVariance = 0;
                double XA = 0, XB = 0;
                double YA = 0, YB = 0;
                for (int i = 0; i < arr_tar.Count; i++)
                {
                    computeMeanVar(list_detect_distance_update[i], out xMean, out xVariance, out yMean, out yVariance);
                    XA = xMean - Math.Pow(3, 1 / 2) * Math.Pow(xVariance, 2);
                    XB = xMean + Math.Pow(3, 1 / 2) * Math.Pow(xVariance, 2);
                    YA = yMean - Math.Pow(3, 1 / 2) * Math.Pow(yVariance, 2);
                    YB = yMean + Math.Pow(3, 1 / 2) * Math.Pow(yVariance, 2);
                    uniformList[i] = new List<PointD>(Noise.addUniformNoise(list_detect_distance_update[i].ToArray(),
                        XA, XB, YA, YB));                  
                }
                prepareforuniformListfinal();
                button_goback.Enabled = true;
                if (DialogResult.OK == MessageBox.Show("congratulations! 添加噪声完毕，你选择添加了平均噪声"))
                {
                    noiseFlag = NoiseEnum.UNIFORM;
                    this.tabControl1.SelectedIndex = 1;
                }              
            }         
        }
        /// <summary>
        /// 选中真实轨迹复选框 响应事件
        /// </summary>    
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {            
            if (this.checkBox1.Checked == true)
            {
                MessageBox.Show("选中真实轨迹");             
                draw_monitor_trace_realtrace();
            }               
        }
        /// <summary>
        /// 地图工具集选项响应事件
        /// </summary>     
        private void comboBox_ToolList_SelectedIndexChanged(object sender, EventArgs e)
        {            
            switch (comboBox_ToolList.SelectedIndex)
            {
                case 0:
                    axMap1.CurrentTool = MapXLib.ToolConstants.miZoomInTool;
                    break;
                case 1:
                    axMap1.CurrentTool = MapXLib.ToolConstants.miZoomOutTool;
                    break;
                case 2:
                    axMap1.CurrentTool = MapXLib.ToolConstants.miPanTool;
                    break;
                case 3:
                    axMap1.CurrentTool = MapXLib.ToolConstants.miCenterTool;
                    break;
                case 4:
                    axMap1.CurrentTool = MapXLib.ToolConstants.miLabelTool;
                    break;
                default:
                    axMap1.CurrentTool = MapXLib.ToolConstants.miPanTool;
                    break;
            }           
        }
        /// <summary>
        /// 特征量对应的文本框内容发生改变 事件
        /// </summary>   
        private void TextChanged(object sender, EventArgs e)
        {
            button_text_update.Enabled = true;
            flag_editchange = true;
        }
        /// <summary>
        /// 文本框更新按钮响应事件
        /// </summary>     
        private void UpdateTextToTxt(object sender, EventArgs e)
        {
            buttonDectecModeling.Enabled = true;
            wrTxt();
            readTxt();
            MessageBox.Show("配置文件更新完成");
        }
        /// <summary>
        /// 经度纬度文本框内容发生修改 事件
        /// </summary>        
        private void Text_jinduAndweiduChanged(object sender, EventArgs e)
        {
            double MapX=0,MapY=0;
            if(textBox_longitude.Text!="")
                   MapX = Convert.ToDouble(textBox_longitude.Text);  //经度

            if(textBox_latitude.Text!="")
                   MapY = Convert.ToDouble(textBox_latitude.Text);  //纬度
            float screenX = 0, screenY = 0; //屏幕坐标

            if (textBox_longitude.Text != "" && textBox_latitude.Text != "")
            {
                axMap1.ConvertCoord(ref screenX, ref screenY, ref MapX, ref MapY,
                                MapXLib.ConversionConstants.miMapToScreen);  //已知经纬度 转换为屏幕坐标
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY;
            }              
        }
        /// <summary>
        /// 以下三个函数，7，8，9checkedchanged，选中了噪声，建模完成按钮变为可点击
        /// </summary>    
        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            buttonModelDone.Enabled = true;
        }
        private void radioButton8_Click(object sender, EventArgs e)
        {
            buttonModelDone.Enabled = true;
        }
        private void radioButton9_Click(object sender, EventArgs e)
        {
            buttonModelDone.Enabled = true;
        }  
        /// <summary>
        /// 如下三个checkbox状态改变函数用来监听选择的雷达类型       
        /// </summary>   
        private void dopplercheckBox_CheckedChanged(object sender, EventArgs e)
        {
            //checkbox 状态改变
            if (this.dopplercheckBox.CheckState == CheckState.Checked)
            {                
                if (hasChoosedRadar < 2)
                {
                    hasChoosedRadar++;
                    //显示雷达图片
                    pictureBox3.BackgroundImage = global::radarsystem.Properties.Resources.duopule;
                    pictureBox3.Visible = true;
                    //显示添加噪音单选框
                    this.groupBox4.Visible = true;
                    this.groupBox4.Location = new System.Drawing.Point(870, 55);
                }
                else
                {
                    MessageBox.Show("只能选择两个雷达");
                    hasChoosedRadar++;
                }                              
            }
            else
            {
                this.groupBox4.Visible = false;
                pictureBox3.Visible = false;
                hasChoosedRadar--;
            }

        }

        private void multpBasecheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.multpBasecheckBox.CheckState == CheckState.Checked)
            {
                if (hasChoosedRadar < 2)
                {
                    hasChoosedRadar++;
                    //MessageBox.Show("lllll");
                    pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.radarpic;  //还没替换为多基地雷达图标
                    pictureBox4.Visible = true;
                    this.groupBox5.Visible = true;
                    this.groupBox5.Location = new System.Drawing.Point(870, 150);
                }
                else
                {
                    MessageBox.Show("只能选择两个雷达");
                    hasChoosedRadar++;
                }

                
            }
            else
            {
                this.groupBox5.Visible = false;
                pictureBox4.Visible = false;
                hasChoosedRadar--;
            }
        }

        private void bvrcheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if(this.bvrcheckBox.CheckState == CheckState.Checked)
            {
                if (hasChoosedRadar < 2)
                {
                    hasChoosedRadar++;
                    pictureBox5.BackgroundImage = global::radarsystem.Properties.Resources.HVR;  //还没替换为多基地雷达图标
                    pictureBox5.Visible = true;

                    this.groupBox6.Visible = true;
                    this.groupBox6.Location = new System.Drawing.Point(870, 245);
                }
                else
                {
                    MessageBox.Show("只能选择两个雷达");
                    hasChoosedRadar++;
                }
               
            }
            else
            {
                this.groupBox6.Visible = false;
                pictureBox5.Visible = false;
                hasChoosedRadar--;
            }
        }
       

        private void pictureBox3_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;  //可以拖动
            currentX = e.X;
            currentY = e.Y;
        }      
        private void pictureBox3_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                pictureBox3.Top = pictureBox3.Top + (e.Y - currentY);
                pictureBox3.Left = pictureBox3.Left + (e.X - currentX);
                //  System.Threading.Thread.Sleep(1000);
            }
            isDragging = false;
            constantSpeed();
            constantAcceleration();
            constantSlowDown();          
            drawtrace_update();
        }
        private void pictureBox5_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;  //可以拖动
            currentX = e.X;
            currentY = e.Y;
        }
        private void pictureBox5_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                pictureBox5.Top = pictureBox5.Top + (e.Y - currentY);
                pictureBox5.Left = pictureBox5.Left + (e.X - currentX);
                //  System.Threading.Thread.Sleep(1000);
            }
            isDragging = false;
            constantSpeed();
            constantAcceleration();
            constantSlowDown();
            //    circleMotion();
            // drawtrace();
            drawtrace_update();
        }
        /// <summary>
        /// 为指挥控制的command_listone，command_listtwo填充数据
        /// </summary>
        private void prepareforoption12()
        {
            for (int i = 0; i < arr_tar.Count; i++)
            {
                command_listone[i].Clear();
                command_listtwo[i].Clear();
                command_listmix[i].Clear();
            }
            double distance1, distance2,distance3;
            distance1 = 7 * panel1.Width / 20;
            PointD point = new PointD();
            PointD point_diff = new PointD();
            PointD point_diff1 = new PointD();
            for (int i = 0; i < arr_tar.Count; i++)
            {
                for (int j = 0; j < list_trace_update[i].Count; j++)
                {
                    point.X = list_trace_update[i][j].X;
                    point.Y = list_trace_update[i][j].Y;

                    point_diff.X = point.X;
                    point_diff.Y = point.Y;
                    double x, y;
                    x = System.Math.Abs(point.X - pictureBox3.Left);        
                    y = System.Math.Abs(point.Y - pictureBox3.Top);

                    double x1, y1;
                    x1 = System.Math.Abs(point.X - pictureBox4.Left);
                    y1 = System.Math.Abs(point.Y - pictureBox4.Top);
                    //point_diff.X = System.Math.Abs(point.X - pictureBox4.Left);
                    //point_diff.Y = System.Math.Abs(point.Y - pictureBox4.Top);
                    point_diff.X = x;
                    point_diff.Y = y;

                    point_diff1.X = x1;
                    point_diff1.Y = y1;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    distance3 = Math.Sqrt(point_diff1.X * point_diff1.X + point_diff1.Y * point_diff1.Y);
                    if (distance2 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {                        
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        command_listone[i].Add(point_save);                        
                    }              
                    else if (distance3 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {                       
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        command_listtwo[i].Add(point_save);                        
                    }
                    else
                    {
                        continue;
                    }
                }

            }
            double xMean = 0, xVariance = 0;
            double yMean = 0, yVariance = 0;

            double xMean1 = 0, xVariance1 = 0;
            double yMean1 = 0, yVariance1 = 0;
            if (radioButton14.Checked == true&&radioButton17.Checked == true)    
            //指挥控制选择的第一个,第二个雷达，均选择高斯噪声
            {
                //添加高斯噪声
                //均值
                //noiseFlag = NoiseEnum.GUASSIAN;        
                //计算均值和方差
                for (int i = 0; i < arr_tar.Count; i++)     //得到guassList数组
                {
                    computeMeanVar(command_listone[i], out xMean, out xVariance, out yMean, out yVariance);
                    command_listone[i] = new List<PointD>(Noise.addGuassianNoise(command_listone[i].ToArray(),
                        xMean, xVariance, yMean, yVariance));

                    computeMeanVar(command_listtwo[i], out xMean1, out xVariance1, out yMean1, out yVariance1);
                    command_listtwo[i] = new List<PointD>(Noise.addGuassianNoise(command_listtwo[i].ToArray(),
                        xMean1, xVariance1, yMean1, yVariance1));
                }
            }
            if (radioButton14.Checked == true && radioButton19.Checked == true)
            //指挥控制选择的第一个,第二个雷达，分别选择高斯噪声，均匀噪声
            {
                //添加高斯噪声
                //均值
                //计算均值和方差
                for (int i = 0; i < arr_tar.Count; i++)     //得到guassList数组
                {
                    computeMeanVar(command_listone[i], out xMean, out xVariance, out yMean, out yVariance);
                    command_listone[i] = new List<PointD>(Noise.addGuassianNoise(command_listone[i].ToArray(),
                        xMean, xVariance, yMean, yVariance));

                    computeMeanVar(command_listtwo[i], out xMean1, out xVariance1, out yMean1, out yVariance1);
                    command_listtwo[i] = new List<PointD>(Noise.addGuassianNoise(command_listtwo[i].ToArray(),
                        xMean1, xVariance1, yMean1, yVariance1));
                }
            }

            if (radioButton16.Checked == true && radioButton17.Checked == true)
            //指挥控制选择的第一个,第二个雷达，分别选择均匀噪声，高斯噪声
            {
                //添加高斯噪声
                //均值
                //计算均值和方差
                for (int i = 0; i < arr_tar.Count; i++)     //得到guassList数组
                {
                    computeMeanVar(command_listone[i], out xMean, out xVariance, out yMean, out yVariance);
                    command_listone[i] = new List<PointD>(Noise.addUniformNoise(command_listone[i].ToArray(),
                        xMean, xVariance, yMean, yVariance));

                    computeMeanVar(command_listtwo[i], out xMean1, out xVariance1, out yMean1, out yVariance1);
                    command_listtwo[i] = new List<PointD>(Noise.addGuassianNoise(command_listtwo[i].ToArray(),
                        xMean1, xVariance1, yMean1, yVariance1));


                }
            }

            if (radioButton16.Checked == true && radioButton19.Checked == true)
            //指挥控制选择的第一个,第二个雷达，分别选择均匀噪声，均匀噪声
            {
                //添加高斯噪声
                //均值

                //计算均值和方差
                for (int i = 0; i < arr_tar.Count; i++)     //得到guassList数组
                {
                    computeMeanVar(command_listone[i], out xMean, out xVariance, out yMean, out yVariance);
                    command_listone[i] = new List<PointD>(Noise.addUniformNoise(command_listone[i].ToArray(),
                        xMean, xVariance, yMean, yVariance));

                    computeMeanVar(command_listtwo[i], out xMean1, out xVariance1, out yMean1, out yVariance1);
                    command_listtwo[i] = new List<PointD>(Noise.addUniformNoise(command_listtwo[i].ToArray(),
                        xMean1, xVariance1, yMean1, yVariance1));
                }
            }
            for(int i=0;i<arr_tar.Count;i++)
            {                
                int k=(command_listone[i].Count<=command_listtwo[i].Count)?command_listone[i].Count:
                    command_listtwo[i].Count;
                for (int j = 0; j < k; j++)
                {                 
                    command_listmix[i].Add((command_listone[i][j] + command_listtwo[i][j]) / 2);
                }
            }         
        }
        /// <summary>
        /// 轨迹融合按钮响应事件
        /// </summary>      
        private void mixtrailButton_Click(object sender, EventArgs e)
        {            
            if (hasChoosedRadar == 2)
            {
                if (this.dopplercheckBox.CheckState == CheckState.Checked)   
                {
                    if (this.multpBasecheckBox.Checked == true)       //选择了多基地雷达和多普勒雷达
                    {
                        this.featurecomboBox1.Items.Clear();
                        this.featurelistView.Items.Clear();
                        prepareforoption12();       //选择第1，2号雷达，即多普勒雷达和多基地雷达，为此填充数据                 
                        this.tabControl1.SelectedIndex = 1;                    
                    }
                    else                  //选择了多普勒雷达和超视距雷达
                    {
                        this.featurecomboBox1.Items.Clear();
                        prepareforoption12();                       
                        this.tabControl1.SelectedIndex = 1;                       
                      
                    }
                }
                else                 //选择了多基地和超视距雷达
                {
                    this.featurecomboBox1.Items.Clear();
                    prepareforoption12();                    
                    this.tabControl1.SelectedIndex = 1;              
                }
            }
            else
            {
                MessageBox.Show("限定选择两个雷达");
            }
        }
        /// <summary>
        /// udp 报文复选框选中与否 响应事件
        /// </summary>     
        private void checkBox_udpSocket_CheckedChanged(object sender, EventArgs e)
        {                     
            if (checkBox_udpSocket.Checked == true)
            {
                for (int i = 0; i < 4; i++)
                    list_trace_update[i].Clear();            
                Thread myThread = new Thread(new ThreadStart(ReceiveData));              
                myThread.Start();
            }
            else
            {            
                constantSpeed();
                constantAcceleration();
                constantSlowDown();
                circleMotion();
                drawtrace_update();
            }
        }
        /// <summary>
        /// 接收数据接口
        /// </summary>
        public void ReceiveData()
        {
            radarsystem.udpSocket.StructDemo struct_df = new radarsystem.udpSocket.StructDemo();            
            int port = 10000;
            IPAddress HostIP = IPAddress.Parse("127.0.0.1");
            IPEndPoint host;
            int trail_type;
            while (PortInUse(port))
            {
                port++;
            }
            while (checkBox_udpSocket.Checked == true)
            {               
                host = new IPEndPoint(HostIP, port);
                UdpClient udpClient = new UdpClient(host);            
                try
                {
                    Byte[] receiveBytes = udpClient.Receive(ref host);                
                    struct_df = (radarsystem.udpSocket.StructDemo)radarsystem.udpSocket.
                        ByteToStruct(receiveBytes, typeof(radarsystem.udpSocket.StructDemo));
                    if (struct_df.scsmhead.unit_flag != 0x76)
                        continue;                 
                    if (!arr_tar.Contains(struct_df.srcTgtTrk.nType.ToString())) 
                         arr_tar.Add(struct_df.srcTgtTrk.nType.ToString()); 
                    Point point = new Point(); 
                    point.X = (int)struct_df.srcTgtTrk.dLat; 
                    point.Y = (int)struct_df.srcTgtTrk.dLon; 
                    list_trace_update[arr_tar.IndexOf(struct_df.srcTgtTrk.nType.ToString())].Add(point);                     
                    drawtrace_update();
                    udpClient.Close();                                               
                    
                }
                catch (Exception e)
                { 
                    MessageBox.Show(e.ToString());
                }

            } 
            

        }
        /// <summary>
        /// 判断端口是否被占用，如果被占用，则使用下一个端口号
        /// </summary>  
        public static bool PortInUse(int port)
        {
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveUdpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }
            return inUse;
        }    
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {            
            System.Diagnostics.Process.GetCurrentProcess().Kill(); 
        }     
    }
}

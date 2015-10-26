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
//　　System.Text;
namespace radarsystem
{
    public partial class Form1 : Form
    {
        //多目标轨迹
        //public struct trace
        //{
        //    public long tar_ID;
        //    public long track_ID;
        //    public Point trail_Point;
        //    public long time;
        //};
        //List<Point>[] list_trace = new List<Point>[50];

        List<Point>[] list_trace_update = new List<Point>[20];  //剧情设置有变化，运动类型包括匀加速，匀速，匀减速，
        //圆周运动等典型运动，目标最多20个，_update指的是10月20号以后的版本
     //   List<PointD>[] list = new List<PointD>[50];

        //List<PointD>[] list_detect_distance = new List<PointD>[50];  //list数组 元素数据类型为PointD，最后用于存储判断了雷达扫描距离的 数据点，仅仅是为了
        //计算精度要求，

        List<PointD>[] list_detect_distance_update = new List<PointD>[20];//波形图上画真实轨迹来源

        //List<PointD>[] list_detect_distance_final = new List<PointD>[50];   //最终用这个数组,用这个数组添加噪声，画波形图上噪音轨迹，以及特征分析

        List<PointD>[] list_detect_distance_final_update = new List<PointD>[20];//波形图上画噪声轨迹来源 
        //List<PointD> list = new List<PointD>();
        //List<Point> list_trace = new List<Point>();
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
        //Thread t2;
        //Thread t1;
     //用pictureBox4 的左上角坐标表示雷达的中心点坐标
   
        //在指挥控制中确定已经选择了雷达的个数
        int hasChoosedRadar = 0;
      
        public Form1()
        {
            InitializeComponent();
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

            ArrayList ToolList = new ArrayList();
            ToolList.Add(MapXLib.ToolConstants.miZoomInTool);
            ToolList.Add(MapXLib.ToolConstants.miZoomOutTool);
            ToolList.Add(MapXLib.ToolConstants.miPanTool);
            ToolList.Add(MapXLib.ToolConstants.miCenterTool);
            ToolList.Add(MapXLib.ToolConstants.miLabelTool);

            comboBox_ToolList.DataSource = ToolList;
            //CMapXFeature FtA
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //连接数据库
            long id, tar_ID;
            int index;
            string conStr = string.Format(@"Provider=Microsoft.Jet.OLEDB.4.0;
                            Data source=" + Application.StartupPath + "\\database\\whut\\RecognitionAid.mdb");

            DataSet ds = dbInterface.query(conStr, "select * from TargetTrailPoints", "目标轨迹");

            //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)//获取目标个数
            //{
            //    trace s1 = new trace();
            //    s1.tar_ID = Convert.ToInt64(ds.Tables[0].Rows[i]["TGT_ID"]);
            //    id = s1.tar_ID;
            //    if (!arr_tar.Contains(id))
            //        arr_tar.Add(id);
            //}
            //for (int i = 0; i < arr_tar.Count; i++)//申请list和list_trace空间
            //{
            //    list_trace[i] = new List<Point>();
            //    list_detect_distance[i] = new List<PointD>();
            //    list_detect_distance_final[i] = new List<PointD>();
            //    color[i] = System.Drawing.Color.FromArgb((220 * i) % 255, (20 * i) % 255, (150 * i) % 255);
            //    guassianList[i] = new List<PointD>();
            //    poissonList[i] = new List<PointD>();
            //    uniformList[i] = new List<PointD>();
            //    guassianList_final[i] = new List<PointD>();
            //    poissonList_final[i] = new List<PointD>();
            //    uniformList_final[i] = new List<PointD>();
                
            //}

            for (int i = 0; i < 20;i++ )
            {
                list_trace_update[i] = new List<Point>();
                list_detect_distance_update[i] = new List<PointD>();
                list_detect_distance_final_update[i] = new List<PointD>();
                color[i] = System.Drawing.Color.FromArgb((220 * i) % 255, (20 * i) % 255, (150 * i) % 255);
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
                //   for (int k = 0; k < arr_tar.Count; k++)
                //      Console.WriteLine(arr_tar[k]);

                //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)          //循环取出ds.table中的值
                //{
                //    //trace s = new trace();
                //    Point p = new Point();
                //    tar_ID = Convert.ToInt64(ds.Tables[0].Rows[i]["TGT_ID"]);
                //    //id = s.tar_ID;
                //    //s.track_ID = Convert.ToInt64(ds.Tables[0].Rows[i]["TrailID"]);
                //    p.X = Convert.ToInt32(ds.Tables[0].Rows[i]["X"]);
                //    p.Y = Convert.ToInt32(ds.Tables[0].Rows[i]["Y"]);
                //    //s.trail_Point = p;
                //    //s.time = Convert.ToInt64(ds.Tables[0].Rows[i]["moveTime"]);
                //    index = arr_tar.IndexOf(tar_ID);
                //    list_trace[index].Add(p);
                //}
                //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)          //循环取出ds.table中的值
                //{
                //    PointD p = new PointD();
                //    tar_ID = Convert.ToInt64(ds.Tables[0].Rows[i]["TGT_ID"]);
                //    p.X = Convert.ToDouble(ds.Tables[0].Rows[i]["X"]);
                //    p.Y = Convert.ToDouble(ds.Tables[0].Rows[i]["Y"]);
                //    index = arr_tar.IndexOf(tar_ID);
                //    list_detect_distance[index].Add(p);
                //}
                //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)          //循环取出ds.table中的值
                //{

                //    PointD s = new PointD();                  // 实例化Point对象
                //    s.X= Convert.ToDouble(ds.Tables[0].Rows[i]["X"]);
                //    s.Y = Convert.ToDouble(ds.Tables[0].Rows[i]["Y"]);            

                //    list.Add(s);    // 将取出的对象保存在LIST中  以上是获得值。


                //}
                //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)          //循环取出ds.table中的值
                //{

                //    Point s = new Point();                  // 实例化Point对象
                //    s.X = Convert.ToInt32(ds.Tables[0].Rows[i]["X"]);  //X，Y看做是经度纬度
                //    s.Y = Convert.ToInt32(ds.Tables[0].Rows[i]["Y"]);

                //    list_trace.Add(s);    // 将取出的对象保存在LIST中  以上是获得值。


                //}
                //foreach (Point p in list_trace)
                //{
                //    Console.WriteLine(p.X);

                //    Console.WriteLine(p.Y);
                //}
                screenpoint_pic4 = PointToScreen(pictureBox4.Location);
            Console.WriteLine(screenpoint_pic4.X);
            Console.WriteLine(screenpoint_pic4.Y);
           
        }
        //private void drawtrace()
        //{
        //    System.Threading.Tasks.Parallel.For(0, arr_tar.Count, i =>
       //         {
        //            TestMethod(i);
        //        });
            //for (int i = 0; i <arr_tar.Count; i++)
            //{
            //    //QueueUserWorkItem()方法：将工作任务排入线程池。
            //    ThreadPool.QueueUserWorkItem(new WaitCallback(TestMethod), i);
            //    // TestMethod 表示要执行的方法(与WaitCallback委托的声明必须一致)。
            //    // i   为传递给Fun方法的参数(obj将接受)。
            //}

            //if (!flag_thread1)
            //{             
            //    t1 = new Thread(new ThreadStart(TestMethod));
            //    t1.IsBackground = true;
            //    t1.Start();
            //    flag_thread1 = true;
            //}
            //else
            //{

            //    t1.Abort();
            //    t1 = new Thread(new ThreadStart(TestMethod));
            //    t1.IsBackground = true;
            //    t1.Start();
            //}
            //   t2.Start();

           
        //}
        //public  void TestMethod(object obj)
        //{
        //    Graphics g;
        //    int flag = (int)obj;
        //    g =axMap1.CreateGraphics();
        //    Pen p = new Pen(color[flag], 2);         
           
        //    Point one, two;
        //    for (int i = 0; i < list_trace[flag].Count-1; i++)
        //    {
        //        one = list_trace[flag][i];
        //        two = list_trace[flag][i + 1];
        //        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        //        g.DrawLine(p, one, two);
        //        System.Threading.Thread.Sleep(200);
        //    } 
        //    g.Dispose();
        //}
   //     private void draw_monitor_trace(List<PointD> points)
        private void draw_monitor_trace()
        {
           // thread2(points);

            for (int i = 0; i < 4; i++)
            {
                //QueueUserWorkItem()方法：将工作任务排入线程池。
                ThreadPool.QueueUserWorkItem(new WaitCallback(thread2), i);
                // Thread2 表示要执行的方法(与WaitCallback委托的声明必须一致)。
                // i   为传递给Fun方法的参数(obj将接受)。
            }
            //if (!flag_thread2) 
            //{            
            //    t2 = new Thread(new ParameterizedThreadStart(thread2));
            //    t2.IsBackground = true;
            //    t2.Start(points);
            //    flag_thread2 = true;
            //}
            //else
            //{
            //    t2.Abort();
            //    t2 = new Thread(new ParameterizedThreadStart(thread2));
            //    t2.IsBackground = true;
            //    t2.Start(points);
            //}
            //t2.Start();
        }
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

            //  for (int i = 0; i < list_trace[flag].Count-1; i++)
            //{
            //    one = list_trace[flag][i];
            //    two = list_trace[flag][i + 1];
            if (radioButton7.Checked == true)       //高斯噪声
            {
                for (int i = 0; i < guassianList_final[flag].Count ; i++)
                {
                    //double类型的坐标转换成int
                    list_trace.Add(new Point((int)guassianList_final[flag][i].X, (int)guassianList_final[flag][i].Y));
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
        //    MessageBox.Show("ja");
             if (radioButton14.Checked==true)       //指挥控制单独处理
             {
                 this.tabControl1.SelectedIndex = 1;
                 
              //   MessageBox.Show("ha");
                for (int i = 0; i < command_listmix[flag].Count; i++)
                {
                    //double类型的坐标转换成int
                //    MessageBox.Show(i.ToString());
                    list_trace.Add(new Point((int)command_listmix[flag][i].X, (int)command_listmix[flag][i].Y));
                    //g.DrawString();
                }
            }
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
            if (list_trace.Count == 1)
            {
                one.X = list_trace[0].X - pictureBox4.Left+cir_Point.X;
                one.Y = list_trace[0].Y - pictureBox4.Left + cir_Point.Y;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                  one.Y - 3, 6, 6));//画实心椭圆
            }

            
        //    double x1=116.41667;
        //    double y1 =39.91667;   //beijing经纬度
        //    double x2=114.31667;
        //    double y2 = 30.51667;   //武汉经纬度
        //    double dis=axMap1.Distance(x1,y1,x2,y2)*2;
        ////    textBox_longitude.Text = dis.ToString();
        }


        private void draw_monitor_trace_realtrace()         //专门画雷达波形图上真实轨迹的，，类似于地图上的轨迹用线程testmethod
        {
            for (int i = 0; i < arr_tar.Count; i++)
            {
                //QueueUserWorkItem()方法：将工作任务排入线程池。
                ThreadPool.QueueUserWorkItem(new WaitCallback(thread3), i);
                // Thread2 表示要执行的方法(与WaitCallback委托的声明必须一致)。
                // i   为传递给Fun方法的参数(obj将接受)。
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

            for (int i = 0; i < list_trace.Count - 1; i++)
            {
                point = list_trace[i];
                point_diff = point;
                point_diff.X = point.X - pictureBox4.Left;
                point_diff.Y = point.Y - pictureBox4.Top;
                //distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                //if (distance2 - distance1 > 0)
                //    continue;
                
                SolidBrush myBrush = new SolidBrush(color[flag]);//画刷
          //      g.FillEllipse(myBrush, new Rectangle(cir_Point.X + point_diff.X - 3, cir_Point.Y + point_diff.Y - 3, 3, 3));//画实心椭圆
                //    g.DrawLine(new Pen(Color.Red), point_diff.X, point_diff.Y, point_diff.X, point_diff.Y);
                //    g.DrawLine(new Pen(Color.Red), 200, 200,210, 210);
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
        //
        private void button_goback_Click(object sender, EventArgs e)
        {
           // label_sel_radartype.Text = "雷达类型选择";
            //checkedListBox_radartype.Show();
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
            this.dopplercheckBox.Visible = false;
            this.multpBasecheckBox.Visible = false;
            this.bvrcheckBox.Visible = false;

            this.groupBox4.Visible = false;
            this.groupBox5.Visible = false;
            this.groupBox6.Visible = false;
            this.mixtrailButton.Visible = false;
            
        }
     

        private void btn_Finish_Click(object sender, EventArgs e)
        {
            if (strCollected == string.Empty)
                MessageBox.Show("您未添加任何噪声！");
            else MessageBox.Show("您选择添加了" + strCollected, "提示");
        }

       
        //特性分析中下拉框状态改变响应函数
        private void featurecomboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            //int showIndex = featurecomboBox1.SelectedIndex;
            string selectedText = featurecomboBox1.SelectedItem.ToString();
            int selectedId =(int) Convert.ToInt64(selectedText);
            int index = arr_tar.IndexOf(selectedId);
            //int index =(int) selectedId;
            FeatureModel feature = new FeatureModel();
            Dictionary<String, double> featDicX;
            Dictionary<String, double> featDicY; 

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

                fftList = feature.getFrequentFFTFeature(uniformList_final[index]);
                ifftList = feature.getFrequentIFFTFeature(uniformList_final[index]);
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
                //ListViewItem listItem = new ListViewItem();
                //listItem.SubItems.Add(""+featDic[featName[i]]);
                featurelistView.Items[i+1].SubItems.Add("" + featDicX[featName[i]]);
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
            else if (scene == Scene.PAS_SONAR)
            {
                featurelistView.Items.Add("探测距离");
                featurelistView.Items[++i].SubItems.Add("<20km");
                featurelistView.Items[i].SubItems.Add("<20km");

                featurelistView.Items.Add("方位");
                featurelistView.Items[++i].SubItems.Add("0~2*pi");
                featurelistView.Items[i].SubItems.Add("0~2*pi");
            }
            else if (scene == Scene.ELEC_VS)
            {
                //暂时在程序中写死，应该从文本框中获得值！！！
                featurelistView.Items.Add("探测距离");
                featurelistView.Items[++i].SubItems.Add("220km");
                featurelistView.Items[i].SubItems.Add("220km");

                featurelistView.Items.Add("载频");
                featurelistView.Items[++i].SubItems.Add("100GHZ");
                featurelistView.Items[i].SubItems.Add("100GHZ");

                featurelistView.Items.Add("重频");
                featurelistView.Items[++i].SubItems.Add("50GHZ");
                featurelistView.Items[i].SubItems.Add("50GHZ");

                featurelistView.Items.Add("脉宽");
                featurelistView.Items[++i].SubItems.Add("20us");
                featurelistView.Items[i].SubItems.Add("20us");

                featurelistView.Items.Add("脉幅");
                featurelistView.Items[++i].SubItems.Add("50");
                featurelistView.Items[i].SubItems.Add("50");

                featurelistView.Items.Add("天线扫描周期");
                featurelistView.Items[++i].SubItems.Add("2.50");
                featurelistView.Items[i].SubItems.Add("2.50");

                featurelistView.Items.Add("载频捷变量");
                featurelistView.Items[++i].SubItems.Add("0.5");
                featurelistView.Items[i].SubItems.Add("0.5");

                featurelistView.Items.Add("重频抖变量");
                featurelistView.Items[++i].SubItems.Add("±1%~±20%");
                featurelistView.Items[i].SubItems.Add("±1%~±20%");
            }

            featurelistView.View = System.Windows.Forms.View.Details;
            featurelistView.GridLines = true;
            featurelistView.EndUpdate();

            //画出频率分析轨迹
   
            List<Point> list_trace = new List<Point>();
            double distance1, distance2;
            distance1 = 7 * panel1.Width / 20;
            Graphics g;
            SolidBrush myBrush = new SolidBrush(color[index]);//画刷
            Pen p = new Pen(color[index], 2);
            g = panel1.CreateGraphics();
            Point point;
            Point point_diff;
            Point cir_Point = new Point(0, 0);
            Point one = new Point(0, 0);
            Point two = new Point(0, 0);
            cir_Point.X = panel1.Width / 10 * 5;
            cir_Point.Y = panel1.Height / 10 * 5;

            //傅立叶
            for (i = 0; i < fftList.Count - 1; i++)
            {
                point = fftList[i];
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
                two.X = fftList[i + 1].X - pictureBox4.Left + cir_Point.X;
                two.Y = fftList[i + 1].Y - pictureBox4.Top + cir_Point.Y;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                   one.Y - 3, 6, 6));//画实心椭圆
                g.FillEllipse(myBrush, new Rectangle(two.X - 3,
                  two.Y - 3, 6, 6));//画实心椭圆

                g.DrawLine(p, one, two);
                System.Threading.Thread.Sleep(200);
            }

            if (fftList.Count == 1)
            {
                one.X = fftList[0].X - pictureBox4.Left + cir_Point.X;
                one.Y = fftList[0].Y - pictureBox4.Left + cir_Point.Y;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                  one.Y - 3, 6, 6));//画实心椭圆
            }
            //反傅立叶
            for (i = 0; i < ifftList.Count - 1; i++)
            {
                point = ifftList[i];
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
                two.X = ifftList[i + 1].X - pictureBox4.Left + cir_Point.X;
                two.Y = ifftList[i + 1].Y - pictureBox4.Top + cir_Point.Y;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                   one.Y - 3, 6, 6));//画实心椭圆
                g.FillEllipse(myBrush, new Rectangle(two.X - 3,
                  two.Y - 3, 6, 6));//画实心椭圆

                g.DrawLine(p, one, two);
                System.Threading.Thread.Sleep(200);
            }

            if (ifftList.Count == 1)
            {
                one.X = fftList[0].X - pictureBox4.Left + cir_Point.X;
                one.Y = fftList[0].Y - pictureBox4.Left + cir_Point.Y;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                  one.Y - 3, 6, 6));//画实心椭圆
            }
          
        }

        //画特性分析中间面板的坐标和圆
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

            //如果添加了噪音，有轨迹，就在波形图右上角添加轨迹标识
            if (noiseFlag == NoiseEnum.GUASSIAN)
            {
                for (int i = 0; i < arr_tar.Count; i++)
                {
                    if (guassianList_final[i].Count != 0)
                    {
                        Pen p = new Pen(color[i], 1 / 2);

                        //画线
                        g.DrawLine(p, 430, 24 * (i + 1), 440, 24 * (i + 1));
                        //写轨迹标识
                        g.DrawString(arr_tar[i].ToString(),new Font(FontFamily.GenericMonospace,10f),Brushes.Black,new PointF(443,22*(i+1)));
                        
                    }
                }
            }
            else if (noiseFlag == NoiseEnum.POISSON)
            {
                for (int i = 0; i < arr_tar.Count; i++)
                {
                    if (poissonList_final[i].Count != 0)
                    {
                        Pen p = new Pen(color[i], 1 / 2);

                        //画线
                        g.DrawLine(p, 430, 24 * (i + 1), 440, 24 * (i + 1));
                        //写轨迹标识
                        g.DrawString(arr_tar[i].ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(443, 22 * (i + 1)));

                    }
                }
            }
            else if (noiseFlag == NoiseEnum.UNIFORM)
            {
                for (int i = 0; i < arr_tar.Count; i++)
                {
                    if (uniformList_final[i].Count != 0)
                    {
                        Pen p = new Pen(color[i], 1 / 2);

                        //画线
                        g.DrawLine(p, 430, 24 * (i + 1), 440, 24 * (i + 1));
                        //写轨迹标识
                        g.DrawString(arr_tar[i].ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(443, 22 * (i + 1)));

                    }
                }
            }
           
        }
     

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            using (Graphics g = panel2.CreateGraphics())
            {
                Pen pen = new Pen(Color.Black, 1 / 2);
                for (int j = 1; j < 5; j++)
                {
                    g.DrawEllipse(pen, 25 - (j - 1) * 8, 25 - (j - 1) * 8, j * 16, j * 16);
                }
            }
        }

        //private void OnDargDrop(object sender, DragEventArgs e) //拖动雷达时候产生该事件
        //{
        //   // pictureBox4.
        //    MessageBox.Show("ga");
        //    screenpoint_pic4 = PointToScreen(pictureBox4.Location);
        //    Console.WriteLine(screenpoint_pic4.X);

        //}  
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;  //可以拖动
            currentX = e.X;
            currentY = e.Y;
        }

        private void drawtrace_update()     //
        {
           
            for (int i = 0; i < 3; i++)    //直接生成4个线程，匀加，匀减，匀速，圆周运动
            {
                //QueueUserWorkItem()方法：将工作任务排入线程池。
                ThreadPool.QueueUserWorkItem(new WaitCallback(thread_drawtrace), i);
                // TestMethod 表示要执行的方法(与WaitCallback委托的声明必须一致)。
                // i   为传递给Fun方法的参数(obj将接受)。
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
        private void constantSpeed()    //匀速运动类型，起点为100，30，方向角为顺时针0 rad，最好多生成一些点
        {
            list_trace_update[0].Clear();
            arr_tar.Clear();
            Point p=new Point();
            p.X=100;        
            p.Y=30;    //匀速运动起点,经纬度 各为100，30
            list_trace_update[0].Add(p);
            //list_detect_distance_update[0].Add(new PointD ((double)p.X,(double)p.Y));
            arr_tar.Add(0);
            for (int i = 1; i < 30; i++)
            {
                Point p1 = new Point();
                p1.X += p.X + i * 20;
                p1.Y = p.Y;
               
             //   double MapX = p1.X, mapY = p1.Y;  //精度 ，纬度
             //   float screenX = 0, screenY = 0; //屏幕坐标
            //    double 
            //    axMap1.ConvertCoord(ref screenX, ref screenY, ref MapX, ref mapY,
            //                       MapXLib.ConversionConstants.miMapToScreen);  //已知经纬度 转换为屏幕坐标
            //    p1.X = (int)screenX;
            //    p1.Y = (int)screenY;
                list_trace_update[0].Add(p1);

            }

        //    drawConstantSpeed(0);

        }
        private void constantAcceleration()     //匀加速运动，保存到list_trace_update[1],方向角为顺时针x度
        {
            list_trace_update[1].Clear();
            Point p=new Point();
            p.X=120;        
            p.Y=50;    //匀加速运动起点,经纬度 各为120，50
            int a=4;  //加速度为12
            list_trace_update[1].Add(p);
            //list_detect_distance_update[1].Add(new PointD((double)p.X, (double)p.Y));
            arr_tar.Add(1);
            for (int i = 1; i < 30; i++)
            {
                Point p1 = new Point();
                p1.X = p.X +(int)(2*i*i*0.9);   //cos（x）=0.9,x=?
                p1.Y = p.Y+(int)(2*i*i*0.4);     //sin（x）
                list_trace_update[1].Add(p1);

            }

        }
        private void constantSlowDown()     //匀减速运动，保存到list_trace_update[2],方向角为顺时针53度
        {
            list_trace_update[2].Clear();
            Point p = new Point();
            p.X = 100;
            p.Y = 70;    //匀减速运动起点,经纬度 各为120，70
            int a = 4;  //加速度为4
            int v0 = 50;
            int vt = v0;
            list_trace_update[2].Add(p);
            //list_detect_distance_update[2].Add(new PointD((double)p.X, (double)p.Y));
            arr_tar.Add(2);
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

        private void circleMotion()         //圆周运动，周期为PI，保存到list_trace_update[3]
        {
            list_trace_update[3].Clear();
            Point p = new Point();
            p.X = 200;
            p.Y = 200;    //圆周运动起点,经纬度 各为200，200
           
            int T=20;    //周期为20
            int r = 50;
            double pi=3.1415;
            //list_trace_update[3].Add(p);
            //list_detect_distance_update[3].Add(new PointD((double)p.X, (double)p.Y));
            arr_tar.Add(3);
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
            //for (int i = 1; i < 20; i++)
            //{
            //    Point p1 = new Point();
            //  //  p1.X = r*(int)Math.Sin(2 * pi / T * i);
            //  //  p1.Y = r*(int)Math.Cos(2 * pi / T * i);
            //    p1.X = (int)(r * 0.5 + p.X);
            //    p1.Y = (int)(r * 0.5 + p.Y);
            //    list_trace_update[3].Add(p1);
            //}

        }

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
                    //point_diff.X = System.Math.Abs(point.X - pictureBox4.Left);
                    //point_diff.Y = System.Math.Abs(point.Y - pictureBox4.Top);
                    point_diff.X = x;
                    point_diff.Y = y;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {
                        // list_trace.
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        list_detect_distance_update[i].Add(point_save);
                        // continue;
                    }
                    else
                    {
                        continue;
                    }
                }

            }
    //        for (int k = 0; k < 4; k++)
    //            Console.WriteLine(list_detect_distance_final_update[k].Count);
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
          //  MessageBox.Show("ga");
            if (isDragging)
            {
                pictureBox4.Top = pictureBox4.Top + (e.Y - currentY);
                pictureBox4.Left = pictureBox4.Left + (e.X - currentX);
              //  System.Threading.Thread.Sleep(1000);
            }
            
            isDragging = false;
            
           // drawtrace();
            if (checkBox_udpSocket.Checked == false)
                circleMotion();
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

        private void Feature_SelectedIndexChanged(object sender, EventArgs e)   //这段代码是有冗余的，
        {
            //flag_thread2 = 1;
            //Control ctrl=tabControl1.GetControl(2);
            if (tabControl1.SelectedIndex == 1)  
            {
                
                if (noiseFlag == NoiseEnum.GUASSIAN)
                {
                    //显示添加高斯噪音的轨迹
                //    System.Threading.Tasks.Parallel.For(0, arr_tar.Count, i =>
                //        {
                         //  draw_monitor_trace(guassianList[i]);
                      draw_monitor_trace();
                      for (int i = 0; i < arr_tar.Count; i++)
                      {
                          if (guassianList_final[i].Count != 0)
                              if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)  //去重
                                  this.featurecomboBox1.Items.Add("" + arr_tar[i]);
                          //if (guassianList_final[i].Count != 0)
                          //    if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)  //去重
                          //        this.featurecomboBox1.Items.Add("" + arr_tar[i]);
                      }
                 //       });
                }
                else if (noiseFlag == NoiseEnum.POISSON)
                {
                    //显示添加泊松噪音的轨迹
                    //System.Threading.Tasks.Parallel.For(0, arr_tar.Count, i =>
                    //{
                    //    //draw_monitor_trace(poissonList[i]);

                    //});
                    draw_monitor_trace();
                    for (int i = 0; i < arr_tar.Count; i++)
                    {
                        if (poissonList_final[i].Count != 0)
                            if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)
                                this.featurecomboBox1.Items.Add("" + arr_tar[i]);
                        //if (poissonList_final[i].Count != 0)
                        //    if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)
                        //        this.featurecomboBox1.Items.Add("" + arr_tar[i]);
                    }
                }
                else if (noiseFlag == NoiseEnum.UNIFORM)
                {
                    //System.Threading.Tasks.Parallel.For(0, arr_tar.Count, i =>
                    //{
                    //    //draw_monitor_trace(uniformList[i]);
                    //});    
                    draw_monitor_trace();
                    for (int i = 0; i < arr_tar.Count; i++)
                    {
                        if (uniformList_final[i].Count != 0)
                            if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)
                                this.featurecomboBox1.Items.Add("" + arr_tar[i]);
                        //if ( [i].Count != 0)
                        //    if (this.featurecomboBox1.FindString(arr_tar[i].ToString()) == -1)
                        //        this.featurecomboBox1.Items.Add("" + arr_tar[i]);
                    }
                }
                else
                {
                    MessageBox.Show("未添加任何噪声，请先建模！");
                }
            }
                //draw_monitor_trace();
            
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //autoForm.controlAutoSize(this);
        }        
        /**
         *  显示特性分析中X坐标轴的刻度
         **/
        private void Xpanel_Paint(object sender, PaintEventArgs e)
        {
            
            Graphics g = Xpanel.CreateGraphics();
            double start = -1;
            int sLoc = 0;
            int addition = 40;
            for (int i = 0; i < 11; i++)
            {
                g.DrawString((start + 0.2 * i).ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(sLoc + addition * i, 0));
            }
        }

        /**
         * 显示特性分析中Y轴坐标刻度
         **/

        private void Ypanel_Paint(object sender, PaintEventArgs e)
        {
            
            Graphics g = Ypanel.CreateGraphics();
            double start = 1;
            int sLoc = 0;
            int addition = 40;
            for (int i = 0; i < 11; i++)
            {
                g.DrawString((start - 0.2 * i).ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(0, sLoc + addition * i));
            }
        }

        private void clearforListDetectDis()        //清空list_detect_distance_final数组
        {
            for (int i = 0; i < arr_tar.Count; i++)
            {
                list_trace_update[i].Clear();
                list_detect_distance_update[i].Clear();
            }
        }

        private void clearfornoiseListfinal()        //清空guassList_final,poissionList_final,uniformList_final数组
        {
            for (int i = 0; i < arr_tar.Count; i++)
            {
                list_detect_distance_final_update[i].Clear();
                guassianList[i].Clear();
                poissonList[i].Clear();
                uniformList[i].Clear();
                guassianList_final[i].Clear();
                poissonList_final[i].Clear();
                uniformList_final[i].Clear();
            }
               
        }


        private void radioButton1_CheckedChanged(object sender, EventArgs e)  //第一组groupbox1中的radiobutton,都对应了这个事件
        {
            clearforListDetectDis();  //切换到不同雷达，清空list_detect_distance_final 数组
            clearfornoiseListfinal(); //切换到不同雷达， 清空guassList,guassList_final,
            arr_tar.Clear();
            if (checkBox_udpSocket.Checked == false)
            {
                constantSpeed();
                constantAcceleration();
                constantSlowDown();
                circleMotion();
                // draw_monitor_trace();
                drawtrace_update();
               
            }
        //    flag_command = false;
            //poissionList_final,uniformList_final数组
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
            //    Graphics g = axMap1.CreateGraphics();
            if (radioButton1.Checked == true)  //选中了第一个单选按钮，即选择了多普勒雷达
            {
                
              //  button_goback.Visible = true;
                //pictureBox4.Image = Bitmap.FromFile(AppDomain.CurrentDomain.BaseDirectory + "\..\\..\\..\\radarsystem\\Resources\\多普勒雷达.jpg");
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
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.radarpic;  //还没替换为多基地雷达图标
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "多基地雷达";
                groupBox1.Visible = false;

                //设置当前场景为多基地
                scene = Scene.MUTLIBASE;
                //清空combobox的值
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY;  
              //  drawtrace();
                readTxt();

            }
            if (radioButton3.Checked == true)  //选中了第3个单选按钮，即选择了超视距雷达
            {                            
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.HVR;  //还没替换为多基地雷达图标
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "超视距雷达";
                groupBox1.Visible = false;
                //设置当前场景为超视距雷达
                scene = Scene.BVR;
                //清空combobox的值
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY; 
               // drawtrace();
                readTxt();

            }
            if (radioButton4.Checked == true)  //选中了第4个单选按钮，即选择了声呐
            {

                //  button_goback.Visible = true;
                //pictureBox4.Image = Bitmap.FromFile(AppDomain.CurrentDomain.BaseDirectory + "\..\\..\\..\\radarsystem\\Resources\\多普勒雷达.jpg");
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.SONAR;  //还没替换为多基地雷达图标
                pictureBox4.Visible = true;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "声呐主动";
                groupBox1.Visible = false;
                scene = Scene.ACT_SONAR;
                //清空combobox的值
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY; 
               // drawtrace();
            //    readTxt();

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
                //清空combobox的值
                this.featurecomboBox1.Items.Clear();
                pictureBox4.Left = (int)screenX;
                pictureBox4.Top = (int)screenY; 

               // drawtrace();
            //    readTxt();

            }
            if (radioButton6.Checked == true)  //选中了第6个单选按钮，即选择了指挥控制
            {
                //int first=0,second=0,third=0;
                //pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.radarpic; 
                //还没替换为指挥控制雷达图标，2部雷达？
               // pictureBox4.Visible = true;
                //探测建模
               // buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "指挥控制";
                groupBox1.Visible = false;
                scene = Scene.COMMAND;
                //清空combobox的值
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
              //  drawtrace();
           //     readTxt();

            }
            if (radioButton13.Checked == true)  //选中了第7个单选按钮，即选择了声呐（被动）
            {
                pictureBox4.BackgroundImage = global::radarsystem.Properties.Resources.SONAR;
                buttonDectecModeling.Visible = true;
                label_sel_radartype.Visible = true;
                label_sel_radartype.Text = "声呐被动";
                groupBox1.Visible = false;
                //场景
                scene = Scene.PAS_SONAR;
                //清空combobox的值
                this.featurecomboBox1.Items.Clear();
                // drawtrace();
             //   readTxt();

            }
            button_goback.Visible = true;
        }

        private void read_interface(int line_num)       //读配置文件公共接口
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
            //string str_temp="";
            textBox_doppler.Text = "";
           // textBox_doppler.Text = "检测范围\r\n\r\n距离精度\r\n\r\n目标速度\r\n\r\n速度精度";
          
            int line_num = 0;
            int count = 0;
            if (radioButton1.Checked == true)
            {
                line_num = 0;
                read_interface(line_num);
               // count = 1;
               // if (content_read[line_num * 1] == "多普勒雷达")
               // {           
                    
               // }

            }
            if (radioButton2.Checked == true)
            {
                line_num = 1;
                read_interface(line_num);
            }
            if (radioButton3.Checked == true)
            {
                line_num = 2;
                read_interface(line_num);
            }
              
          
   
        }

        private void OnButtonUpdateConfigClick(object sender, EventArgs e)  //选择文件更新 按钮响应事件
        {         
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = "E:\\";
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
           // }
            
        }

        private void wrTxt()
        {
          //  MessageBox.Show("wr");
            int line_num=0; //行号
            String path = Application.StartupPath + "\\configure.txt";
            
            string[] content = System.IO.File.ReadAllText(path).Split(new char[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            if (radioButton1.Checked == true)   //多普勒雷达被选中
            {
                line_num = 0;  //指定修改哪些行，最后一次整个文件都会更新
                content[line_num* 1 + 1] = "探测距离(km)"+"\t"+textBox_juli.Text;
                content[line_num * 1 + 2] = "载频(GHZ)"+"\t"+textBox_zaipin.Text;
                content[line_num * 1 + 3] = "重频(GHZ)" + "\t" + textBox_chongpin.Text;
                content[line_num * 1 + 4] = "脉宽(us)" + "\t" + textBox_maikuan.Text;
                content[line_num * 1 + 5] = "脉幅" + "\t" + textBox_maifu.Text;
                content[line_num * 1 + 6] = "天线扫描周期" + "\t" + textBox_saomiao.Text;
                content[line_num * 1 + 7] = "载频捷变量" + "\t" + textBox_jiebianliang.Text;
                content[line_num * 1 + 8] = "重频抖动量" + "\t" + textBox_doudongliang.Text;
            }
            //content[1] = "9,f,g,h";
            System.IO.File.WriteAllText(path, string.Join("\r\n", content),
                Encoding.Unicode);

        }
        private void OnButtonDetectModeling(object sender, EventArgs e)  //探测建模按钮响应事件
        {

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
                button_goback.Enabled = false;
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

        public void computeMeanVar(List<PointD> list, out double xMean, out double xVariance, out double yMean, out double yVariance)
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

        private void prepareforListDetectDisFin()      //准备数据，即填充list_detect_distance_final_update[] 数组
        {
            for (int i = 0; i < arr_tar.Count; i++)
                list_detect_distance_final_update[i].Clear();
            //List<Point> list_trace = new List<Point>();
            double distance1, distance2;
            distance1 = 7 * panel1.Width / 20;          
            PointD point = new PointD();
            PointD point_diff = new PointD();      

            for (int i = 0; i < arr_tar.Count; i++)
            {   
                
                for (int j = 0; j < list_detect_distance_update[i].Count; j++)
                {
                    point.X = list_detect_distance_update[i][j].X;
                    point.Y = list_detect_distance_update[i][j].Y;

                    point_diff.X = point.X;
                    point_diff.Y = point.Y;

                    point_diff.X = System.Math.Abs(point.X - pictureBox4.Left);
                    point_diff.Y = System.Math.Abs(point.Y - pictureBox4.Top);
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {
                        // list_trace.
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        list_detect_distance_final_update[i].Add(point_save);
                        // continue;
                    }
                    else
                    {
                        continue;
                    }
                }

            }
            for (int k = 0; k < 4; k++)
                Console.WriteLine(list_detect_distance_final_update[k].Count);
        }

        private void prepareforguassListFinal()     //为guassList_final,poissonList_final,uniformList_final准备数据
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
                        // list_trace.
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        //guassianList[i][j] = point_save;
                        guassianList_final[i].Add(point_save);
                        // continue;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        private void prepareforpoissonListFinal()     //为guassList_final,poissonList_final,uniformList_final准备数据
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
                        // list_trace.
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        //poissonList[i][j] = point_save;
                        poissonList_final[i].Add(point_save);
                        // continue;
                    }
                    else
                    {
                        continue;
                    }
                    
                }
            }
        }

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
                        // list_trace.
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        //uniformList[i][j] = point_save;
                        uniformList_final[i].Add(point_save);
                        // continue;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

        }
        private void OnButtonModelDone(object sender, EventArgs e)
        {
            prepareforListDetectDis();
            prepareforListDetectDisFin();   //准备数据，为数组list_detect_distance_final,注意：每次切换一种雷达时候，
           
            
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
                        computeMeanVar(list_detect_distance_final_update[i], out xMean, out xVariance, out yMean, out yVariance);
                        guassianList[i] = new List<PointD>(Noise.addGuassianNoise(list_detect_distance_final_update[i].ToArray(), 
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
                        poissonList[i] = new List<PointD>(Noise.addPoissonNoise(list_detect_distance_final_update[i].ToArray(),
                            (panel1.Width / 10) * 7, (panel1.Width / 10) * 7));
                        
                     }
                    prepareforpoissonListFinal();
                    
                   
                    button_goback.Enabled = true;
                    if (DialogResult.OK == MessageBox.Show("congratulations! 添加噪声完毕，你选择添加了泊松噪声"))
                    {
                        //MessageBox.Show(""+(panel1.Width / 10) * 7);
                        noiseFlag = NoiseEnum.POISSON;
                        //将当前的页面切换成特性分析
                        this.tabControl1.SelectedIndex = 1;

                    }

                    //button_goback.Enabled = true;
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
                        computeMeanVar(list_detect_distance_final_update[i], out xMean, out xVariance, out yMean, out yVariance);
                        XA = xMean - Math.Pow(3, 1 / 2) * Math.Pow(xVariance, 2);
                        XB = xMean + Math.Pow(3, 1 / 2) * Math.Pow(xVariance, 2);
                        YA = yMean - Math.Pow(3, 1 / 2) * Math.Pow(yVariance, 2);
                        YB = yMean + Math.Pow(3, 1 / 2) * Math.Pow(yVariance, 2);

                        uniformList[i] = new List<PointD>(Noise.addUniformNoise(list_detect_distance_final_update[i].ToArray(),
                            XA, XB, YA, YB));
                       
                        //button_goback.Enabled = true;

                    }
                    prepareforuniformListfinal();
                    button_goback.Enabled = true;
                    if (DialogResult.OK == MessageBox.Show("congratulations! 添加噪声完毕，你选择添加了平均噪声"))
                    {
                        noiseFlag = NoiseEnum.UNIFORM;
                        this.tabControl1.SelectedIndex = 1;
                    }
                    //button_goback.Enabled = true;
                }
                else
                    MessageBox.Show("请选择添加一种噪声");
            
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
            if (this.checkBox1.Checked == true)
            {
                MessageBox.Show("选中真实轨迹");
                //如果真是轨迹选项选中
                //System.Threading.Tasks.Parallel.For(0, arr_tar.Count, i =>
                //{
                //    //draw_monitor_trace(list_detect_distance_final[i]);
                //    draw_monitor_trace();
                //});
                draw_monitor_trace_realtrace();
            }
                

        }

        private void comboBox_ToolList_SelectedIndexChanged(object sender, EventArgs e)
        {
            axMap1.CurrentTool = (MapXLib.ToolConstants)comboBox_ToolList.SelectedItem;
        }

        private void TextChanged(object sender, EventArgs e)
        {
            flag_editchange = true;
        }

        private void UpdateTextToTxt(object sender, EventArgs e)
        {
            wrTxt();
            readTxt();
            MessageBox.Show("配置文件更新完成");
        }

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
        //    Graphics g = axMap1.CreateGraphics();
          
        }

        //在波形图右上角画出线条的标识
        public void draw_trace_id(long id, Color c,int x,int y)
        {
            
        }
        /// <summary>
        /// 如下三个checkbox状态改变函数用来监听选择的雷达类型
        /// 
        /// </summary>
   
        private void dopplercheckBox_CheckedChanged(object sender, EventArgs e)
        {
            //checkbox 状态改变
            if (this.dopplercheckBox.CheckState == CheckState.Checked)
            {
                //MessageBox.Show("lllll");
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
            //    circleMotion();
            // drawtrace();
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
                        // list_trace.
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        command_listone[i].Add(point_save);
                        // continue;
                    }
                    //else
                    //{
                    //    continue;
                    //}

                    else if (distance3 - distance1 <= 0)   //相当于判断雷达的最大扫描范围
                    {
                        // list_trace.
                        PointD point_save = new PointD();
                        point_save.X = point.X;
                        point_save.Y = point.Y;
                        command_listtwo[i].Add(point_save);
                        // continue;
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
                  //  MessageBox.Show(j.ToString());
                    command_listmix[i].Add((command_listone[i][j] + command_listtwo[i][j]) / 2);
                }
            }

           

        }
        private void mixtrailButton_Click(object sender, EventArgs e)
        {
            //点击了轨迹融合按钮，之后
            if (hasChoosedRadar == 2)
            {
                if (this.dopplercheckBox.CheckState == CheckState.Checked)   
                {
                    if (this.multpBasecheckBox.Checked == true)       //选择了多基地雷达和多普勒雷达
                    {
                        prepareforoption12();       //选择第1，2号雷达，即多普勒雷达和多基地雷达，为此填充数据
                     //   MessageBox.Show("12");

                    //    if (radioButton14.Checked == true)
                    //        MessageBox.Show("14");
                    //    flag_command = true;
                        draw_monitor_trace();
                     //   flag_command = false;
                    }
                    else                  //选择了多普勒雷达和超视距雷达
                    {
                        prepareforoption12();
                        draw_monitor_trace();
                       // MessageBox.Show("13");
                    }
                }
                else                 //选择了多基地和超视距雷达
                {
                    prepareforoption12();
                    draw_monitor_trace();
                   // MessageBox.Show("23");
                }
            }
            else
            {
                MessageBox.Show("限定选择两个雷达");
            }
        }

        private void checkBox_udpSocket_CheckedChanged(object sender, EventArgs e)
        {
           // udpSocket udpsocket = new udpSocket();
            if (checkBox_udpSocket.Checked == true)
            {
                for (int i = 0; i < 4; i++)
                    list_trace_update[i].Clear();
                Thread myThread = new Thread(new ThreadStart(ReceiveData));
                //    //将线程设为后台运行   
                // //   myThread.IsBackground = true;
                myThread.Start();
            }
            else
            {
                constantSpeed();
                constantAcceleration();
                constantSlowDown();
                circleMotion();
               // draw_monitor_trace();
                drawtrace_update();
            }
        }

        public void ReceiveData()
        {
            radarsystem.udpSocket.StructDemo struct_df = new radarsystem.udpSocket.StructDemo();
            
            int port = 10000;
            IPAddress HostIP = IPAddress.Parse("127.0.0.1");
            IPEndPoint host;
            while (PortInUse(port))
            {
                port++;
            }
            host = new IPEndPoint(HostIP, port);
            UdpClient udpClient = new UdpClient(host);
            while (true)
            {
                //   IPEndPoint iep = new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[3], 18001);


               
                //      UdpClient.Send("发送的字节", "发送的字节长度", host);  
                //      IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    Byte[] receiveBytes = udpClient.Receive(ref host);
                    //  string receiveData = Encoding.Unicode.GetString(receiveBytes);

                    //    Console.WriteLine("接收到信息：" + receiveData);
                    //Console.WriteLine("接收到信息：" + BitConverter.ToString(receiveBytes));
                    struct_df = (radarsystem.udpSocket.StructDemo)radarsystem.udpSocket.ByteToStruct(receiveBytes, typeof(radarsystem.udpSocket.StructDemo));
                    if (struct_df.scsmhead.unit_flag != 0x76)
                        continue;
                    if (!arr_tar.Contains(struct_df.srcTgtTrk.nType))
                        arr_tar.Add(struct_df.srcTgtTrk.nType);
                    Point point = new Point();
                    point.X = (int)struct_df.srcTgtTrk.dLat;
                    point.Y = (int)struct_df.srcTgtTrk.dLon;
                    list_trace_update[arr_tar.IndexOf(struct_df.srcTgtTrk.nType)].Add(point);
                    drawtrace_update();
                    //Console.WriteLine("从结构体中获得" + receiveBytes[1]);
                    //Console.WriteLine("从结构体中获得" + sf.scsmhead.length);
                    //Console.WriteLine("从结构体中获得" + sf.scsmhead.recv);
                    //Console.WriteLine("从结构体中获得" + sf.srcTgtTrk.dLat);
                    Console.WriteLine("从结构体中获得" + struct_df.srcTgtTrk.dLon);
                    //MessageBox.Show("接收到信息：" + struct_df.srcTgtTrk.dLon);
                    
                }
                catch (Exception e)
                { 
                    MessageBox.Show(e.ToString());
                }

            } 
            udpClient.Close();

        }

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
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace radarsystem
{
    public partial class FrequencyForm : Form
    {
        public FrequencyForm()
        {
            InitializeComponent();
        }

        public void draw_fft_trail(List<Point> fftList,Color c)
        {

            List<Point> fft_trace = new List<Point>();
           
            Graphics g;
            SolidBrush myBrush = new SolidBrush(c);//画刷
            Pen p = new Pen(c, 2);
            g = frequentpanel.CreateGraphics();

            Point one = new Point(0, 0);
            Point two = new Point(0, 0);


            //检测傅立叶和反傅立叶中是否有点不再波形图内
            for (int i = 0; i < fftList.Count;i++ )
            {
                if (fftList[i].X > 251 || fftList[i].X < -251 || fftList[i].Y > 230 || fftList[i].Y < -230)
                    continue;
                fft_trace.Add(fftList[i]);
            }

                //傅立叶
            if (fft_trace.Count == 0)
            {

            }
            else if (fft_trace.Count == 1)
            {
                one.X = fft_trace[0].X + 250;
                one.Y = fft_trace[0].Y + 250;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                    one.Y - 3, 6, 6));//画实心椭圆
            }
            else
            {
                for (int i = 0; i < fft_trace.Count - 1; i++)
                {

                    one.X = fft_trace[i].X + 250;
                    one.Y = fft_trace[i].Y + 250;
                    two.X = fft_trace[i + 1].X + 250;
                    two.Y = fft_trace[i + 1].Y + 250;
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

        public void draw_ifft_trail(List<Point> ifftList, Color c)
        {
            List<Point> ifft_trace = new List<Point>();
            Graphics g;
            SolidBrush myBrush = new SolidBrush(c);//画刷
            Pen p = new Pen(c, 2);
            g = frequentpanel.CreateGraphics();

            Point one = new Point(0, 0);
            Point two = new Point(0, 0);


            for (int i = 0; i < ifftList.Count; i++)
            {
                if (ifftList[i].X > 251 || ifftList[i].X < -251 || ifftList[i].Y > 230 || ifftList[i].Y < -230)
                    continue;

                ifft_trace.Add(ifftList[i]);
            }

            if (ifft_trace.Count == 0)
            {

            }
            else if (ifft_trace.Count == 1)
            {
                one.X = ifft_trace[0].X+250;
                one.Y = ifft_trace[0].Y+250;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                  one.Y - 3, 6, 6));//画实心椭圆
            }
            else
            {
                for (int i = 0; i < ifft_trace.Count - 1; i++)
                {
                    //坐标变换，图中中心变为坐标原点
                    one.X = ifft_trace[i].X + 250;
                    one.Y = ifft_trace[i].Y + 250;

                    two.X = ifft_trace[i + 1].X + 250;
                    two.Y = ifft_trace[i + 1].Y + 250;
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

        private void frequentpanel_Paint(object sender, PaintEventArgs e)
        {
            //创建画板
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Black, 1 / 2);
            //g.DrawLine(pen, 0, 0, 335, 0);

            int factor = frequentpanel.Width / 10;
            for (int i = 0; i < 10; i++)
            {
                //画水平线
                g.DrawLine(pen, 0, i * factor, frequentpanel.Width, i * factor);
                //画竖直线
                g.DrawLine(pen, i * factor, 0, factor * i, frequentpanel.Height);
            }

            //画圆
            for (int j = 1; j < 5; j++)
            {
                g.DrawEllipse(pen, 4 * factor - (j - 1) * factor, 4 * factor - (j - 1) * factor, j * 2 * factor, j * 2 * factor);
                //g.DrawEllipse(panel1.Width/10*4,)
            }

            
        }

        private void xpanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = xpanel.CreateGraphics();
            double start = -1;
            int sLoc = 0;
            int addition = 50;
            for (int i = 0; i < 11; i++)
            {
                g.DrawString((start + 0.2 * i).ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(sLoc + addition * i, 0));
            }
        }

        private void ypanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = ypanel.CreateGraphics();
            double start = 1;
            int sLoc = 0;
            int addition = 50;
            for (int i = 0; i < 11; i++)
            {
                g.DrawString((start - 0.2 * i).ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(0, sLoc + addition * i));
            }
        }
       
    }
}

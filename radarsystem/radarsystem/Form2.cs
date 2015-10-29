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

        public void draw_fft_trail(List<Point> fftList,List<Point> ifftList,Color c)
        {
            //int index = (int)obj;
            List<Point> list_trace = new List<Point>();
            List<Point> fft_trace = new List<Point>();
            List<Point> ifft_trace = new List<Point>();

            double distance1, distance2;
            distance1 = 7 * frequentpanel.Width / 20;
            Graphics g;
            SolidBrush myBrush = new SolidBrush(c);//画刷
            Pen p = new Pen(c, 2);
            g = frequentpanel.CreateGraphics();
            Point point;
            Point point_diff;
            Point cir_Point = new Point(0, 0);
            Point one = new Point(0, 0);
            Point two = new Point(0, 0);
            cir_Point.X = frequentpanel.Width / 10 * 5;
            cir_Point.Y = frequentpanel.Height / 10 * 5;

            //检测傅立叶和反傅立叶中是否有点不再波形图内
            for (int i = 0; i < fftList.Count;i++ )
            {
                if (fftList[i].X > 502 || fftList[i].X < 0 || fftList[i].Y > 460 || fftList[i].Y < 0)
                    continue;
                fft_trace.Add(fftList[i]);
            }

            for (int i = 0; i < ifftList.Count; i++)
            {
                if (ifftList[i].X > 502 || ifftList[i].X < 0 || ifftList[i].Y > 460 || ifftList[i].Y < 0)
                    continue;
                ifft_trace.Add(ifftList[i]);
            }


                //傅立叶
            if (fft_trace.Count == 0)
            {

            }
            else if (fft_trace.Count == 1)
            {
                one.X = fft_trace[0].X;
                one.Y = fft_trace[0].Y;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                    one.Y - 3, 6, 6));//画实心椭圆
            }
            else
            {
                for (int i = 0; i < fft_trace.Count - 1; i++)
                {
                    /*point = fftList[i];
                    point_diff = point;
                    point_diff.X = point.X - pictureBox4.Left;
                    point_diff.Y = point.Y - pictureBox4.Top;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 > 0)
                        continue;
                   
                    one.X = cir_Point.X + point_diff.X;
                    one.Y = cir_Point.Y + point_diff.Y;
                    two.X = fftList[i + 1].X - pictureBox4.Left + cir_Point.X;
                    two.Y = fftList[i + 1].Y - pictureBox4.Top + cir_Point.Y;*/

                    one = fft_trace[i];
                    two = fft_trace[i + 1];
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


                    g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                        one.Y - 3, 6, 6));//画实心椭圆
                    g.FillEllipse(myBrush, new Rectangle(two.X - 3,
                        two.Y - 3, 6, 6));//画实心椭圆

                    g.DrawLine(p, one, two);
                    System.Threading.Thread.Sleep(200);
                }
            }


            one = new Point(0, 0);
            two = new Point(0, 0);

            //反傅立叶
            if(ifft_trace.Count == 0)
            {

            }
            else if (ifft_trace.Count == 1)
            {
                one.X = ifft_trace[0].X;
                one.Y = ifft_trace[0].Y;
                g.FillEllipse(myBrush, new Rectangle(one.X - 3,
                  one.Y - 3, 6, 6));//画实心椭圆
            }
            else
            {
                for (int i = 0; i < ifftList.Count - 1; i++)
                {
                    /*point = ifftList[i];
                    point_diff = point;
                    point_diff.X = point.X - pictureBox4.Left;
                    point_diff.Y = point.Y - pictureBox4.Top;
                    distance2 = Math.Sqrt(point_diff.X * point_diff.X + point_diff.Y * point_diff.Y);
                    if (distance2 - distance1 > 0)
                        continue;*/
                    //        g.FillEllipse(myBrush, new Rectangle(cir_Point.X + point_diff.X - 3, cir_Point.Y + point_diff.Y - 3, 3, 3));//画实心椭圆
                    //    g.DrawLine(new Pen(Color.Red), point_diff.X, point_diff.Y, point_diff.X, point_diff.Y);
                    //    g.DrawLine(new Pen(Color.Red), 200, 200,210, 210);
                    /*one.X = cir_Point.X + point_diff.X;
                    one.Y = cir_Point.Y + point_diff.Y;
                    two.X = ifftList[i + 1].X - pictureBox4.Left + cir_Point.X;
                    two.Y = ifftList[i + 1].Y - pictureBox4.Top + cir_Point.Y;
                     * */
                    one = ifft_trace[i];
                    two = ifft_trace[i + 1];
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
            int addition = 40;
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
            int addition = 40;
            for (int i = 0; i < 11; i++)
            {
                g.DrawString((start - 0.2 * i).ToString(), new Font(FontFamily.GenericMonospace, 10f), Brushes.Black, new PointF(0, sLoc + addition * i));
            }
        }
       
    }
}

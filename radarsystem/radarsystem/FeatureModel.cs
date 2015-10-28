using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace radarsystem
{
    public class FeatureModel
    {
        /**
         * 特性分析处理类，这里面添加特性处理的代码
         * param: list 是运行轨迹的一些点； count 是特性的个数
         * */
        public Dictionary<String, double> getTimeAndSpaceFeatureX(List<PointD> list,int count)
        {
            Dictionary<String, double> featDic = new Dictionary<String, double>();
            //计算时域空域特征分析
            double[] features = new double[count];

            if (list.Count == 0 || list == null)
            {
                featDic["算术平均值"] = 0; featDic["几何平均值"] = 0; featDic["均方根值"] = 0; featDic["方差"] = 0;
                featDic["标准差"] = 0; featDic["波形指标"] = 0; featDic["脉冲指标"] = 0; featDic["方根幅值"] = 0;
                featDic["裕度指标"] = 0; featDic["峭度指标"] = 0; featDic["自相关函数"] = 0; featDic["峰值指标"] = 0;
                featDic["互相关函数"] = 0;
                return featDic;
            }

            PointD[] p1 = new PointD[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                p1[i] = list[i];
            }

            double[] pX = new double[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                pX[i] = p1[i].X;
            }
            //算术平均值,arithmetic mean value
            for (int i = 0; i < list.Count; i++)
            {
                features[0] += p1[i].X;
            }
            features[0] /= list.Count;
            //保留两位小数
            features[0] = Math.Round(features[0], 2);
            featDic.Add("算术平均值", features[0]);

            //几何平均值,geometric mean
            features[1] = 1.0;
            for (int i = 0; i < list.Count; i++)
            {
                if (p1[i].X != 0)
                    features[1] *= p1[i].X;

            }
            features[1] = Math.Pow(features[1], 1 / list.Count);
            features[1] = Math.Round(features[1], 2);
            featDic.Add("几何平均值", features[1]);


            //均方根值,root mean square value
            for (int i = 0; i < list.Count; i++)
            {
                features[2] += Math.Pow(p1[i].X, 2);
            }
            features[2] /= list.Count;
            features[2] = Math.Pow(features[2], 1 / 2);
            features[2] = Math.Round(features[2], 2);
            featDic.Add("均方根值", features[2]);

            //方差, variance
            for (int i = 0; i < list.Count; i++)
            {
                features[3] += Math.Pow(p1[i].X - features[0], 2);
            }
            if (list.Count == 1)
                features[3] = 0;
            else
                features[3] /= list.Count - 1;
            features[3] = Math.Round(features[3], 2);
            featDic.Add("方差", features[3]);

            //标准差,standard deviation
            for (int i = 0; i < list.Count; i++)
            {
                features[4] += Math.Pow(p1[i].X - features[0], 2);
            }
            features[4] /= list.Count;
            features[4] = Math.Pow(features[4], 1 / 2);
            features[4] = Math.Round(features[4], 2);
            featDic.Add("标准差", features[4]);

            //波形指标,waveform indicators
            features[5] = featDic["均方根值"] / Math.Abs(featDic["算术平均值"]);
            features[5] = Math.Round(features[5], 2);
            featDic.Add("波形指标", features[5]);

            //峰值指标,peak index
            features[6] = pX.Max() / featDic["均方根值"];
            features[6] = Math.Round(features[6], 2);
            featDic["峰值指标"] = features[6];

            //脉冲指标,pulse factor
            features[7] = pX.Max() / Math.Abs(featDic["算术平均值"]);
            features[7] = Math.Round(features[7], 2);
            featDic["脉冲指标"] = features[7];

            //方根幅值,root amplitude
            for (int i = 0; i < list.Count; i++)
            {
                features[8] += Math.Pow(Math.Abs(p1[i].X), 1 / 2);
            }
            features[8] = Math.Pow(features[8] / list.Count, 2);
            features[8] = Math.Round(features[8], 2);
            featDic["方根幅值"] = features[8];

            //裕度指标,margin indicator
            features[9] = pX.Max() / featDic["方根幅值"];
            features[9] = Math.Round(features[9], 2);
            featDic["裕度指标"] = features[9];

            //峭度指标,Kurosis amplitude
            for (int i = 0; i < list.Count; i++)
            {
                features[10] += Math.Pow(p1[i].X, 4);
            }
            features[10] /= list.Count;
            features[10] = features[10] / Math.Pow(featDic["均方根值"], 4);
            features[10] = Math.Round(features[10], 2);
            featDic["峭度指标"] = features[10];

            //自相关函数,m=2,autocorrelation
            for (int i = 0; i < (list.Count - 2); i++)
            {
                features[11] += p1[i].X * p1[i + 2].X;
            }
            features[11] = Math.Round(features[11], 2);
            featDic["自相关函数"] = features[11];

            //互相关函数,cross-correlation
            for (int i = 0; i < (list.Count - 2); i++)
            {
                features[12] += p1[i].X * p1[i + 2].Y;
            }
            features[12] = Math.Round(features[12], 2);
            featDic["互相关函数"] = features[12];

            return featDic;
        }

        public Dictionary<String, double> getTimeAndSpaceFeatureY(List<PointD> list, int count)
        {
            Dictionary<String, double> featDic = new Dictionary<String, double>();
            //计算时域空域特征分析
            double[] features = new double[count];

            if (list.Count == 0 || list == null)
            {
                featDic["算术平均值"] = 0; featDic["几何平均值"] = 0; featDic["均方根值"] = 0; featDic["方差"] = 0;
                featDic["标准差"] = 0; featDic["波形指标"] = 0; featDic["脉冲指标"] = 0; featDic["方根幅值"] = 0;
                featDic["裕度指标"] = 0; featDic["峭度指标"] = 0; featDic["自相关函数"] = 0; featDic["峰值指标"] = 0;
                featDic["互相关函数"] = 0;
                return featDic;
            }
            PointD[] p1 = new PointD[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                p1[i] = list[i];
            }

            double[] pY = new double[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                pY[i] = p1[i].Y;
            }
            //算术平均值,arithmetic mean value
            for (int i = 0; i < list.Count; i++)
            {
                features[0] += p1[i].Y;
            }
            features[0] /= list.Count;
            //保留两位小数
            features[0] = Math.Round(features[0], 2);
            featDic.Add("算术平均值", features[0]);

            //几何平均值,geometric mean
            features[1] = 1.0;
            for (int i = 0; i < list.Count; i++)
            {
                if (p1[i].Y != 0)
                    features[1] *= p1[i].Y;

            }
            features[1] = Math.Pow(features[1], 1 / list.Count);
            features[1] = Math.Round(features[1], 2);
            featDic.Add("几何平均值", features[1]);


            //均方根值,root mean square value
            for (int i = 0; i < list.Count; i++)
            {
                features[2] += Math.Pow(p1[i].Y, 2);
            }
            features[2] /= list.Count;
            features[2] = Math.Pow(features[2], 1 / 2);
            features[2] = Math.Round(features[2], 2);
            featDic.Add("均方根值", features[2]);

            //方差, variance
            for (int i = 0; i < list.Count; i++)
            {
                features[3] += Math.Pow(p1[i].Y - features[0], 2);
            }
            if (list.Count == 1)
                features[3] = 0;
            else
                features[3] /= list.Count - 1;
            features[3] = Math.Round(features[3], 2);
            featDic.Add("方差", features[3]);

            //标准差,standard deviation
            for (int i = 0; i < list.Count; i++)
            {
                features[4] += Math.Pow(p1[i].Y - features[0], 2);
            }
            features[4] /= list.Count;
            features[4] = Math.Pow(features[4], 1 / 2);
            features[4] = Math.Round(features[4], 2);
            featDic.Add("标准差", features[4]);

            //波形指标,waveform indicators
            features[5] = featDic["均方根值"] / Math.Abs(featDic["算术平均值"]);
            features[5] = Math.Round(features[5], 2);
            featDic.Add("波形指标", features[5]);

            //峰值指标,peak index
            features[6] = pY.Max() / featDic["均方根值"];
            features[6] = Math.Round(features[6], 2);
            featDic["峰值指标"] = features[6];

            //脉冲指标,pulse factor
            features[7] = pY.Max() / Math.Abs(featDic["算术平均值"]);
            features[7] = Math.Round(features[7], 2);
            featDic["脉冲指标"] = features[7];

            //方根幅值,root amplitude
            for (int i = 0; i < list.Count; i++)
            {
                features[8] += Math.Pow(Math.Abs(p1[i].Y), 1 / 2);
            }
            features[8] = Math.Pow(features[8] / list.Count, 2);
            features[8] = Math.Round(features[8], 2);
            featDic["方根幅值"] = features[8];

            //裕度指标,margin indicator
            features[9] = pY.Max() / featDic["方根幅值"];
            features[9] = Math.Round(features[9], 2);
            featDic["裕度指标"] = features[9];

            //峭度指标,Kurosis amplitude
            for (int i = 0; i < list.Count; i++)
            {
                features[10] += Math.Pow(p1[i].Y, 4);
            }
            features[10] /= list.Count;
            features[10] = features[10] / Math.Pow(featDic["均方根值"], 4);
            features[10] = Math.Round(features[10], 2);
            featDic["峭度指标"] = features[10];

            //自相关函数,m=2,autocorrelation
            for (int i = 0; i < (list.Count - 2); i++)
            {
                features[11] += p1[i].Y * p1[i + 2].Y;
            }
            features[11] = Math.Round(features[11], 2);
            featDic["自相关函数"] = features[11];

            //互相关函数,cross-correlation
            for (int i = 0; i < (list.Count - 2); i++)
            {
                features[12] += p1[i].Y * p1[i + 2].X;
            }
            features[12] = Math.Round(features[12], 2);
            featDic["互相关函数"] = features[12];

            return featDic;
        }

        /**
         * 计算频域特性：傅立叶
         **/
        public List<Point> getFrequentFFTFeature(List<PointD> list)
        {
            List<Point> fftList = new List<Point>();
            List<PointD> tempList = new List<PointD>();
           
            //首先将实数转为复数数组，接着进行傅立叶变换，之后将复数变换成实数
            if (list.Count == 0 || list == null)
                return null;
            if (list.Count == 1)
            {
                //fftList.Add(new Point())
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null)
                        fftList.Add(new Point((int)list[i].X, (int)list[i].Y));
            }
            else     //只找到参数list中有数据的对象
            {
                
                fftList = complexToReal(fft_frequency(realToComplex(list), list.Count));
            }

            return fftList;

            
        }

        //反傅立叶变换
        public List<Point> getFrequentIFFTFeature(List<PointD> list)
        {
            List<Point> ifftList = new List<Point>();
            List<PointD> tempList = new List<PointD>();
        
              //首先将实数转为复数数组，接着进行傅立叶变换，之后将复数变换成实数
            if (list.Count == 0 || list == null)
                return null;
            if (list.Count == 1)
            {
                //fftList.Add(new Point())
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null)
                        ifftList.Add(new Point((int)list[i].X, (int)list[i].Y));
            }
            else     //只找到参数list中有数据的对象
            {
                /*for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null)
                    {
                        tempList.Add(list[i]);
                    }
                }*/
                ifftList = complexToReal(ifft_frequency(realToComplex(list), list.Count));
            }
                
             
           

            return ifftList;


        }

        public Complex[] realToComplex(List<PointD> list)
        {
            if (list == null)
                return null;

            Complex[] c = new Complex[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                    continue;
                c[i] = Complex.transferToComplex(list[i]);
            }

            return c;
        }

        public List<Point> complexToReal(Complex[] complex)
        {
            if (complex == null)
                return null;
            List<Point> pointList = new List<Point>();
            Point p;
            for (int i = 0; i < complex.Length; i++)
            {
                if (complex[i] == null)
                {
                    continue;
                }
                p = new Point((int)complex[i].Real, (int)complex[i].Image);
                pointList.Add(p);
            }

            return pointList;
        }


        /// <summary>
        /// 一维频率抽取基2快速傅里叶变换
        /// 频率抽取：输入为自然顺序，输出为码位倒置顺序
        /// 基2：待变换的序列长度必须为2的整数次幂
        /// </summary>
        /// <param name="sourceData">待变换的序列(复数数组)</param>
        /// <param name="countN">序列长度,可以指定[0,sourceData.Length-1]区间内的任意数值</param>
        /// <returns>返回变换后的序列（复数数组）</returns>
        private Complex[] fft_frequency(Complex[] sourceData, int countN)
        {
            
            if (countN == 0)
                return null;
            if (sourceData == null)
                return null;

            //2的r次幂为N，求出r.r能代表fft算法的迭代次数
            double dr = Math.Log(countN, 2);
            //int r = Convert.ToInt32(dr);
            int r = (int)Math.Ceiling(dr);   //向上取整
            Complex[] resultData = new Complex[countN];

            if (dr - r != 0)
            {
                countN = (int)Math.Pow(2, r);
            }


            //分别存储蝶形运算过程中左右两列的结果
            Complex[] interVar1 = new Complex[countN];
            Complex[] interVar2 = new Complex[countN];

            //interVar1 = (Complex[])sourceData.Clone();
            int index = 0;
            for (; index < sourceData.Length; index++)
            {
                interVar1[index] = sourceData[index];
            }
            if (sourceData.Length < countN)
            {

                while (index < countN)
                {
                    
                    interVar1[index] = new Complex();
                    index++;
                }
            }

            //w代表旋转因子
            Complex[] w = new Complex[countN / 2];
            //为旋转因子赋值。（在蝶形运算中使用的旋转因子是已经确定的，提前求出以便调用）
            //旋转因子公式 \  /\  /k __
            //              \/  \/N  --  exp(-j*2πk/N)
            //这里还用到了欧拉公式
            for (int i = 0; i < countN / 2; i++)
            {
                double angle = -i * Math.PI * 2 / countN;
                w[i] = new Complex(Math.Cos(angle), Math.Sin(angle));
            }

            //蝶形运算
            for (int i = 0; i < r; i++)
            {
                //i代表当前的迭代次数，r代表总共的迭代次数.
                //i记录着迭代的重要信息.通过i可以算出当前迭代共有几个分组，每个分组的长度

                //interval记录当前有几个组
                // <<是左移操作符，左移一位相当于*2
                //多使用位运算符可以人为提高算法速率^_^
                int interval = 1 << i;

                //halfN记录当前循环每个组的长度N
                int halfN = 1 << (r - i);

                //循环，依次对每个组进行蝶形运算
                for (int j = 0; j < interval; j++)
                {
                    //j代表第j个组

                    //gap=j*每组长度，代表着当前第j组的首元素的下标索引
                    int gap = j * halfN;

                    

                    //进行蝶形运算
                    for (int k = 0; k < halfN / 2; k++)
                    {
                       
                        interVar2[k + gap] = interVar1[k + gap] + interVar1[k + gap + halfN / 2];
                        interVar2[k + (halfN / 2) + gap] = (interVar1[k + gap] - interVar1[k + gap + (halfN / 2)]) * w[k * interval];
                    }
                }

                //将结果拷贝到输入端，为下次迭代做好准备
                interVar1 = (Complex[])interVar2.Clone();
            }

            //将输出码位倒置
            for (uint j = 0; j < countN; j++)
            {
                //j代表自然顺序的数组元素的下标索引

                //用rev记录j码位倒置后的结果
                uint rev = 0;
                //num作为中间变量
                uint num = j;

                //码位倒置（通过将j的最右端一位最先放入rev右端，然后左移，然后将j的次右端一位放入rev右端，然后左移...）
                //由于2的r次幂=N，所以任何j可由r位二进制数组表示，循环r次即可
                for (int i = 0; i < r; i++)
                {
                    rev <<= 1;
                    rev |= num & 1;
                    num >>= 1;
                }
                interVar2[rev] = interVar1[j];
            }
            for (int i = 0; i < resultData.Length; i++)
                resultData[i] = interVar2[i];
            return resultData;

        }

        /// <summary>
        /// 一维频率抽取基2快速傅里叶逆变换
        /// </summary>
        /// <param name="sourceData">待反变换的序列（复数数组）</param>
        /// <param name="countN">序列长度,可以指定[0,sourceData.Length-1]区间内的任意数值</param>
        /// <returns>返回逆变换后的序列（复数数组）</returns>
        private Complex[] ifft_frequency(Complex[] sourceData, int countN)
        {
            //将待逆变换序列取共轭，再调用正变换得到结果，对结果统一再除以变换序列的长度N
            if (countN == 0)
                return null;
            for (int i = 0; i < countN; i++)
            {
                sourceData[i] = sourceData[i].Conjugate();
            }

            Complex[] interVar = new Complex[countN];

            interVar = fft_frequency(sourceData, countN);

            for (int i = 0; i < countN; i++)
            {
                if(interVar[i] != null)
                    interVar[i] = new Complex(interVar[i].Real / countN, -interVar[i].Image / countN);
            }

            return interVar;
        }
       
    }
}

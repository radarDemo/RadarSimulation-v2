using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
//using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
namespace udpserver
{
    class Program
    {
        static int num=20;     //客户端允许开启的个数
       static void Main(string[] args)
        {
            Thread myThread = new Thread(new ThreadStart(SendData));
            //将线程设为后台运行   
         //   myThread.IsBackground=true;

            myThread.Start();
        }     
    
      

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
       struct StructDemo
       {         
           public SCSMXPHead scsmhead;
           public SrcTgtTrk srcTgtTrk;
       }

        struct SCSMXPHead{

            public ushort length;//报文长度
            public ushort reserve;//备用
            public uint send;//报文源地址
            public uint recv;//报文目的地址
            public Byte seq_no;//序列号
            public Byte ack_no;//确认号
            public Byte flag;//报文标识
            public Byte unit_num;//信息单元个数
            public Byte unit_seq;//信息单元序号
            public Byte unit_flag;//信息单元标识
            public ushort unit_length;//信息单元长度
            public uint time_stamp;//时戳，0.1ms
        }

        struct SrcTgtTrk //原始航迹报文
        {
            public short nPlatID;//平台号
            public short nInfSrcID;//信源号
            public int batch;//批号
            public char nInfSrcType; //信源类型 （1：普通雷达）
            public char nTgtBuildFlag;//目标生成方式
            public char nTrkStatus; //航迹状态（1新航迹 2更新航迹 3撤消航迹 4丢失航迹）
            public char nTrkDim; //航迹维数（1三坐标 2距离方位 3纯方位 4方位俯仰）
            public char bDetectPt; //外推标记
            public char bTrkTrackOk;//航迹跟踪标记
            public char nFastCorr;//快速关联标记
            public char nReserve2;
            public long lTime;//目标更新时刻，单位：毫秒
            public long lRcvTime;//接收时刻， 单位：毫秒
            public char nTrkQuality;//航迹质量等级
            public char nRealInfoFlag;//情报类型
            public char nRdrWorkMode;//工作模式
            public char nWarningFlag;//告警标识
            public WdTgtTrk nWdTrk;//控制字
            public float dLon;//经度 单位：0.001度
            public float dLat;//纬度  单位：0.001度
            public float dAlt;//高度 单位：米
            public float fDistance;//距离 单位：米
            public float fBear;//方位  单位：度
            public float fElev;//仰角  单位：度
            public float fSpeed;//绝对水平速度 单位：米
            public float fCourse; //绝对航向 单位：度
            public float fvz; //z速度 单位：米/秒
            public float fReSpeed;//相对航速
            public float fReCourse;//相对航向
            public float fErrDisX;//距离精度 单位：米
            public float fErrBearY; //方位精度 单位：度
            public float fErrElevZ; //仰角精度 单位：度
            public char nTgtNum;//目标数量 
            public char nArmyCivil;//军民
            public char nAttr;//属性
            public char nType;//类型
            //public short nKind;//种类
            public short nNation;//国籍
            public short nKind;//种类
            public short nModel;//目标型号
            public short nReserve3;//备用 
            public long nRadarT; //天线扫描周期 单位：0.001秒
        }

 

 

//控制字：有效填1，无效填0

        struct WdTgtTrk{
            public uint Pos;//B0:经纬高，有其中任一个则填1
            public uint DBE;//B1:方位距离仰角，有其中任一个则填1
            public uint VelRadius;//B2相对航速，表示径向速度
            public uint VelCourseRel;//B3:相对航速航向
            public uint VelCourseAbs;//B4:绝对航速航向
            public uint Reserve1; //B5
            public uint Reserve2; //B6
            public uint Reserve3; //B7
            public uint Reserve4; //B8
            public uint Reserve5; //B9
            public uint Reserve6; //B10
            public uint Reserve7; //B11
            public uint Reserve8; //B12
            public uint Reserve9; //B13
            public uint Reserve10; //B14
            public uint TgtName; //B15名称
            public uint TgtAttr; //B16 属性
            public uint TgtType;//B17 类型
            public uint TgtKind; //B18 种类
            public uint TgtNation; //B19 国籍
            public uint TgtBoardID; //B20 机舷号
            public uint TgtPlatModel; //B21 平台号
            public uint TgtArmCivil; //B22 军民
            public uint Reserve11; //B23
            public uint Reserve12; //B24
            public uint Reserve13; //B25
            public uint Reserve14; //B26
            public uint Reserve15; //B27
            public uint Reserve16; //B28
            public uint Reserve17; //B29
            public uint Reserve18; //B30
            public uint Reserve19; //B31

            }
    
           //将结构体转换为字节数组
           public static byte[] StructToBytes(object structObj, int size)
           {
               //StructDemo sd;
               //int num = 2;
               byte[] bytes = new byte[size];
               IntPtr structPtr = Marshal.AllocHGlobal(size);
               //将结构体拷到分配好的内存空间
               Marshal.StructureToPtr(structObj, structPtr, false);
               //从内存空间拷贝到byte 数组
               Marshal.Copy(structPtr, bytes, 0, size);
               //释放内存空间
               Marshal.FreeHGlobal(structPtr);
               return bytes;

           }

           //将Byte转换为结构体类型
           public static object ByteToStruct(byte[] bytes, Type type)
           {
               int size = Marshal.SizeOf(type);
               if (size > bytes.Length)
               {
                   return null;
               }
               //分配结构体内存空间
               IntPtr structPtr = Marshal.AllocHGlobal(size);
               //将byte数组拷贝到分配好的内存空间
               Marshal.Copy(bytes, 0, structPtr, size);
               //将内存空间转换为目标结构体
               object obj = Marshal.PtrToStructure(structPtr, type);
               //释放内存空间
               Marshal.FreeHGlobal(structPtr);
               return obj;
           }
        

         private  static void SendData()
        {

            StructDemo sd;
            sd.scsmhead.length = 200;
            sd.scsmhead.recv = 127001;
            sd.scsmhead.reserve = 0;
            sd.scsmhead.send = 0;
            sd.scsmhead.seq_no = 1;
            sd.scsmhead.ack_no = 1;
            sd.scsmhead.flag = 1;
            sd.scsmhead.unit_num = 1;
            sd.scsmhead.unit_seq = 1;
            sd.scsmhead.unit_flag = 0x76;
            sd.scsmhead.unit_length = 200;
            sd.scsmhead.time_stamp = 1;
            sd.srcTgtTrk.nPlatID = 1;
            sd.srcTgtTrk.nInfSrcID = 1;
            sd.srcTgtTrk.batch = 1;
            sd.srcTgtTrk.nInfSrcType = '1';
            sd.srcTgtTrk.nTgtBuildFlag = '1';
            sd.srcTgtTrk.nTrkStatus = '1';
            sd.srcTgtTrk.nTrkDim = '1';
            sd.srcTgtTrk.bDetectPt = '1';
            sd.srcTgtTrk.bTrkTrackOk = '1';
            sd.srcTgtTrk.nFastCorr = '1';
            sd.srcTgtTrk.nReserve2 = '1';
            sd.srcTgtTrk.lTime = 1;
            sd.srcTgtTrk.lRcvTime = 1;
            sd.srcTgtTrk.nTrkQuality = '1';
            sd.srcTgtTrk.nRealInfoFlag = '1';
            sd.srcTgtTrk.nRdrWorkMode = '1';
            sd.srcTgtTrk.nWarningFlag = '1';

            sd.srcTgtTrk.nWdTrk.Pos = 1;
            sd.srcTgtTrk.nWdTrk.DBE = 1;
            sd.srcTgtTrk.nWdTrk.VelRadius = 1;
            sd.srcTgtTrk.nWdTrk.VelCourseRel = 1;
            sd.srcTgtTrk.nWdTrk.VelCourseAbs = 1;
            sd.srcTgtTrk.nWdTrk.Reserve1 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve2 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve3 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve4 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve5 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve6 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve7 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve8 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve9 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve10 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve11 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve12 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve13 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve14 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve15 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve16 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve17 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve18 = 1;
            sd.srcTgtTrk.nWdTrk.Reserve19 = 1;
            sd.srcTgtTrk.nWdTrk.TgtName = 1;
            sd.srcTgtTrk.nWdTrk.TgtType = 1;
            sd.srcTgtTrk.nWdTrk.TgtAttr = 1;
            sd.srcTgtTrk.nWdTrk.TgtKind = 1;
            sd.srcTgtTrk.nWdTrk.TgtNation = 1;
            sd.srcTgtTrk.nWdTrk.TgtBoardID = 1;
            sd.srcTgtTrk.nWdTrk.TgtPlatModel = 1;
            sd.srcTgtTrk.nWdTrk.TgtArmCivil = 1;

            sd.srcTgtTrk.dLat = 1;
            sd.srcTgtTrk.dLon = 1;
            sd.srcTgtTrk.dAlt = 1;
            sd.srcTgtTrk.fDistance = 1;
            sd.srcTgtTrk.fBear = 1;
            sd.srcTgtTrk.fElev = 1;
            sd.srcTgtTrk.fSpeed = 1;
            sd.srcTgtTrk.fCourse = 1;
            sd.srcTgtTrk.fvz = 1;
            sd.srcTgtTrk.fReCourse = 1;
            sd.srcTgtTrk.fReSpeed = 1;
            sd.srcTgtTrk.fErrDisX = 1;
            sd.srcTgtTrk.fErrBearY = 1;
            sd.srcTgtTrk.fErrElevZ = 1;
            sd.srcTgtTrk.nTgtNum = '1';
            sd.srcTgtTrk.nArmyCivil = '1';
            sd.srcTgtTrk.nAttr = '1';
            sd.srcTgtTrk.nType = '5';
            sd.srcTgtTrk.nKind = 1;
            sd.srcTgtTrk.nNation = 1;
            sd.srcTgtTrk.nModel = 1;
            sd.srcTgtTrk.nReserve3 = 1;
            sd.srcTgtTrk.nRadarT = 1;
         

            int size = 0;
            //此处使用非安全代码来获取到StructDemo的值
            //unsafe
            //{
            //    size = Marshal.SizeOf(sd);
            //}
             unsafe
             {              
                 size = Marshal.SizeOf(sd);
             }
             
             //Console.WriteLine(size);
                     
        
            int i = 0;
            IPAddress HostIP = IPAddress.Parse("127.0.0.1");

            int []port = new int[num]; //创建20个udp sockets，对应的客户端也可以打开20个            
            IPEndPoint []iep=new IPEndPoint[num];
            UdpClient[] udpServer = new UdpClient[num];
           
          
            for (int j = 0; j < 20; j++)
            {
                port[j]=10000+j;
                iep[j] = new IPEndPoint(HostIP, port[j]);
                udpServer[j] = new UdpClient();
            }
            
            while (true)
            {
                sd.srcTgtTrk.dLat = 8 * i+50;
                sd.srcTgtTrk.dLon = 8 * i+50;
                byte[] bytes = StructToBytes(sd, size);            
                try
                {
                    
                    Console.WriteLine(bytes[1]);
                    for (int ct = 0; ct < 20;ct++ )
                        udpServer[ct].Send(bytes, bytes.Length, iep[ct]);
                    //   udpServer1.Send(bytes, bytes.Length, host1);
                    i++;
                    if ((i + 1) % 30 == 0)
                        i = 0;
                    Thread.Sleep(2000);  //为了让客户端更有效接受
                  

                }
                catch (Exception err)
                {
                    //  MessageBox.Show(err.Message, "发送失败");
                }
               
               
                // udpServer1.Close();
            }
            for (int i1 = 0; i1 < 20; i1++)
                udpServer[i1].Close();
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
    


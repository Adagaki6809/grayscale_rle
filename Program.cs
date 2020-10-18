using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace grayscale_rle
{
    class Program
    {
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        static void Main(string[] args)
        {
            if (args.Length!=0) {
                Console.WriteLine(args[0]);
                Console.ReadKey();
            }
            
            FileStream fsHIP = new FileStream("font1.hip", FileMode.Open, FileAccess.Read);
            Console.WriteLine("длина font1.hip: " + fsHIP.Length);
            byte[] HipData = new byte[fsHIP.Length];
            fsHIP.Read(HipData, 0, (int)fsHIP.Length);
            //ширина из .hip
            byte[] widthHIP = new byte[4] { HipData[16], HipData[17], HipData[18], HipData[19] };
            //высота из .hip
            byte[] heightHIP = new byte[4] { HipData[20], HipData[21], HipData[22], HipData[23] };
            //длина пиксельных данных
            int PixelDataLengthHIP = BitConverter.ToInt32(widthHIP, 0) * BitConverter.ToInt32(heightHIP, 0) * 4;

            Console.WriteLine("ширина font1.hip: " + BitConverter.ToInt32(widthHIP, 0));
            Console.WriteLine("швысота font1.hip: " + BitConverter.ToInt32(heightHIP, 0));
            Console.Write(BitConverter.ToString(HipData, 32, 46).Replace("-", " "));
            Console.WriteLine();

            string BMPHeaderString = "424D36002000000000003600000028000000000200000004000001002000000000000000000000000000000000000000000000000000";
            byte[] BMPHeaderBytes = StringToByteArray(BMPHeaderString);
            for (int i = 0; i < BMPHeaderBytes.Length; i++)
                Console.Write(BMPHeaderBytes[i]);
            Console.WriteLine();
            FileStream fsBMP = new FileStream("font1.bmp", FileMode.Create, FileAccess.ReadWrite);
            
            fsBMP.Write(BMPHeaderBytes, 0, BMPHeaderBytes.Length);
            
            byte[] PixelDataHIP = new byte[PixelDataLengthHIP];
            byte[] t = new byte[PixelDataLengthHIP];

            int count = 0;
            int inc = 0;
            for (int i = 32; i < HipData.Length;)
            {
                count = HipData[i + 2];
                while (count != 0)
                {
                    PixelDataHIP[inc] = HipData[i + 1];
                    PixelDataHIP[inc+1] = HipData[i];
                    PixelDataHIP[inc+2] = HipData[i];
                    PixelDataHIP[inc+3] = HipData[i];
                    count--;
                    inc+=4;
                }
                i += 3;
            }

            
            for (int i = 0; i < 1024;i++)
            {
                Array.Copy(PixelDataHIP, i*2048, t, t.Length - 2048 - i*2048, 2048);

            }



            Console.WriteLine("Seek:" + fsBMP.Seek(0, SeekOrigin.End));
            fsBMP.Write(PixelDataHIP, 0, PixelDataHIP.Length);


            FileStream fsHIPNew = new FileStream("font.hip", FileMode.Create, FileAccess.Write);
            
            fsBMP.Close();
            Array.Clear(PixelDataHIP, 0, PixelDataHIP.Length);
            int PixelDataLengthBMP = 4096 * 4096 * 4;
            byte[] PixelDataBMP = new byte[PixelDataLengthBMP];
            fsBMP = new FileStream("font.bmp", FileMode.Open, FileAccess.Read);
            Console.WriteLine("Seek:" + fsBMP.Seek(54, SeekOrigin.Begin)); // тут надо именно Begin
            Console.WriteLine("seek: " + fsBMP.Position);
            fsBMP.Read(PixelDataBMP, 0, PixelDataBMP.Length);

            byte[] HIPDataNew = new byte[10000000];
            byte[] prev = new byte[2];
            count = 0;
            int j = 0;
            //for (int i = 0; i < PixelData.Length;i+=4)
            for (int i = 0; i < PixelDataLengthBMP; i += 4)
            {
                if (count < 1)
                {
                    prev[0] = HIPDataNew[j] = PixelDataBMP[i + 1];
                    prev[1] = HIPDataNew[j + 1] = PixelDataBMP[i];
                    count++;
                    HIPDataNew[j + 2] = (byte)count;
                    continue;
                }
                if (i == 1742116)
                { }
                if ((PixelDataBMP[i + 1] == prev[0]) && (PixelDataBMP[i] == prev[1]) && (PixelDataBMP[i + 2] == prev[0]) && (PixelDataBMP[i + 3] == prev[0]))
                {
                    if ((count == 255))
                    {
                        count=1;
                        j += 3;
                        HIPDataNew[j + 2] = (byte)count;
                    }
                    else
                    {
                        count ++;
                        HIPDataNew[j + 2] = (byte)count;
                    }
                }
                else
                {
                    count = 1;
                    j += 3;
                    prev[0] = HIPDataNew[j] = PixelDataBMP[i + 1];
                    prev[1] = HIPDataNew[j + 1] = PixelDataBMP[i];
                    HIPDataNew[j + 2] = (byte)count;
                }
            }

            string s = BitConverter.ToString(HIPDataNew, 0, HIPDataNew.Length).Replace("-", " ");
            int bb = s.IndexOf("00 00 00");
            //bb=s.Length;
            Console.WriteLine("длина fontDATA: "+bb/3);
            fsHIPNew.Seek(32, SeekOrigin.Begin);
            fsHIPNew.Write(HIPDataNew, 0, bb/3);
            
            Console.WriteLine("длина hipdatanew: " + fsHIPNew.Length);
            s = BitConverter.ToString(BitConverter.GetBytes(fsHIPNew.Length), 0, 4).Replace("-", "");
            Console.WriteLine("длина hipdatanew(string): " + s);
            
            
            string HIPHeaderString = "4849500025010000"+s+"00000000"+ "00100000" + "00100000" + "0401000000000000";
            byte[] HIPHeaderBytes = StringToByteArray(HIPHeaderString);
            fsHIPNew.Seek(0, SeekOrigin.Begin);
            fsHIPNew.Write(HIPHeaderBytes, 0, HIPHeaderBytes.Length);
            
            Console.WriteLine("длина hipheader: " + HIPHeaderBytes.Length);

            fsHIP.Close();
            fsBMP.Close();
            fsHIPNew.Close();
            Console.ReadLine();
        }
    }
}




#region вроде как поворот картинки на 180 градусов
//Array.Reverse(PixelData);
/*
byte temp = 0;
for (int k = 0; k < PixelData.Length;)
{
    for (int i = 0; i < (2048/2);)
    {
        for (int j = 0; j < 4; j++)
        {
            temp = PixelData[k+i + j];
            PixelData[k+i+ j] = PixelData[PixelData.Length - i - 4 + j-k];
            PixelData[PixelData.Length - i - 4 + j-k] = temp;
        }
        i += 4;
    }
    k += 2048;
}
*/
#endregion
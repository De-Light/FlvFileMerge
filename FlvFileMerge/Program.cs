using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlvFileMerge
{
    class Program
    {
        static void Main()
        {
            //文件路径
            string dirAddr = @"G:\Movie\历史\";
            string outputAddr = @"G:\Movie\history\";
            //分P循环
            for (int i = 1; i <= 140; i++)
            {
                string addr = dirAddr + i.ToString() + '\\';
                string fileName = outputAddr + i.ToString() + "." + getFileName(addr) + ".flv";
                List<string> blvFiles = GetBlvFiles(addr);
                MergeFlv(fileName, blvFiles);
            }
            MergeFlv();
        }

        private static void MergeFlv(string fileName, List<string> blvFiles)
        {
            CheckFileSuitable(blvFiles);
            MergeFile(fileName, blvFiles);
            Console.WriteLine(fileName + "Merge finished");
        }

        private static void MergeFile(string fileName, List<string> blvFiles)
        {
            using (FileStream fsMerge = new FileStream(fileName, FileMode.Create))
            {
                int time = 0;
                for (int i = 0; i < blvFiles.Count; i++)
                {
                    using (FileStream fs = new FileStream(blvFiles[i], FileMode.Open))
                    {
                        bool firstFile = Convert.ToBoolean(i);
                        time = Merge(fs, fsMerge, !firstFile, time);
                    }
                }
            }
        }

        private static void CheckFileSuitable(List<string> blvFiles)
        {
            for (int i = 0; i < blvFiles.Count - 1; i++)
                using (FileStream fs1 = new FileStream(blvFiles[i], FileMode.Open))
                {
                    for (int j = i + 1; j < blvFiles.Count; j++)
                    {
                        using (FileStream fs2 = new FileStream(blvFiles[j], FileMode.Open))
                            if (!IsSuitableToMerge(GetFLVFileInfo(fs1), GetFLVFileInfo(fs2)))
                            {
                                Console.WriteLine("Video files not suitable to merge");
                                Environment.Exit(0);
                            }
                    }
                }
        }

        private static List<string> GetBlvFiles(string addr)
        {
            List<string> rtn = new List<string>();
            for (int i = 0; i < 15; i++)
            {
                string blvAddr = addr + "lua.flv720.bili2api.64\\" + i.ToString() + ".blv";
                if (File.Exists(blvAddr))
                {
                    rtn.Add(blvAddr);
                }
            }
            return rtn;
        }

        private static string getFileName(string addr)
        {
            StreamReader r = new StreamReader(addr + "entry.json");
            string jsonStr = r.ReadLine();
            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonStr);
            string fileName = jo["page_data"]["part"].ToString();
            return fileName;
        }

        static void MergeFlv()
        {
            String path1 = @"G:\Movie\历史\1\lua.flv720.bili2api.64\0.blv";
            String path2 = @"G:\Movie\历史\1\lua.flv720.bili2api.64\1.blv";
            String path3 = @"G:\Movie\历史\1\lua.flv720.bili2api.64\2.blv";
            String output = @"G:\Movie\历史\1\lua.flv720.bili2api.64\out.flv";

            using (FileStream fs1 = new FileStream(path1, FileMode.Open))
            using (FileStream fs2 = new FileStream(path2, FileMode.Open))
            using (FileStream fs3 = new FileStream(path3, FileMode.Open))
            using (FileStream fsMerge = new FileStream(output, FileMode.Create))
            {
                Console.WriteLine(IsFLVFile(fs1));
                Console.WriteLine(IsFLVFile(fs2));
                Console.WriteLine(IsFLVFile(fs3));

                if (IsSuitableToMerge(GetFLVFileInfo(fs1), GetFLVFileInfo(fs2)) == false
                    || IsSuitableToMerge(GetFLVFileInfo(fs1), GetFLVFileInfo(fs3)) == false)
                {
                    Console.WriteLine("Video files not suitable to merge");
                }

                int time = Merge(fs1, fsMerge, true, 0);
                time = Merge(fs2, fsMerge, false, time);
                time = Merge(fs3, fsMerge, false, time);
                Console.WriteLine("Merge finished");
            }
        }

        const int FLV_HEADER_SIZE = 9;
        const int FLV_TAG_HEADER_SIZE = 11;
        const int MAX_DATA_SIZE = 16777220;

        class FLVContext
        {
            public byte soundFormat;
            public byte soundRate;
            public byte soundSize;
            public byte soundType;
            public byte videoCodecID;
        }

        static bool IsSuitableToMerge(FLVContext flvCtx1, FLVContext flvCtx2)
        {
            return (flvCtx1.soundFormat == flvCtx2.soundFormat) &&
              (flvCtx1.soundRate == flvCtx2.soundRate) &&
              (flvCtx1.soundSize == flvCtx2.soundSize) &&
              (flvCtx1.soundType == flvCtx2.soundType) &&
              (flvCtx1.videoCodecID == flvCtx2.videoCodecID);
        }

        static bool IsFLVFile(FileStream fs)
        {
            int len;
            byte[] buf = new byte[FLV_HEADER_SIZE];
            fs.Position = 0;
            if (FLV_HEADER_SIZE != fs.Read(buf, 0, buf.Length))
                return false;

            if (buf[0] != 'F' || buf[1] != 'L' || buf[2] != 'V' || buf[3] != 0x01)
                return false;
            else
                return true;
        }

        static FLVContext GetFLVFileInfo(FileStream fs)
        {
            bool hasAudioParams, hasVideoParams;
            int skipSize, readLen;
            int dataSize;
            byte tagType;
            byte[] tmp = new byte[FLV_TAG_HEADER_SIZE + 1];
            if (fs == null) return null;

            FLVContext flvCtx = new FLVContext();
            fs.Position = 0;
            skipSize = 9;
            fs.Position += skipSize;
            hasVideoParams = hasAudioParams = false;
            skipSize = 4;
            while (!hasVideoParams || !hasAudioParams)
            {
                fs.Position += skipSize;

                if (FLV_TAG_HEADER_SIZE + 1 != fs.Read(tmp, 0, tmp.Length))
                    return null;

                tagType = (byte)(tmp[0] & 0x1f);
                switch (tagType)
                {
                    case 8:
                        flvCtx.soundFormat = (byte)((tmp[FLV_TAG_HEADER_SIZE] & 0xf0) >> 4);
                        flvCtx.soundRate = (byte)((tmp[FLV_TAG_HEADER_SIZE] & 0x0c) >> 2);
                        flvCtx.soundSize = (byte)((tmp[FLV_TAG_HEADER_SIZE] & 0x02) >> 1);
                        flvCtx.soundType = (byte)((tmp[FLV_TAG_HEADER_SIZE] & 0x01) >> 0);
                        hasAudioParams = true;
                        break;
                    case 9:
                        flvCtx.videoCodecID = (byte)((tmp[FLV_TAG_HEADER_SIZE] & 0x0f));
                        hasVideoParams = true;
                        break;
                    default:
                        break;
                }

                dataSize = FromInt24StringBe(tmp[1], tmp[2], tmp[3]);
                skipSize = dataSize - 1 + 4;
            }

            return flvCtx;
        }

        static int FromInt24StringBe(byte b0, byte b1, byte b2)
        {
            return (int)((b0 << 16) | (b1 << 8) | (b2));
        }

        static int GetTimestamp(byte b0, byte b1, byte b2, byte b3)
        {
            return ((b3 << 24) | (b0 << 16) | (b1 << 8) | (b2));
        }

        static void SetTimestamp(byte[] data, int idx, int newTimestamp)
        {
            data[idx + 3] = (byte)(newTimestamp >> 24);
            data[idx + 0] = (byte)(newTimestamp >> 16);
            data[idx + 1] = (byte)(newTimestamp >> 8);
            data[idx + 2] = (byte)(newTimestamp);
        }

        static int Merge(FileStream fsInput, FileStream fsMerge, bool isFirstFile, int lastTimestamp = 0)
        {
            int readLen;
            int curTimestamp = 0;
            int newTimestamp = 0;
            int dataSize;
            byte[] tmp = new byte[20];
            byte[] buf = new byte[MAX_DATA_SIZE];

            fsInput.Position = 0;
            if (isFirstFile)
            {
                if (FLV_HEADER_SIZE + 4 == (fsInput.Read(tmp, 0, FLV_HEADER_SIZE + 4)))
                {
                    fsMerge.Position = 0;
                    fsMerge.Write(tmp, 0, FLV_HEADER_SIZE + 4);
                }
            }
            else
            {
                fsInput.Position = FLV_HEADER_SIZE + 4;
            }

            while (fsInput.Read(tmp, 0, FLV_TAG_HEADER_SIZE) > 0)
            {
                dataSize = FromInt24StringBe(tmp[1], tmp[2], tmp[3]);
                curTimestamp = GetTimestamp(tmp[4], tmp[5], tmp[6], tmp[7]);
                newTimestamp = curTimestamp + lastTimestamp;
                SetTimestamp(tmp, 4, newTimestamp);
                fsMerge.Write(tmp, 0, FLV_TAG_HEADER_SIZE);

                readLen = dataSize + 4;
                if (fsInput.Read(buf, 0, readLen) > 0)
                {
                    fsMerge.Write(buf, 0, readLen);
                }
                else
                {
                    goto failed;
                }
            }

            return newTimestamp;

            failed:
            throw new Exception("Merge Failed");
        }
    }

}

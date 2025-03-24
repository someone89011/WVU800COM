using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using static WVU800COM.WVU800COM;

namespace WVU800COM
{

    public class WVU800COM
    {
        // serial command

        private const byte CU_HEADER = 0x55;
        private const byte PAD_HEADER = 0xAA;

        // 4 byte
        const byte GET_CU_CHANNEL = 0;
        const byte GET_ACK = 1;

        // 7 byte
        const byte VOTE_START = 0;
        const byte SET_CU_CHANNEL = 1;

        const byte SET_OFF_PAD = 0x80;
        const byte SET_PAD_CHANNEL = 0x81;
        const byte SET_PAD_ATTEND = 0x82;
        const byte SET_PAD_THERSHOLD = 0x83;
        const byte SET_PAD_ID = 0x84;
        const byte SET_PAD_TEST_KEY = 0x85;
        const byte SET_PAD_AUTO_CHANNEL = 0x86;
        const byte SET_PAD_LED = 0x87;

        // 8 byte
        const byte SCAN_SIZE = 0;
        const byte GIVEID_START = 2;

        // 20 byte
        const byte SET_LCD_WELCOME = 16;

        public enum VoteMode : byte { Key3 = 1, Key10 = 2, Multi = 3, Rate = 4, CM1 = 5, CM2 = 6, CM3 = 7, CM4 = 8, AttendSpecKey = 13, AttendAnyKey = 14, AttendSensor = 15, AttendNew = 16, LastValid = 16, Secret = 32 };

        public enum AttendMode : byte { AttendKey = 0, AnyKey = 1 };

        SerialPort sp = new SerialPort();

        // interrupt variable
        List<byte> rxData = new List<byte>();

        // Handler variable
        private static List<byte> response = new List<byte>();

        public event EventHandler<WVU800COMEventArgs> DataReceived;

        static AutoResetEvent autoEvent = new AutoResetEvent(false);

        WVU800COMEventArgs edata;

        public class WVU800COMEventArgs : EventArgs
        {
            public List<int> id = new List<int>();
            public List<byte> val1 = new List<byte>();
            public List<byte> val2 = new List<byte>();
            public List<double> battlv = new List<double>();

        }

        public virtual void OnDataReceived(WVU800COMEventArgs e)
        {
            if (DataReceived != null)
                DataReceived.Invoke(this, e);
        }

        public WVU800COM()
        {
            sp.BaudRate = 115200;
            sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);

        }

        public int open_com(string com)
        {
            try
            {
                sp.PortName = com;
                sp.Open();
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        public void close_com()
        {
            try
            {
                sp.Close();
            }
            catch (SystemException e)
            { }
        }

        public bool IsOpen()
        {
            return sp.IsOpen;
        }

        void com_data(byte d1)
        {
            byte[] dat = new byte[4];
            dat[0] = 0x55;
            dat[1] = 0x34;
            dat[2] = d1;
            dat[3] = 0x35;
            if (sp.IsOpen)
            {
                sp.Write(dat, 0, 4);
                System.Threading.Thread.Sleep(10);
            }
        }

        void com_data(int d1, byte d2, byte d3)
        {
            byte[] dat = new byte[7];
            dat[0] = 0x55;
            dat[1] = 0x37;
            dat[2] = (byte)(d1 & 0xFF);
            dat[3] = (byte)((d1 >> 8) & 0xFF);
            dat[4] = d2;
            dat[5] = d3;
            dat[6] = 0x35;
            if (sp.IsOpen)
            {
                sp.Write(dat, 0, 7);
                System.Threading.Thread.Sleep(10);
            }
        }

        void com_data(int d1, byte d2, byte d3, byte d4)
        {
            byte[] dat = new byte[8];
            dat[0] = 0x55;
            dat[1] = 0x38;
            dat[2] = (byte)(d1 & 0xFF);
            dat[3] = (byte)((d1 >> 8) & 0xFF);
            dat[4] = d2;
            dat[5] = d3;
            dat[6] = d4;
            dat[7] = 0x35;
            if (sp.IsOpen)
            {
                sp.Write(dat, 0, 8);
                System.Threading.Thread.Sleep(10);
            }
        }

        void com_data(int d1, byte d2, byte d3, byte d4, byte d5, byte d6)
        {
            byte[] dat = new byte[10];
            dat[0] = 0x55;
            dat[1] = 0x3A;
            dat[2] = (byte)(d1 & 0xFF);
            dat[3] = (byte)((d1 >> 8) & 0xFF);
            dat[4] = d2;
            dat[5] = d3;
            dat[6] = d4;
            dat[7] = d5;
            dat[8] = d6;
            dat[9] = 0x35;
            if (sp.IsOpen)
            {
                sp.Write(dat, 0, 10);
                System.Threading.Thread.Sleep(10);
            }
        }

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int id;
            byte key, rssi;
            double battlv;
            edata = new WVU800COMEventArgs();
            const int UART_BYTE_LEN = 7;
            int len = sp.BytesToRead;
            byte[] comdata = new byte[len];
            byte[] tmp = new byte[UART_BYTE_LEN];
            byte csum;
            sp.Read(comdata, 0, len);
            for (int j = 0; j < len; j++)
                rxData.Add(comdata[j]);
            bool processing = true;
            if (rxData.Count == 0) 
                processing = false;
            while (processing)
            {
                switch (rxData[0])
                {
                    case PAD_HEADER:
                            csum = 0;
                            if (rxData.Count < 7)
                            { 
                                processing = false;
                                break;
                            }
                            else
                            {
                                for (int i = 0; i < 7 - 1; i++)
                                {
                                    csum += rxData[i];
                                }
                                if (csum == rxData[6])
                                {
                                    id = (rxData[2] << 8) + rxData[1];
                                    key = rxData[5];

                                battlv = rxData[4];
                                //battlv = Math.Round(((dat[4] * 3.34f / 256) * 2), 2);

                                rssi = 0;
                                    if (rxData[3] >= 0x91)
                                        rssi = 5;
                                    else if (rxData[3] >= 0x88)
                                        rssi = 4;
                                    else if (rxData[3] >= 0x7F)
                                        rssi = 3;
                                    else if (rxData[3] >= 0x76)
                                        rssi = 2;
                                    else if (rxData[3] >= 0x6D)
                                        rssi = 1;

                                rssi = rxData[3];

                                edata.id.Add(id);
                                edata.val1.Add(key);
                                edata.battlv.Add(battlv);
                                edata.val2.Add(rssi);
                                    
                                }
                                rxData.RemoveRange(0, 7);
                                if (rxData.Count == 0)
                                    processing = false;
                            }
                            break;
                        case CU_HEADER:
                            csum = 0;
                            if (rxData.Count < 4)
                            {
                                processing = false;
                                break;
                            }
                            else
                            {
                                for (int i = 0; i < 4 - 1; i++)
                                {
                                    csum += rxData[i];
                                }
                                if (csum == rxData[3])
                                {
                                    id = 0;

                                edata.id.Add(id);
                                edata.val1.Add(rxData[2]); ;
                                edata.val2.Add(rxData[1]);
                                edata.battlv.Add(0);
                                }
                                rxData.RemoveRange(0, 4);
                                if (rxData.Count == 0)
                                processing = false; 
                            
                            }
                            break;
                        default:
                        if (rxData.Count > 0)
                        {
                            rxData.RemoveAt(0);
                            if (rxData.Count == 0)
                                processing = false;
                        }
                            break;
                    }           
            }
            if (edata.id.Count > 0)
                OnDataReceived(edata);
        }

        public void start_attend(int mode, bool attendnew)
        {
            byte attendmode = (byte)VoteMode.AttendSpecKey;
            switch (mode)
            {
                case 0:
                    attendmode = (byte)VoteMode.AttendSpecKey;
                    break;
                case 1:
                    attendmode = (byte)VoteMode.AttendAnyKey;
                    break;
                default:
                    break;

            }
            if (attendnew)
                attendmode += (byte)VoteMode.AttendNew;
            com_data(0, 0, attendmode);
        }

        public void start_vote(int mode, bool lastvalid, bool secret)
        {
            WVU800COM.VoteMode votemode;
            switch (mode)
            {
                case 0:
                    votemode = WVU800COM.VoteMode.Key3;
                    break;
                case 1:
                    votemode = WVU800COM.VoteMode.Key10;
                    break;
                case 2:
                    votemode = WVU800COM.VoteMode.Multi;
                    break;
                case 3:
                    votemode = WVU800COM.VoteMode.Rate;
                    break;
                case 4:
                    votemode = WVU800COM.VoteMode.CM1;
                    break;
                case 5:
                    votemode = WVU800COM.VoteMode.CM2;
                    break;
                case 6:
                    votemode = WVU800COM.VoteMode.CM3;
                    break;
                case 7:
                    votemode = WVU800COM.VoteMode.CM4;
                    break;
                default:
                    votemode = WVU800COM.VoteMode.Key10;
                    break;
            }
            if (lastvalid)
                votemode |= WVU800COM.VoteMode.LastValid;
            if (secret)
                votemode |= WVU800COM.VoteMode.Secret;
            com_data(0, VOTE_START, (byte)votemode);
        }

        public void Dispose()
        {
        }

        public void stop_vote()
        {
            start_scan();
        }

        public void start_scan()
        {
            com_data(0, 0, 0);
        }

        public void start_giveid(int fid, int tid)
        {
            byte fid1, fid2, tid1, tid2;
            // from id
            fid1 = (byte)((fid) & (0xFF));
            fid2 = (byte)(((fid) >> 8) & (0xFF));
            // to id
            tid1 = (byte)((tid) & (0xFF));
            tid2 = (byte)(((tid) >> 8) & (0xFF));
            com_data(0, GIVEID_START, fid1, fid2, tid1, tid2);
        }

        public void set_off_pad(int id)
        {
            com_data(id, SET_OFF_PAD, 0);
        }

        public void set_CUChannel(int chn)
        {
            com_data(0, SET_CU_CHANNEL, (byte)(chn));
        }

        public void set_pad_channel(int id, int chn)
        {
            com_data(id, SET_PAD_CHANNEL, (byte)(chn));
        }

        public void set_pad_attend(int id)
        {
            com_data(id, SET_PAD_ATTEND, 0xFF);
        }

        public void set_pad_absent(int id)
        {
            com_data(id, SET_PAD_ATTEND, 0);
        }

        public void set_PADID(int id, int cid)
        {
            byte a, b;
            a = (byte)(cid & 0xFF);
            b = (byte)((cid >> 8) & 0xFF);
            com_data(id, SET_PAD_ID, a, b);
        }

        public void set_TestKey(int id, byte key)
        {
            com_data(id, SET_PAD_TEST_KEY, key);
        }

        public void set_AutoChannel(int id)
        {
            com_data(id, SET_PAD_AUTO_CHANNEL, 0);
        }

        public void set_PADLED(int id, byte led)
        {
            com_data(id, SET_PAD_LED, led);
        }

        public void get_CUChannel()
        {
            com_data(GET_CU_CHANNEL);
        }

        public void set_pad_thershold(int id, int thers)
        {
            com_data(id, SET_PAD_THERSHOLD, (byte)thers);
        }

        public void set_cu_scansize(int scansize)
        {
            byte a = (byte)(scansize & 0xFF);
            byte b = (byte)((scansize >> 8) & 0xFF);
            com_data(0, SCAN_SIZE, a, b);
        }

        public void set_welcome(byte[] buf)
        {
            byte[] dat = new byte[20];
            dat[0] = 0x55;
            dat[1] = 0x44;
            dat[2] = SET_LCD_WELCOME;
            for (int i = 0; i < 16; i++)
            {
                dat[i + 3] = buf[i];
            }
            dat[19] = 0x35;
            if (sp.IsOpen)
            {
                sp.Write(dat, 0, 20);
                autoEvent.WaitOne(2000);
                //System.Threading.Thread.Sleep(20);
            }
        }

    }
}

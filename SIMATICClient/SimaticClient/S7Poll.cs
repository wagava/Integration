using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharp7;
using System.Threading;
using System.IO;

namespace SimaticClientService
{

    class S7Poll : IDisposable
    {
        #region Init Variable
        private System.Timers.Timer timerPoll;
        Thread PollThread;

        private int TryReadDataFromPLC_cnt = 0; //счетчик попыток чтения данных с ПЛК
        public bool DataReadIsGood;//внешний флаг для определения проблем с чтением данных с ПЛК
        public int TimerPeriod;
        int ConnRetryCntMax = 10;
        int ConnRetryCnt = 0;
        S7Client PLCclient;
        private PLCStatus plcstatus = PLCStatus.Disconnected;
        public string LastError;
        public double[] DataGot = new double[10];
        public List<Tag> Tags = new List<Tag>();

        WinLogger WinLog = new WinLogger(AppDomain.CurrentDomain.FriendlyName);

        private int SkipCnt = 3; //Количество раз запросов при возникновении ошибок
        private string _IP;
        private int _Rack;
        private int _Slot;
        private int prevPLCclient_LastError = 0;

        public enum PLCStatus
        {
            Connected,
            Disconnected,
            Error
        }
        #endregion


        #region Constructor
        public S7Poll(string IP, int Rack, int Slot, int period)
        {
            _IP = IP;
            _Rack = Rack;
            _Slot = Slot;
            PLCclient = new S7Client();
            LastError = "Class Created";
            WinLog.Write(2, $"S7Poll created. Period = {period}");


            if (this.ConnectTo(IP, Rack, Slot))
            {
                WinLog.Write(2, "PLC Connected");
                plcstatus = PLCStatus.Connected;
            }
            else
            {
                plcstatus = PLCStatus.Disconnected;
                WinLog.Write(2, "PLC is not connected");
            }

            TimerPeriod = period;
            PollThread = new Thread(new ThreadStart(InitTimer));
            PollThread.Start();
        }
        #endregion


        #region ConnectTo
        public bool ConnectTo(string address, int rack, int slot)
        {
            try
            {
                int res = PLCclient.ConnectTo(address, rack, slot);
                if (res == 0)
                {
                    plcstatus = PLCStatus.Connected;
                    return true;
                }
                else
                {
                    LastError = PLCclient.ErrorText(res);
                    WinLog.Write(2, $"PLC is not connected, {LastError}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }


        }
        #endregion


        #region Connect
        public bool Connect()
        {
            try
            {
                int res = PLCclient.Connect();
                if (res == 0)
                {
                    plcstatus = PLCStatus.Connected;
                    return true;
                }
                else
                {
                    LastError = PLCclient.ErrorText(res);
                    WinLog.Write(2, $"PLC is not connected, {LastError}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }
        #endregion


        #region Disconnect
        public bool Disconnect()
        {
            try
            {
                PLCclient.Disconnect();
                plcstatus = PLCStatus.Disconnected;
                WinLog.Write(1, $"PLC {_IP}, метод 'Disconnect', отключение от ПЛК...");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }
        #endregion


        public void SetConfigValue(string Name, int AddrDB, int AddrInDB, string Type)
        {
            Tag tag = new Tag(Name, AddrDB, AddrInDB, Type, null);
            Tags.Add(tag);
        }


        #region InitConfig
        public void InitConfig(List<Tag> tags)
        {
            WinLog.Write(3, $"InitConfig tags");
            Tags = tags;
        }
        #endregion


        #region ReadMultiVars
        public List<List<double>> ReadMultiVars(List<int> DBNumber, List<int> StartByteNum, List<string> Type, List<int> QDBNumber, List<int> QStartByteNum, List<string> QType)
        {
            var s7MultiVar = new S7MultiVar(PLCclient);
            //List<byte[]> db = new List<byte[]>();
            byte[] db1 = new byte[2];
            byte[] db2 = new byte[2];

            byte[] db1Q = new byte[2];
            byte[] db2Q = new byte[2];

            WinLog.Write(1, $"ReadMultiVars");

            for (int i = 0; i < DBNumber.Count; i++)
            {
                if (i == 0)
                {
                    if (Type[i] == "REAL")
                    {
                        Array.Resize(ref db1, 8);
                    }
                    else if (Type[i] == "BYTE")
                    {
                        //на будущее
                    }


                    s7MultiVar.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber[i], StartByteNum[i], db1.Length, ref db1);

                    if (QType[i] == "NONE")
                    {

                    }
                    else if (QType[i] == "BYTE")
                    {
                        s7MultiVar.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, QDBNumber[i], QStartByteNum[i], db1Q.Length, ref db1Q);
                    }
                }
                else if (i == 1)
                {
                    if (Type[i] == "REAL")
                    {
                        Array.Resize(ref db2, 8);
                    }
                    else if (Type[i] == "BYTE")
                    {
                        //на будущее
                    }


                    s7MultiVar.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber[i], StartByteNum[i], db2.Length, ref db2);

                    if (QType[i] == "NONE")
                    {

                    }
                    else if (QType[i] == "BYTE")
                    {
                        s7MultiVar.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, QDBNumber[i], QStartByteNum[i], db2Q.Length, ref db2Q);
                    }
                }
            }
            //чтение переменных
            List<double> resD = new List<double>();
            List<double> resQ = new List<double>();
            List<List<double>> resList = new List<List<double>>();
            int res = s7MultiVar.Read();

            if (res == 0)
            {
                if (!DataReadIsGood) DataReadIsGood = true;

                if (Type[0] == "REAL")
                {
                    resD.Add(S7.GetRealAt(db1, 0));
                }
                else if (Type[0] == "BYTE")
                {
                    byte tbyte = S7.GetByteAt(db1, 0);
                    resD.Add(Convert.ToDouble(tbyte));
                }
                else
                    resD.Add(Convert.ToDouble(S7.GetByteAt(db1, 0)));

                if (QType[0] == "BYTE")
                {
                    resQ.Add(Convert.ToDouble(S7.GetByteAt(db1Q, 0)));
                }
                else if (QType[0] == "NONE")
                {
                    resQ.Add(192);
                }
                else
                    resQ.Add(192);

                if (Type[1] == "REAL")
                {
                    resD.Add(S7.GetRealAt(db2, 0));
                }
                else if (Type[1] == "BYTE")
                {
                    byte tbyte = S7.GetByteAt(db2, 0);
                    resD.Add(Convert.ToDouble(tbyte));
                }
                else
                    resD.Add(Convert.ToDouble(S7.GetByteAt(db2, 0)));

                if (QType[1] == "BYTE")
                {
                    resQ.Add(Convert.ToDouble(S7.GetByteAt(db2Q, 0)));
                }
                else if (QType[1] == "NONE")
                {
                    resQ.Add(192);
                }
                else
                    resQ.Add(192);
            }
            else
            {
                TryReadDataFromPLC_cnt++;
                DataReadIsGood = false;
                WinLog.Write(2, "S7Poll: Error read data from PLC!");
                LastError = "Read Error!!!";
            }

            WinLog.Write(3, $"ReadMultiVars, return, resD = {resD[0]}, {resD[1]}, resQ = {resQ[0]}, {resQ[1]}");
            resList.Add(resD);
            resList.Add(resQ);
            return resList;
        }
        #endregion

        #region ReadDB
        public double ReadDb(int DBNumber, int StartByteNum, string Type)
        {
            double resD = 0;
            byte[] dbBuffer = null;
            int size = 1;
            if (Type == "REAL")
            {
                dbBuffer = new byte[8];
                size = 4;
            }
            else if (Type == "BYTE")
            {
                dbBuffer = new byte[2];
                size = 1;
            }
            prevPLCclient_LastError = PLCclient._LastError;

            try
            {
                int res = PLCclient.DBRead(DBNumber, StartByteNum, size, dbBuffer);

                if (PLCclient._LastError != 0)
                {
                    using (FileStream fstream = new FileStream($"c:\\Services\\Diagnostic.txt", FileMode.Append))
                    {
                        byte[] input = Encoding.Default.GetBytes($"1 - {DateTime.Now}:Ошибка при опросе {this._IP}, DB={DBNumber},addr={StartByteNum}, size={size}, buffer={dbBuffer}, PLCclient._LastError = {PLCclient._LastError} \n");
                        fstream.Write(input, 0, input.Length);
                    }
                }
                if (res == 0)
                {
                    if (!DataReadIsGood) DataReadIsGood = true;
                    if (Type == "REAL")
                    {
                        double dbtemp = S7.GetRealAt(dbBuffer, 0);
                        resD = dbtemp;
                    }
                    else if (Type == "BYTE")
                    {
                        byte dbtemp = S7.GetByteAt(dbBuffer, 0);
                        resD = Convert.ToDouble(dbtemp);
                    }
                }
                else
                {
                    TryReadDataFromPLC_cnt++;
                    DataReadIsGood = false;
                    WinLog.Write(2, "S7Poll: Error read data from PLC!");
                    LastError = "Read Error!!!";
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                WinLog.Write(1, ex.Message);
            }
            if ((PLCclient._LastError != 0) | (prevPLCclient_LastError != 0))
                return -99999;
            else
                return resD;

        }
        #endregion

        public void RunUpdate()
        {
            if (TryReadDataFromPLC_cnt > 2)//5 //Если пытались считать данные с ПЛК и ничего не получилось, пробуем переподключиться
            {
                TryReadDataFromPLC_cnt = 0;
                WinLog.Write(2, "Данные не читаются, попытка подключения...");
                this.Disconnect();
                this.Connect();
            }
            else
            {
                if (plcstatus == PLCStatus.Connected)
                {
                    if (SkipCnt > 0)
                        SkipCnt--;
                    if ((Tags.Count != 0) & (SkipCnt == 0))
                    {
                        #region multivar
                        /* List<int> addrs = new List<int>();
                         List<int> addrIndb = new List<int>();
                         List<string> type = new List<string>();

                         List<int> addrsQ = new List<int>();
                         List<int> addrIndbQ = new List<int>();
                         List<string> typeQ = new List<string>();

                         int i_end;
                         List<List<double>> resD = new List<List<double>>();
                         if ((i_iterTags + 2) >= (Tags.Count-1))  //2 - вычитка по 2 параметра
                             i_end = Tags.Count;
                         else
                             i_end = i_iterTags + 2; //2 - вычитка по 2 параметра

                         if (i_iterTags < i_end)
                         {
                             for (int i = i_iterTags; i < i_end; i++)
                             {

                                 addrs.Add(Tags[i].ItemDBAddr);
                                 addrIndb.Add(Tags[i].ItemAddrInDB);
                                 type.Add(Tags[i].ItemType);
                                 if (Tags[i].ItemQUse) {
                                     addrsQ.Add(Tags[i].ItemQDBAddr);
                                     addrIndbQ.Add(Tags[i].ItemQAddrInDB);
                                     typeQ.Add(Tags[i].ItemQType);
                                 }
                                 else
                                 {
                                     typeQ.Add("NONE");
                                 }    

                                 i_iterTags = i_iterTags + 1;
                             }

                             resD = ReadMultiVars(addrs, addrIndb, type, addrsQ, addrIndbQ, typeQ); //Получаем значения  для указанных переменных

                             for (int i = resD[0].Count - 1; i >= 0; i--) //распихиваем по структурам
                             {
                                 WinLog.Write(3, $"{Tags[i_end - 1].ItemAddr}: {resD[0][i]}, {resD[1][i]}");
                                 Tags[i_end - 1].ItemValue = resD[0][i];
                                 Tags[i_end - 1].ItemQValue = resD[1][i];
                                 i_end = i_end - 1;

                             }
                         }


                         if (i_iterTags == Tags.Count)
                             i_iterTags = 0;*/
                        #endregion multivar
                        #region readarea
                        /*  double res;
                          for (int i = 0; i < Tags.Count; i++)
                          {

                              res = this.ReadDb(Tags[i].ItemDBAddr, Tags[i].ItemAddrInDB, Tags[i].ItemType);
                              Tags[i].ItemValue = res;

                              res = Tags[i].ItemQUse ? this.ReadDb(Tags[i].ItemQDBAddr, Tags[i].ItemQAddrInDB, Tags[i].ItemQType) : 192;
                              Tags[i].ItemQValue = res;
                          }*/
                        #endregion readarea
                        double res;
                        double resQ;
                        for (int i = 0; i < Tags.Count; i++)
                        {

                            res = this.ReadDb(Tags[i].ItemDBAddr, Tags[i].ItemAddrInDB, Tags[i].ItemType);
                            if ((res != -99999) & (PLCclient._LastError == 0))
                            {
                                Tags[i].ItemValue = res;
                            }
                            else
                            {
                                if (PLCclient._LastError > 0) //Любая ошибка
                                {
                                    WinLog.Write(1, $"PLC {_IP} получил ошибку {PLCclient._LastError}, отключение от ПЛК...");
                                    this.Disconnect();
                                    break;
                                }

                                SkipCnt = 3;
                                break;

                            }

                            resQ = Tags[i].ItemQUse ? this.ReadDb(Tags[i].ItemQDBAddr, Tags[i].ItemQAddrInDB, Tags[i].ItemQType) : 192;
                            if ((resQ != -99999) & (PLCclient._LastError == 0))
                            {
                                Tags[i].ItemQValue = resQ;
                            }
                            else
                            {
                                if (PLCclient._LastError > 0) //Любая ошибка
                                {
                                    WinLog.Write(1, $"PLC {_IP} получил ошибку {PLCclient._LastError}, отключение от ПЛК...");
                                    this.Disconnect();
                                    break;
                                }
                                SkipCnt = 3;
                                break;
                            }
                        }

                    }

                    #region ReadbyOne
                    /* res = this.ReadDb(Tags[i_iterTags].ItemDBAddr, Tags[i_iterTags].ItemAddrInDB, Tags[i_iterTags].ItemType);
                     Tags[i_iterTags].ItemValue = res;

                     //res = this.ReadDb(Tags[i].ItemQDBAddr, Tags[i].ItemQAddrInDB, Tags[i].ItemQType);
                     res = Tags[i_iterTags].ItemQUse ? this.ReadDb(Tags[i_iterTags].ItemQDBAddr, Tags[i_iterTags].ItemQAddrInDB, Tags[i_iterTags].ItemQType) : 192;
                     Tags[i_iterTags].ItemQValue = res;

                     if (i_iterTags == Tags.Count - 1)
                         i_iterTags = 0;
                     else
                         i_iterTags++;*/
                    #endregion ReadbyOne
                }
                else
                {
                    if (ConnRetryCnt == ConnRetryCntMax)
                    {
                        WinLog.Write(2, $"{_IP}: PLCStatus.Disconnected. Попытка подключения...");
                        if (plcstatus == PLCStatus.Connected)
                            this.Disconnect();
                        this.Connect();
                        ConnRetryCnt = 0;
                    }
                    else
                    {
                        ConnRetryCnt++;
                    }
                }
            }
            //PollBusy = false;
        }


        public void Dispose() => PLCclient.Disconnect();

        //Для таймера. Периодический вызов потока для опроса и вычисления алгоритма
        protected void InitTimer()
        {
            try
            {
                timerPoll = new System.Timers.Timer();
                timerPoll.Interval = TimerPeriod;
                timerPoll.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
                timerPoll.Start();
            }
            catch (Exception ex)
            {
                WinLog.Write(1, $"InitTimer ex: " + ex.Message);
            }

        }


        private void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                //делаем опрос контроллеров
                this.RunUpdate();
            }
            catch (Exception ex)
            {
                WinLog.Write(1, $"OnTimer ex: " + ex.Message);
            }
        }
    }
}

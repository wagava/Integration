using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace SimaticClientService
{
    class MainModule : IDisposable
    {
        #region Init

        List<S7Poll> S7Polls = new List<S7Poll>();
        List<CalculateMethods> CalcMtds = new List<CalculateMethods>();
        int[] TimerChtForDataCalc;//счетчики, в зависимости от PollPeriod вызывается DataCalc не чаще чем период опроса

        WinLogger WinLog = new WinLogger(AppDomain.CurrentDomain.FriendlyName);

        List<List<Tag>> Tags = new List<List<Tag>>() { };
        int plcCount;


        #endregion


        public MainModule()
        {
            //чтение конфига, определение сколько контроллеров и данных
            Config_PLCPoll CFG = new Config_PLCPoll();
            plcCount = CFG.ReadConfigJSON();


            //добавление в S7Polls экземляров по количеству контроллеров
            for (int i = 0; i < CFG.lPLC.Count; i++)
            {
                S7Polls.Add(S7PollAdd(i, CFG.lPLC.Values.ElementAt(i).addr, CFG.lPLC.Values.ElementAt(i).rack, CFG.lPLC.Values.ElementAt(i).slot, CFG.lPLC.Values.ElementAt(i).period, CFG.Tags));
            }
            TimerChtForDataCalc = new int[CFG.lPLC.Count];
            for (int i = 0; i < S7Polls.Count; i++)
            {
                CalcMtds.Add(new CalculateMethods(S7Polls[i].Tags));
                TimerChtForDataCalc[i] = 0;
            }
            //вычисляем ссылки на теги и передаем в соотв. тег
            for (int i = 0; i < S7Polls.Count; i++) //см.выше
            {
                for (int j = 0; j < S7Polls[i].Tags.Count; j++)
                {
                    if (!string.IsNullOrEmpty(S7Polls[i].Tags[j].ItemTagLink))
                    {
                        string[] Strsplitting = { };
                        //определяем теги
                        if (S7Polls[i].Tags[j].ItemTagLink.Contains(","))
                        {
                            Strsplitting = S7Polls[i].Tags[j].ItemTagLink.Split(new string[] { "," }, StringSplitOptions.None);
                        }
                        else
                        {
                            Strsplitting = new string[] { S7Polls[i].Tags[j].ItemTagLink };
                        }

                        for (int i1 = 0; i1 < S7Polls.Count; i1++) //см.выше
                        {
                            for (int j1 = 0; j1 < S7Polls[i1].Tags.Count; j1++)
                            {
                                var res = Strsplitting.Intersect(new string[] { S7Polls[i1].Tags[j1].ItemName });  //проверка на вхождение строки в элементы массива
                                if (res.Any())
                                {
                                    S7Polls[i].Tags[j].ItemTagRef.Add(S7Polls[i1].Tags[j1]);
                                }
                            }
                        }
                    }
                }

            }

            CFG.Dispose();

        }

        private S7Poll S7PollAdd(int idx, string IP, int Rack, int Slot, int pollperiod, List<List<Tag>> listTags)
        {

            S7Poll s7 = new S7Poll(IP, Rack, Slot, pollperiod);
            s7.InitConfig(listTags[idx]);
            Tags.Add(listTags[idx]);
            return s7;
        }

        //расчет данных
        protected void DataCalc(int idx, List<Tag> tags)
        {
            //вызов статического метода класса для обсчета
            //методы: FlowMeterData - Фиксируется останов изменения значения в течении 20 секунд 
            try
            {
                if (TimerChtForDataCalc[idx] == S7Polls[idx].TimerPeriod)
                {
                    if (tags[0].ItemValue != null)
                    {
                        CalcMtds[idx].MainCalculate(tags);
                        //если buf2Send не нулевой, отправляем в 1С
                        for (int i = 0; i < tags.Count; i++)
                        {
                            if ((CalcMtds[idx].buf2Send[i, 2] != -99999) & (!CalcMtds[idx].FstSn))
                            {
                                double StartCnt = CalcMtds[idx].buf2Send[i, 0];
                                double EndCnt = CalcMtds[idx].buf2Send[i, 1];
                                double DeltaCnt = CalcMtds[idx].buf2Send[i, 2];
                                CalcMtds[idx].buf2Send[i, 0] = -99999;//сбрасываем значение
                                CalcMtds[idx].buf2Send[i, 1] = -99999;//сбрасываем значение
                                CalcMtds[idx].buf2Send[i, 2] = -99999;//сбрасываем значение
                                CalcMtds[idx].buf2Send[i, 3] = -99999;//сбрасываем значение
                                CalcMtds[idx].buf2Send[i, 4] = -99999;//сбрасываем значение

                                WinLog.Write(2, $"Тег {tags[i].ItemName} подготовлен для отправки {DeltaCnt}");

                                using (FileStream fstream = new FileStream($"c:\\Services\\FileTrace.txt", FileMode.Append))
                                {
                                    byte[] input = Encoding.Default.GetBytes($"{DateTime.Now}: ({tags[i].ItemName}) StartCnt/EndCnt/Delta: {StartCnt} / {EndCnt} / {DeltaCnt} \n");

                                    fstream.Write(input, 0, input.Length);
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WinLog.Write(2, $"DataCalc : {ex.Message}");
            }
            TimerChtForDataCalc[idx] = TimerChtForDataCalc[idx] >= S7Polls[idx].TimerPeriod ? 0 : TimerChtForDataCalc[idx] + 1000;
        }


        //опрос данных
        protected void PLCPoll() { }

        //Метод перидического выполнения
        public void RunModule() //вызывается из Service1.cs каждую секунду
        {
            if (S7Polls.Count != 0)
            {
                for (int i = 0; i < S7Polls.Count; i++)
                {
                    if (S7Polls[i].DataReadIsGood)
                        DataCalc(i, S7Polls[i].Tags);
                }
            }
            else
            {
                WinLog.Write(1, "Нет опрашиваемых устройств! Проверьте конфигурацию.");
            }

        }


        public void SaveBufferValues()
        {
            //сохраняем значения из буфера в файл
            try
            {
                for (int i = 0; i < CalcMtds.Count; i++)
                {
                    double[,] dbuf = CalcMtds[i].GetbufArray();
                    using (FileStream fstream = new FileStream($"c:\\Services\\{i}_CalcMtds.txt", FileMode.Create))//OpenOrCreate
                    {
                        byte[] input = Encoding.Default.GetBytes($"{string.Join("|", dbuf.Cast<double>())}");
                        fstream.Write(input, 0, input.Length);
                    }
                    using (FileStream fstream = new FileStream($"c:\\Services\\{i}_CalcMtdsbuf.txt", FileMode.Create))//OpenOrCreate
                    {
                        byte[] input = Encoding.Default.GetBytes($"{string.Join("|", CalcMtds[i].buf2Send.Cast<double>())}");
                        fstream.Write(input, 0, input.Length);
                    }

                }
            }
            catch (Exception ex)
            { WinLog.Write(1, $"WriteFile ex: " + ex.Message); }
        }


        public async void OpenBufferValues()
        {
            WinLog.Write(1, $"OpenBufferValues. CalcMtds.Count = {CalcMtds.Count}");
            //загружаем значения из файла в буфер
            for (int i = 0; i < CalcMtds.Count; i++)
            {
                try
                {
                    if (File.Exists($"c:\\Services\\{i}_CalcMtds.txt"))
                    {
                        using (FileStream fstream = new FileStream($"c:\\Services\\{i}_CalcMtds.txt", FileMode.OpenOrCreate))
                        {
                            fstream.Seek(0, SeekOrigin.Begin);
                            byte[] output = new byte[fstream.Length];
                            await fstream.ReadAsync(output, 0, output.Length);
                            string textFromFile = Encoding.Default.GetString(output);
                            string[] values = textFromFile.Split(new char[] { '|' });
                            double[,] buf = new double[Tags[i].Count, 20];
                            int row = 0;
                            int col = 0;
                            for (int j = 0; j < values.Length; j++)
                            {
                                if ((Tags[i].Count * 20) >= j)
                                {
                                    col = j % 20;
                                    row = j / 20;
                                    buf[row, col] = Convert.ToDouble(values[j]);
                                }
                            }
                            CalcMtds[i].SetbufArray(buf);
                        }
                    }
                    else
                    {
                        WinLog.Write(1, $"Файла c:\\Services\\{i}_CalcMtds.txt не существует!");

                        double[,] tempD = new double[Tags[i].Count, 20]; //- 99999
                        for (int k = 0; k < Tags[i].Count; k++)
                            for (int k1 = 0; k1 < 20; k1++)
                                tempD[k, k1] = -99999;
                        CalcMtds[i].SetbufArray(tempD);//передаем 'пустой' массив
                    }
                }
                catch (Exception ex)
                {
                    WinLog.Write(1, $"ReadFile ex1: " + ex.Message);
                    double[,] tempD = new double[Tags[i].Count, 20];
                    for (int k = 0; k < Tags[i].Count; k++)
                        for (int k1 = 0; k1 < 20; k1++)
                            tempD[k, k1] = -99999;
                }

                try
                {
                    if (File.Exists($"c:\\Services\\{i}_CalcMtdsbuf.txt"))
                    {
                        using (FileStream fstream = new FileStream($"c:\\Services\\{i}_CalcMtdsbuf.txt", FileMode.OpenOrCreate))
                        {
                            fstream.Seek(0, SeekOrigin.Begin);
                            byte[] output = new byte[fstream.Length];
                            await fstream.ReadAsync(output, 0, output.Length);
                            string textFromFile = Encoding.Default.GetString(output);
                            string[] values = textFromFile.Split(new char[] { '|' });
                            double[,] buf = new double[Tags[i].Count, 5];
                            int row = 0;
                            int col = 0;
                            for (int j = 0; j < values.Length; j++)
                            {
                                if ((Tags[i].Count * 5) >= j)
                                {
                                    col = j % 5;
                                    row = j / 5;
                                    buf[row, col] = Convert.ToDouble(values[j]);
                                }
                            }
                            CalcMtds[i].buf2Send = buf;
                        }
                    }
                    else
                    {
                        WinLog.Write(1, $"Файла c:\\Services\\{i}_CalcMtdsbuf.txt не существует!");

                        double[,] tempD = new double[Tags[i].Count, 5]; //- 99999
                        for (int k = 0; k < Tags[i].Count; k++)
                            for (int k1 = 0; k1 < 5; k1++)
                                tempD[k, k1] = -99999;
                        CalcMtds[i].buf2Send = tempD;
                    }
                }
                catch (Exception ex)
                {
                    WinLog.Write(1, $"ReadFile ex2: " + ex.Message);
                    double[,] tempD = new double[Tags[i].Count, 20]; //- 99999
                    for (int k = 0; k < Tags[i].Count; k++)
                        for (int k1 = 0; k1 < 20; k1++)
                            tempD[k, k1] = -99999;

                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

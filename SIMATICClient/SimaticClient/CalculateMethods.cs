using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimaticClientService
{
    class CalculateMethods : IDisposable
    {
        #region Init
        List<Tag> Tags = new List<Tag>();
        private WinLogger WinLog = new WinLogger(AppDomain.CurrentDomain.FriendlyName);
        double[,] bufArray; //двумерный массив, куда записываются последние значения
        public double[,] buf2Send;//буфер для отправки 1-Значение начала отсчета, 2-значение окончания отсчета, 3-Разница
        private bool InitEnd = false; //окончание инициализации буфера из файла
        public bool FstSn;
        private int[,] bufdep; //двумерный массив, куда записываются зависимости от тегов
        private int[] ErrCntForLog;

        #endregion


        #region Constructor
        public CalculateMethods(List<Tag> tags) //если не будет использоваться, лучше передать только количество
        {
            Tags = tags;
            bufArray = new double[Tags.Count, 20];
            buf2Send = new double[Tags.Count, 5];//1-начало отсчета, 2-конец отсчета, 3-разница, 4/5 - дополнительные параметры
            bufdep = new int[Tags.Count, 2]; //Нужен только один элемент, но для будущего использование закладываем больше
            ErrCntForLog = new int[Tags.Count];

            for (int k = 0; k < Tags.Count; k++)
                for (int k1 = 0; k1 < 5; k1++)
                    buf2Send[k, k1] = -99999;

            for (int i = 0; i < Tags.Count; i++) //ищем зависимости и запоминаем
            {
                if (!string.IsNullOrEmpty(Tags[i].ItemTagLink))
                {
                    for (int j = 0; j < Tags.Count; j++)
                    {
                        if (Tags[j].ItemName == Tags[i].ItemTagLink)
                            bufdep[i, 0] = j;
                    }

                }
            }
            //Определение зависимостей тегов по TagLink и указание их ID
            FstSn = true;
        }
        #endregion
        public double[,] GetbufArray()
        {
            return bufArray;
        }


        public void SetbufArray(double[,] buf)
        {
            bufArray = buf;
            InitEnd = true;
            WinLog.Write(3, $"SetbufArray = {string.Join(" | ", bufArray.Cast<double>())}");
        }


        public bool MainCalculate(List<Tag> tags)
        {
            //заполняем буфер
            if (InitEnd)
            {
                try
                {//для каждого тега получаем значение и формируем новый буфер, помещая новое значение в буфер и сдвигая его на один стек
                    for (int i = 0; i < tags.Count; i++)
                    {
                        if (Convert.ToByte(tags[i].ItemQValue) != 0)//== 192)
                        {
                            for (int j = 19; j != 0; j--)
                                bufArray[i, j] = bufArray[i, j - 1];
                            bufArray[i, 0] = (double)tags[i].ItemValue;
                        }
                        else
                        {
                        }
                    }
                    //проверяем по алгоритму и записываем результат
                    for (int i = 0; i < tags.Count; i++)
                    {

                        if (tags[i].ItemFunc == "MeterData")
                        {
                            FixDataChanged(i, bufArray);
                        }
                        else if (tags[i].ItemFunc == "ValueChanged")
                        {
                            ValueChanged();
                        }
                    }
                }
                catch (Exception ex)
                {
                    WinLog.Write(1, ex.Message);
                }
                WinLog.Write(3, $"bufArray = {string.Join(" | ", bufArray.Cast<double>())}");
            }
            return true;
        }

        private void MeterData(int i, double[,] _bufarray) //FlowMeterData
        {

            double[,] _BufArray;
            bool valchangedStart = false;
            bool valchangedStop = false;
            int valCnt1 = 0;
            int valCnt2 = 0;
            double val = -99999;
            _BufArray = _bufarray;// new double[Tags.Count - 1, 20];
            //2 события:
            //1) Когда все числа одинаковые и прилетает новое число - первое
            //2) Только последний элемент не равен всем остальным

            if (FstSn)
                FstSn = false;
            valCnt1 = 0;
            valCnt2 = 0;
            if (Convert.ToByte(Tags[i].ItemQValue) >= 128) // ручной режим = 128, ниже плохое качество
            {
                for (int j = 19; j != 0; j--)
                {
                    //1)-----------------------------------------
                    if (_BufArray[i, j] == _BufArray[i, j - 1])
                        valCnt1++;
                    if ((valCnt1 == 18) & (_BufArray[i, 1] != _BufArray[i, 0]))
                    {
                        valchangedStart = true;
                        val = _BufArray[i, 1];//первое измененное значение. Фиксируем значение, которое было до изменения
                        WinLog.Write(1, $"первое измененное значение {_BufArray[i, 0]}");
                    }
                    //2)-----------------------------------------
                    if (j > 1)
                    {
                        if (_BufArray[i, j - 1] == _BufArray[i, j - 2])
                            valCnt2++;
                        if ((valCnt2 == 18) & (_BufArray[i, 19] != _BufArray[i, 18]))
                        {
                            valchangedStop = true;
                            val = _BufArray[i, 18];//последнее измененное значение
                            WinLog.Write(1, $"последнее измененное значение {_BufArray[i, 18]}");
                        }
                    }

                }
                //триггер для отправки данных
                if (valchangedStart | valchangedStop)
                {
                    WinLog.Write(1, $"val для передачи {val}");

                    if (valchangedStart)
                    {
                        buf2Send[i, 0] = val; //первое значение после "простоя" значения
                    }
                    else if (valchangedStop)
                    {
                        buf2Send[i, 1] = val; //последнее значение после фиксации "простоя" значения
                        if (buf2Send[i, 0] != -99999)
                            buf2Send[i, 2] = buf2Send[i, 1] - buf2Send[i, 0];
                    }
                    WinLog.Write(1, $"buf2Send[{i}] для передачи {buf2Send[i, 0]}, {buf2Send[i, 1]}, {buf2Send[i, 2]}");
                    valchangedStart = false;
                    valchangedStop = false;
                    val = 0;

                }
            }
            else
            {//каждые 30 секунд пишем в лог сообщение о плохом качестве сигнала
                if (ErrCntForLog[i] < 30)
                    ErrCntForLog[i]++;
                else
                {
                    WinLog.Write(1, $"Тег {Tags[i].ItemName} имеет плохое качество {Convert.ToByte(Tags[i].ItemQValue)}");
                    ErrCntForLog[i] = 0;
                }
            }

        }


        private void FixDataChanged(int i, double[,] _bufarray)//Use Condition
        {
            double valD = -99999;
            valD = _bufarray[i, 0];

            if (FstSn)
                FstSn = false;

            if (Tags[i].ItemTagLink != "") //если линк на тег не пустой - выполянем
            {

                if (Tags[i].ItemTagLink.Contains(","))
                {
                    double[] d_resCalcParam = new double[Tags[i].ItemTagRef.Count]; //результат вычислений по правилам тегов

                    for (int j = 0; j < Tags[i].ItemTagRef.Count; j++)
                    {

                        if (Convert.ToByte(Tags[i].ItemTagRef[j].ItemQValue) >= 128) // ручной режим = 128, ниже плохое качество
                        {
                            if (!string.IsNullOrEmpty(Tags[i].ItemTagRef[j].ItemParamRule))
                            {
                                Dictionary<bool, bool> res = new Dictionary<bool, bool>();

                                int ParVal = Convert.ToInt32(Tags[i].ItemTagRef[j].ItemValue); //после отладки заменить комментом

                                if (Tags[i].ItemTagRef[j].ItemParamRule.Contains("bool"))
                                {
                                    res = TagRules.TagBoolCheck(ParVal, Tags[i].ItemTagRef[j].ItemParamRule);

                                    if (res.Keys.ElementAt(0) & res.Values.ElementAt(0))//обработка правила прошла успешно и триггер выставлен
                                        d_resCalcParam[j] = 1;
                                    else if (res.Keys.ElementAt(0) & !res.Values.ElementAt(0))//обработка правила прошла успешно и триггер сброшен
                                        d_resCalcParam[j] = 0;
                                }
                                else if (Tags[i].ItemTagRef[j].ItemParamRule.Contains("int"))
                                { }
                                else if (Tags[i].ItemTagRef[j].ItemParamRule.Contains("real"))
                                { }
                            }
                            else //если правила нет
                            {
                                d_resCalcParam[j] = Convert.ToDouble(Tags[i].ItemTagRef[j].ItemValue);
                            }
                        }
                        else
                        {//каждые 30 секунд пишем в лог сообщение о плохом качестве сигнала
                            if (ErrCntForLog[i] < 30)
                                ErrCntForLog[i]++;
                            else
                            {
                                WinLog.Write(1, $"Тег {Tags[i].ItemTagRef[j].ItemName} имеет плохое качество {Convert.ToByte(Tags[i].ItemTagRef[j].ItemQValue)}");
                                ErrCntForLog[i] = 0;
                            }
                        }
                    }
                    //вычисление по основному тегу
                    Dictionary<double, string> resD = new Dictionary<double, string>();
                    resD = TagRules.TagBoolCompare(d_resCalcParam, Tags[i].ItemParamRule); //передаем значения тегов по порядку и само правило
                    double resComparing = resD.Keys.ElementAt(0);//1: передаются вычисленные значения по правилам и без. 2: Передается булевая логика


                    if (resComparing == -99999)//обработка правила прошла успешно и триггер выставлен
                    {
                        WinLog.Write(1, $"Ошибка вычисления: {resD.Values.ElementAt(0)}");
                    }
                    else if (resComparing != 0)//обработка правила прошла успешно - результат вычисления True
                    {
                        if (buf2Send[i, 0] == -99999) //записываем первый счетчик
                        {
                            buf2Send[i, 0] = valD;
                            WinLog.Write(1, $"Счетчик начальный:{valD}");
                        }
                    }
                    else if (resComparing == 0)//обработка правила прошла успешно и триггер сброшен
                    {
                        if ((buf2Send[i, 0] != -99999) & (buf2Send[i, 1] == -99999)) //записываем второй счетчик
                        {
                            buf2Send[i, 1] = valD;
                            buf2Send[i, 2] = buf2Send[i, 1] - buf2Send[i, 0];// - buf2Send[i, 3];
                            WinLog.Write(1, $"Счетчик конечный: {valD} , Разница: {buf2Send[i, 2]}");
                        }
                    }
                }
                else
                {

                }
            }
            else
            {
                WinLog.Write(1, $"Тег {Tags[i].ItemName} имеет пустую ссылку!");
            }

        }



        private void ValueChanged()
        { }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

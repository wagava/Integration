using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
//using NCalc; //библиотека для вычисления формулы из строки

namespace SimaticClientService
{
    class TagRules : IDisposable
    {
        public string ErrorMessage = "";
        #region Constructor
        TagRules() { }
        #endregion Constructor


        public static Dictionary<bool, bool> TagBoolCheck(int val, string rule) //возвращаемое значение - <результат выполнения метода, значение после обработки>
        {
            Dictionary<bool, bool> returnRes = new Dictionary<bool, bool>();
            bool resB = false;
            string strRule = rule.Replace(" ", "");
            string[] RuleSplitting = { };

            if (strRule.Contains("|"))
                RuleSplitting = strRule.Split(new string[] { "|" }, StringSplitOptions.None);
            else //в дальнейшем можно будет добавить еще условия для правил
                RuleSplitting = strRule.Split(new string[] { "|" }, StringSplitOptions.None);

            if (RuleSplitting != null)
            {
                for (int i = 0; i < RuleSplitting.Count(); i++)
                {
                    if (RuleSplitting[i].ToLower().Contains("bool:"))
                    {

                        RuleSplitting[i] = RuleSplitting[i].Substring(5);// 0, strRule.Length - 5);

                        var getNumbers = (from t in RuleSplitting[i]
                                          where char.IsDigit(t)
                                          select t).ToArray();

                        int resParseInt = Int32.Parse(new string(getNumbers));
                        bool lresB = false;

                        //все, что выше (bool,x,eq...,значение для сравнения - записывается в буфер в конструкторе
                        if (strRule.ToLower().Contains("eq"))//равно
                        {
                            lresB = (val == resParseInt ? true : false);
                        }
                        else if (strRule.ToLower().Contains("gt"))//больше
                        {
                            lresB = (val > resParseInt ? true : false);
                        }
                        else if (strRule.ToLower().Contains("lt"))//меньше
                        {
                            lresB = (val < resParseInt ? true : false);
                        }
                        else if (strRule.ToLower().Contains("ge"))//больше или равно
                        {
                            lresB = (val >= resParseInt ? true : false);
                        }
                        else if (strRule.ToLower().Contains("le"))//меньше или равно
                        {
                            lresB = (val <= resParseInt ? true : false);
                        }
                        else if (strRule.ToLower().Contains("x"))//проверяем конкретный бит
                        {
                            lresB = (((val >> resParseInt) & 1) != 0 ? true : false);
                        }
                        resB = resB | lresB;

                    }
                    else
                    {
                        returnRes.Add(false, resB);
                        return returnRes;
                    }
                }
            }
            else
            {
                returnRes.Add(false, resB);
                return returnRes;
            }

            returnRes.Add(true, resB);
            return returnRes;
        }


        public static Dictionary<double, string> TagBoolCompare(double[] val, string rule) //возвращаемое значение - результат вычисления
        {

            Dictionary<double, string> returnRes = new Dictionary<double, string>();
            double resD = -99999;
            string RuleStr = rule;
            List<string> strParsed = new List<string>();
            string itemstr = "";
            string tempstr = "";
            try
            {
                RuleStr = RuleStr.Replace(" ", "");
                //подменяем идексы в правиле значениями вычисленных тегов. Т.к. булевые значения, сравниваем с нулем
                tempstr = tempstr + "1, ";
                for (int j = 0; j < val.Length; j++)
                    RuleStr = RuleStr.Replace((j + 1).ToString(), val[j] == 0 ? "False" : "True");
                tempstr = tempstr + "2 " + RuleStr + ",";
                for (int i = 0; i < RuleStr.Length; i++)
                {
                    tempstr = tempstr + "3, ";
                    string symbol = RuleStr.Substring(i, 1);
                    itemstr = itemstr + symbol;

                    if (new[] { ")", "(" }.Any(symbol.Contains))
                    {
                        if (itemstr.Length > 1)
                        {
                            strParsed.Add(itemstr.Substring(0, itemstr.Length - 1));
                            strParsed.Add(itemstr.Substring(itemstr.Length - 1, 1));
                        }
                        else
                            strParsed.Add(itemstr);
                        itemstr = "";
                    }
                    else
                    { }
                }
                if (itemstr != "")
                    strParsed.Add(itemstr);
                bool fExit = false;
                string mathOper = "";
                while (!fExit)
                {
                    tempstr = tempstr + "while, ";
                    for (int i = 0; i < strParsed.Count; i++)
                    {
                        int cnt = 0;

                        if ((strParsed[i] == ")"))
                        {
                            for (int j = i - 1; j >= 0; j--)
                            {
                                string symbols = strParsed[j];

                                if (!(new[] { ")", "(", }.Any(symbols.Contains)))
                                {
                                    if (mathOper != "")
                                    {
                                        strParsed.RemoveAt(j + 1);
                                        cnt++;
                                    }
                                    mathOper = symbols + mathOper;
                                    //функция вычисления
                                }
                                else
                                {
                                    strParsed[i - cnt - 1] = CalcBitOperation(mathOper).ToString();//функция вычисления
                                    tempstr = tempstr + strParsed[i - cnt - 1] + ", ";
                                    strParsed.RemoveAt(i - cnt);
                                    strParsed.RemoveAt(j);
                                    mathOper = "";
                                    break;

                                }
                            }
                        }
                        else if ((!strParsed.Contains(")")))
                        {
                            string symbols = strParsed[i];

                            if (symbols != "")
                            {
                                mathOper = mathOper + symbols;
                                if (strParsed.Count - 1 == i)
                                {

                                    strParsed.Clear();
                                    tempstr = tempstr + CalcBitOperation(mathOper).ToString() + ", ";
                                    strParsed.Add(CalcBitOperation(mathOper) == true ? "1" : "0"); //функция вычисления
                                    break;
                                }
                            }
                        }
                    }
                    if (!strParsed.Contains(")") & (strParsed.Count <= 1))
                    {
                        tempstr = tempstr + ": EXIT -> " + strParsed[0];
                        resD = Convert.ToDouble(strParsed[0]);
                        returnRes.Add(Convert.ToDouble(strParsed[0]), tempstr + " = ok");
                        fExit = true;
                    }

                }


            }
            catch (Exception ex)
            {
                returnRes.Add(0, $"Error: {ex.Message}, {tempstr}");
            }




            return returnRes;
        }


        private static bool CalcBitOperation(string mathOper)
        {
            //функция вычисления
            bool res = false;
            try
            {
                if (mathOper.Contains("&") & mathOper.Contains("|"))
                {
                    string[] parsStrVal;
                    string[] parsStrSign;
                    //string[] parsStrOR;
                    parsStrSign = mathOper.Split(new string[] { "true", "false", "True", "False" }, StringSplitOptions.None);
                    parsStrSign = parsStrSign.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    parsStrVal = mathOper.Split(new string[] { "&", "|" }, StringSplitOptions.None);
                    for (int i1 = 0; i1 < parsStrVal.Length - 1; i1++)
                    {
                        if (parsStrSign[i1] == "&")
                            res = i1 == 0 ? res = Convert.ToBoolean(parsStrVal[i1]) & Convert.ToBoolean(parsStrVal[i1 + 1]) : res = res & Convert.ToBoolean(parsStrVal[i1 + 1]);
                        else if (parsStrSign[i1] == "|")
                            res = i1 == 0 ? res = Convert.ToBoolean(parsStrVal[i1]) | Convert.ToBoolean(parsStrVal[i1 + 1]) : res = res | Convert.ToBoolean(parsStrVal[i1 + 1]);
                    }
                }
                else if (mathOper.Contains("&"))
                {
                    string[] parsStr;
                    // bool res;
                    parsStr = mathOper.Split(new string[] { "&" }, StringSplitOptions.None);

                    for (int i1 = 0; i1 < parsStr.Length - 1; i1++)
                        res = i1 == 0 ? res = Convert.ToBoolean(parsStr[i1]) & Convert.ToBoolean(parsStr[i1 + 1]) : res = res & Convert.ToBoolean(parsStr[i1 + 1]);
                }
                else if (mathOper.Contains("|"))
                {
                    string[] parsStr;

                    parsStr = mathOper.Split(new string[] { "|" }, StringSplitOptions.None);
                    for (int i1 = 0; i1 < parsStr.Length - 1; i1++)
                        res = i1 == 0 ? res = Convert.ToBoolean(parsStr[i1]) | Convert.ToBoolean(parsStr[i1 + 1]) : res = res | Convert.ToBoolean(parsStr[i1 + 1]);
                }
            }
            catch (Exception ex)
            {

            }
            return res;
        }


        public void Dispose()
        {

        }
    }
}

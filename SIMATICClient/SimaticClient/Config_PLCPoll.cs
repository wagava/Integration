using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace SimaticClientService
{
    class Config_PLCPoll : IDisposable
    {

        WinLogger WinLog = new WinLogger(AppDomain.CurrentDomain.FriendlyName);


        #region Constructor
        public Config_PLCPoll()
        {

        }
        #endregion


        public Dictionary<int, PLC> lPLC = new Dictionary<int, PLC>();
        public List<List<Tag>> Tags = new List<List<Tag>>() { }; //список опрашиваемых тегов для каждого контроллера в S7Polls

        public struct PLC
        {
            public string addr;
            public int rack;
            public int slot;
            public int period;
        }

        public int ReadConfigJSON()
        {
            try
            {
                FileInfo fInfo = new FileInfo("c:\\Services\\ServiceCfg.json");
                FileStream filestream = fInfo.OpenRead();

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Config_JSONFormat.Root));
                Config_JSONFormat.Root library = (Config_JSONFormat.Root)jsonSerializer.ReadObject(filestream);


                //разбор
                for (int i = 0; i < library.PLC.NetworkSettings_[0].Count; i++)
                {

                    PLC PLCData = new PLC();

                    PLCData.addr = library.PLC.NetworkSettings_[0][i].Address;
                    PLCData.rack = library.PLC.NetworkSettings_[0][i].Rack;
                    PLCData.slot = library.PLC.NetworkSettings_[0][i].Slot;
                    PLCData.period = library.PLC.NetworkSettings_[0][i].PollPeriod;

                    lPLC.Add(i, PLCData);

                }

                for (int i = 0; i < library.Tags.Tags_.Count; i++)
                {
                    List<Tag> ltag = new List<Tag>();
                    for (int j = 0; j < library.Tags.Tags_[i].Count; j++)
                    {

                        Tag tag = new Tag(library.Tags.Tags_[i][j].Name,
                            library.Tags.Tags_[i][j].ValAddr,
                            library.Tags.Tags_[i][j].ValOffset,
                            library.Tags.Tags_[i][j].ValType,
                            null,
                            library.Tags.Tags_[i][j].QualityUse,
                            library.Tags.Tags_[i][j].QualityAddr,
                            library.Tags.Tags_[i][j].QualityOffset,
                            library.Tags.Tags_[i][j].QualityType,
                            null,
                            library.Tags.Tags_[i][j].Destination1C,
                            library.Tags.Tags_[i][j].Source1C,
                            library.Tags.Tags_[i][j].Function,
                            library.Tags.Tags_[i][j].FunctionParNum,
                            library.Tags.Tags_[i][j].ParamRule,
                            library.Tags.Tags_[i][j].TagLink,
                            new List<Tag>());

                        ltag.Add(tag);
                    }
                    Tags.Add(ltag);
                }


            }
            catch (Exception ex)
            {
                WinLog.Write(1, "Config_PLCPoll: " + ex.Message);
            }
            return lPLC.Count;
        }

        public void Dispose()
        {
            WinLog.Write(2, "Config_PLCPoll.Dispose()");
        }



    }
}

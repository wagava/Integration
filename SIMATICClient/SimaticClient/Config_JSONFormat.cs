using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;


namespace SimaticClientService
{
    class Config_JSONFormat
    {
        [DataContract]
        public class PLC
        {
            [DataMember(Name = "NetworkSettings")]
            public List<List<NetworkSettings>> NetworkSettings_ { get; set; }
        }
        [DataContract]
        public class Root
        {

            [DataMember(Name = "PLC")]
            public PLC PLC { get; set; }
            [DataMember(Name = "Tags")]
            public Tags Tags { get; set; }
        }
        [DataContract]
        public class Tags
        {
            [DataMember(Name = "PLC")]
            public List<List<TG>> Tags_ { get; set; }
        }


        [DataContract]
        public class TG
        {
            [DataMember(Name = "Name")]
            public string Name { get; set; }

            [DataMember(Name = "ValAddr")]
            public int ValAddr { get; set; }

            [DataMember(Name = "ValOffset")]
            public int ValOffset { get; set; }

            [DataMember(Name = "ValType")]
            public string ValType { get; set; }

            [DataMember(Name = "QualityUse")]
            public bool QualityUse { get; set; }

            [DataMember(Name = "QualityAddr")]
            public int QualityAddr { get; set; }

            [DataMember(Name = "QualityOffset")]
            public int QualityOffset { get; set; }

            [DataMember(Name = "QualityType")]
            public string QualityType { get; set; }

            [DataMember(Name = "Destination1C")]
            public string Destination1C { get; set; }

            [DataMember(Name = "Source1C")]
            public string Source1C { get; set; }

            [DataMember(Name = "Function")]
            public string Function { get; set; }

            [DataMember(Name = "FunctionParNum")]
            public byte FunctionParNum { get; set; }

            [DataMember(Name = "ParamRule")]
            public string ParamRule { get; set; }

            [DataMember(Name = "TagLink")]
            public string TagLink { get; set; }
        }


        [DataContract]
        public class NetworkSettings
        {
            [DataMember(Name = "Address")]
            public string Address { get; set; }

            [DataMember(Name = "Rack")]
            public int Rack { get; set; }

            [DataMember(Name = "Slot")]
            public int Slot { get; set; }

            [DataMember(Name = "PollPeriod")]
            public int PollPeriod { get; set; }

        }

        [DataContract]
        public class Settings
        {
            [DataMember(Name = "Url")]
            public string Url { get; set; }

            [DataMember(Name = "User")]
            public string User { get; set; }

            [DataMember(Name = "Password")]
            public string Password { get; set; }

        }
    }
}


using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.Communication.WebSockets
{
    public class WebSocketPacket
    {
        private bool incoming;
        public dynamic data;
        public WebSocketPacket(bool incoming = false)
        {
            this.incoming = incoming;
            this.data = new ExpandoObject();
        }

        public bool deserializeFrom(string data)
        {
            if (!incoming)
                return false;
            try
            {
                this.data = JsonConvert.DeserializeObject(data);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public WebSocketPacket set(string name, dynamic obj)
        {
            if (this.incoming)
                return this;
            ((IDictionary<String, Object>)data)[name] = obj;
            return this;
        }
        public string serialize()
        {
            return JsonConvert.SerializeObject(this.data);
        }
        public dynamic get(string name)
        {

            try
            {
                return this.data[name].Value;
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }
}

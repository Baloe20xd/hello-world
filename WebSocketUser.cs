using Cloud.HabboHotel.GameClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Cloud.Communication.WebSockets
{
    public class WebSocketUser
    {
        public string hash;
        private GameClient gclient;
        private WebSocket csocket;
        private string uuid;
        private int _userId;
        public string id
        {
            get
            {
                return uuid;
            }
        }
        public int userId
        {
            get
            {
                return _userId;
            }
        }
        public WebSocketUser(string uuid, string hash, WebSocket csocket, int userId)
        {
            this.uuid = uuid;
            this.hash = hash;
            this.gclient = null;
            this.csocket = csocket;
            this._userId = userId;
        }
        public void error(WebSocketPacket packet)
        {
            this.trySendMessage(packet);
            this.csocket.Close();
        }
        public bool trySendMessage(WebSocketPacket packet)
        {
            try
            {
                this.csocket.Send(packet.serialize());
                return true;
            }catch
            {
                return false;
            }
        }
        public bool attachGameClient(GameClient client)
        {
            if (this.gclient != null)
                return false;
            this.gclient = client;
            return true;
        }

        public bool tryGetGameClient(out GameClient client)
        {
            client = this.gclient;
            return this.gclient != null;
        }
        public bool tryDisconnect()
        {
            try
            {
                this.csocket.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }


    }
}

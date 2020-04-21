using Cloud.Database.Interfaces;
using Cloud.HabboHotel.GameClients;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Cloud.Communication.WebSockets
{
    public class WebSocketsManager
    {
        private int port = 3000;
        private WebSocketServer wssv;
        private Dictionary<string, WebSocketUser> _users;
        private ILog logging;

        public WebSocketsManager(int working_port, bool usessl, string key, string pass)
        {
            this.port = working_port;
            _users = new Dictionary<string, WebSocketUser>();
            this.logging = LogManager.GetLogger("Cloud.Communication.WebSockets.WebSocketsManager");
            this.logging.Info("WebSocketManager dinliyor: " + working_port);
            this.wssv = new WebSocketServer(this.port, usessl);
            if (usessl)
                wssv.SslConfiguration.ServerCertificate =
  new X509Certificate2(key, pass);
            wssv.AddWebSocketService<WebSocketHandler>("/game");
            this.wssv.Start();
        }
        internal void _proccessClose(string uuid, WebSocket webSocket, CloseEventArgs e)
        {
            if (tryGetUser(uuid, out WebSocketUser usr))
            {

                CloudServer.GetPluginManager().proccessWSClose(usr);
                _users.Remove(_users.First(kvp => kvp.Value == usr).Key);
            }


        }
        internal void _proccessError(object uuid, WebSocket webSocket, ErrorEventArgs e)
        {

        }

        internal void _proccessOpen(string uuid, WebSocket webSocket)
        {


        }

        public void _proccessMsg(string uuid, WebSocket csocket, string data)
        {
            try
            {
                var packet = new WebSocketPacket(true);
                if (!packet.deserializeFrom(data))
                {
                    csocket.Close();
                }
                else
                {
                    if (packet.get("cmd") != null && packet.get("cmd") == "auth")
                    {
                        if (packet.get("hash") == null || packet.get("hash") == "")
                        {
                            csocket.Send((new WebSocketPacket()).set("cmd", "auth").set("res", "err").set("msg", "Missig params").serialize());
                            csocket.Close();
                            return;
                        }
                        else
                        {
                            string hash = Convert.ToString(packet.get("hash"));
                            using (IQueryAdapter dbClient = CloudServer.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("SELECT * FROM users WHERE ws_ticket = @hash");
                                dbClient.AddParameter("hash", hash);
                                dbClient.RunQuery();

                                var rows = dbClient.getTable().Rows;
                                if (rows.Count < 1)
                                {
                                    csocket.Send((new WebSocketPacket()).set("cmd", "auth").set("res", "err").set("msg", "Wrong token").serialize());
                                    csocket.Close();
                                    return;
                                }
                                else
                                {

                                    /*dbClient.SetQuery("UPDATE users SET ws_ticket = '' WHERE ws_ticket = @hash");
                                    dbClient.AddParameter("hash", hash);
                                    dbClient.RunQuery();*/

                                    var row = rows[0];
                                    var userId = Convert.ToInt32(row["id"]);
                                    this._authorizeUser(uuid, csocket, hash, userId);
                                    if (uuidExists(uuid))
                                        CloudServer.GetPluginManager().proccessWSOpen(_users[uuid]);
                                }

                            }
                        }
                    }
                    else
                    {
                        if (!this.uuidExists(uuid))
                        {
                            csocket.Send((new WebSocketPacket()).set("cmd", "auth").set("res", "err").set("msg", "Authenticate firstly.").serialize());
                            csocket.Close();
                            return;
                        }
                        else
                        {
                            proccessMessage(uuid, csocket, data);
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
        }
        private bool proccessMessage(string uuid, WebSocket socket, string data)
        {
            try
            {
                if (uuidExists(uuid))
                {
                    WebSocketUser user;
                    if (!tryGetUser(uuid, out user))
                        return false;
                    var packet = new WebSocketPacket(true);
                    packet.deserializeFrom(data);


                    CloudServer.GetPluginManager().proccessWSMessage(user, packet);


                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }
        private void _authorizeUser(string uuid, WebSocket csocket, string hash, int id)
        {
            var wuser = new WebSocketUser(uuid, hash, csocket, id);
            var authOkMsg = new WebSocketPacket();

            foreach (WebSocketUser user in _users.Values.ToList())
            {
                if (user.hash == hash)
                {
                    user.trySendMessage((new WebSocketPacket()).set("cmd", "connection").set("status", "drop").set("msg", "Another user connected"));
                    user.tryDisconnect();
                }
            }
            var client = CloudServer.GetGame().GetClientManager().GetClientByUserID(id);
            if (client != null)
                wuser.attachGameClient(client);

            this._users.Add(hash, wuser);
            wuser.trySendMessage(authOkMsg.set("cmd", "auth").set("res", "ok"));

        }
        public bool attachGameClient(GameClient client)
        {
            try
            {
                if (client != null)
                    if (client.GetHabbo() != null)
                    {
                        var id = client.GetHabbo().Id;
                        foreach (WebSocketUser user in _users.Values.ToList())
                        {
                            if (user.userId == id)
                                user.attachGameClient(client);
                        }
                    }
            }
            catch
            {
                return false;
            }
            return false;
        }
        private bool userExists(string hash)
        {
            foreach (WebSocketUser user in _users.Values.ToList())
            {
                if (user.hash == hash)
                    return true;
            }
            return false;
        }
        private bool uuidExists(string uuid)
        {
            foreach (KeyValuePair<string, WebSocketUser> pair in _users.ToList())
            {
                if (pair.Value.id == uuid)
                    return true;
            }
            return false;
        }
        public bool tryGetUser(GameClient client, out WebSocketUser user)
        {
            if(client != null && client.GetHabbo() != null)
            foreach (WebSocketUser usr in _users.Values.ToList())
                if (usr.tryGetGameClient(out GameClient clnt))
                {
                        if (clnt == null || clnt.GetHabbo() == null)
                            continue;
                    if (clnt.GetHabbo().Id == client.GetHabbo().Id)
                    {
                        user = usr;
                        return true;
                    }

                }
            user = null;
            return false;
        }
        public bool tryGetUser(string uuid, out WebSocketUser user)
        {

            foreach (WebSocketUser usr in _users.Values.ToList())
                if (usr.id == uuid)
                {
                    user = usr;
                    return true;
                }
            user = null;
            return false;
        }
    }
}


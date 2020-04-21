using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Cloud.Communication.WebSockets
{
    public class WebSocketHandler: WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            string uuid = this.ID;

            var msg = e.Data;
            CloudServer.GetWebSocketsManager()._proccessMsg(uuid, this.Context.WebSocket, msg);
        }
        protected override void OnOpen()
        {
            string uuid = this.ID;

            CloudServer.GetWebSocketsManager()._proccessOpen(uuid, this.Context.WebSocket);

        }
        protected override void OnClose(CloseEventArgs e)
        {
            string uuid = this.ID;

            CloudServer.GetWebSocketsManager()._proccessClose(uuid, this.Context.WebSocket, e);
        }
        protected override void OnError(ErrorEventArgs e)
        {
            string uuid = this.ID;
            CloudServer.GetWebSocketsManager()._proccessError(uuid, this.Context.WebSocket, e);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace ChatSite
{
    public class ChatHub : Hub
    {
        static List<User> Users = new List<User>();
        static List<Message> Message = new List<Message>();


        ChatDataContext CDC = new ChatDataContext();

        public void Connect(string userName)
        {
            var id = Context.ConnectionId;


            if (Users.Count(x => x.ConnectionId == id) == 0)
            {
                Users.Add(new User { ConnectionId = id, UserName = userName });
                if (Message.Count == 0)
                {
                    Message = (from chats in CDC.tblChatHistories
                               select new Message { MessageContent = chats.Chat, UserName = chats.ChatUserName, MessageTime = DateTime.Parse(chats.DateSent.ToString()).ToString("HH:mm") }).ToList();
                }
                // send to caller
                Clients.Caller.onConnected(id, userName, Users, Message);

                // send to all except caller client
                Clients.AllExcept(id).onNewUserConnected(id, userName);

            }

        }

        public void SendMessageToAll(string userName, string message)
        {
            string CurrentTime = DateTime.Now.ToString("HH:mm");
            // store last 100 messages in cache
            AddMessageinCache(userName, message, CurrentTime);

            //Save to database
            tblChatHistory newChat = new tblChatHistory()
            {
                ChatUserName = userName,
                Chat = message,
                DateSent = DateTime.Now
            };
            CDC.tblChatHistories.InsertOnSubmit(newChat);
            CDC.SubmitChanges();

            // Broad cast message
            Clients.All.messageReceived(userName, message, CurrentTime);
        }




        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            var item = Users.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                Users.Remove(item);

                var id = Context.ConnectionId;
                Clients.All.onUserDisconnected(id, item.UserName);

            }

            return base.OnDisconnected(stopCalled);
        }

        private void AddMessageinCache(string userName, string message, string messagetime)
        {
            Message.Add(new Message { UserName = userName, MessageContent = message, MessageTime = messagetime });

            if (Message.Count > 100)
                Message.RemoveAt(0);

        }
    }

    public class Message
    {
        public string UserName { get; set; }
        public string MessageContent { get; set; }
        public string MessageTime { get; set; }
    }

    public class User
    {
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using signalr.backend.Data;
using signalr.backend.Models;

namespace signalr.backend.Hubs
{
    // On garde en mémoire les connexions actives (clé: email, valeur: userId)
    // Note: Ce n'est pas nécessaire dans le TP
    public static class UserHandler
    {
        public static Dictionary<string, string> UserConnections { get; set; } = new Dictionary<string, string>();
    }

    // L'annotation Authorize fonctionne de la même façon avec SignalR qu'avec Web API
    [Authorize]
    // Le Hub est le type de base des "contrôleurs" de SignalR
    public class ChatHub : Hub
    {
        public ApplicationDbContext _context;

        public IdentityUser CurentUser
        {
            get
            {
                // On récupère le userid à partir du Cookie qui devrait être envoyé automatiquement
                string userid = Context.UserIdentifier!;
                return _context.Users.Single(u => u.Id == userid);
            }
        }

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async override Task OnConnectedAsync()
        {
            UserHandler.UserConnections.Add(CurentUser.Email!, Context.UserIdentifier);

            // TODO: Envoyer des message aux clients pour les mettre à jour

            // envoyer aux clients liste de users connectés
            await Clients.All.SendAsync("UsersList", UserHandler.UserConnections.ToList());
            // envoyer aux clients liste de channels
            await Clients.All.SendAsync("ChannelsList", _context.Channel.ToList());
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            // Lors de la fermeture de la connexion, on met à jour notre dictionnary d'utilisateurs connectés
            KeyValuePair<string, string> entrie = UserHandler.UserConnections.SingleOrDefault(uc => uc.Value == Context.UserIdentifier);
            UserHandler.UserConnections.Remove(entrie.Key);

            // TODO: Envoyer un message aux clients pour les mettre à jour
            await Clients.All.SendAsync("UsersList", UserHandler.UserConnections.ToList());
        }

        public async Task CreateChannel(string title)
        {
            _context.Channel.Add(new Channel { Title = title });
            await _context.SaveChangesAsync();

            // TODO: Envoyer un message aux clients pour les mettre à jour
            await Clients.All.SendAsync("ChannelsList", _context.Channel.ToList());
        }

        public async Task DeleteChannel(int channelId)
        {
            Channel channel = _context.Channel.Find(channelId);

            if(channel != null)
            {
                _context.Channel.Remove(channel);
                await _context.SaveChangesAsync();
            }
            // Le nom du groupe représentant les membres du channel supprimé 
            string groupName = CreateChannelGroupName(channelId);
            // Envoyer les messages nécessaires aux clients

            // envoyer aux membres du channel supprimé un message pour les faire sortir du channel
            await Clients.Group(groupName).SendAsync("LeaveChannel", "leave this channel");

            // envoyer aux clients la liste de channels restants
            await Clients.All.SendAsync("ChannelsList", _context.Channel.ToList());
        }

        public async Task JoinChannel(int oldChannelId, int newChannelId)
        {
            var oldChannel = await _context.Channel.FindAsync(oldChannelId);

            // pour afficher un message dans les channels impliqués
            // ([userTag] a joint ce canal et [userTag] left the chat)
            string userTag = "[" + CurentUser.Email! + "]";

            // TODO: Faire quitter le vieux canal à l'utilisateur
            string oldGroupName = CreateChannelGroupName(oldChannelId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldGroupName);

            if (oldChannel != null)
            {
                await Clients.Group(oldGroupName).SendAsync("NewMessage", userTag + " quitte : " + oldChannel.Title);
            }

            // TODO: Faire joindre le nouveau canal à l'utilisateur
            string newGroupName = CreateChannelGroupName(newChannelId);
            await Groups.AddToGroupAsync(Context.ConnectionId, newGroupName);

            if (newChannelId != 0)
            {
                var newChannel = await _context.Channel.FindAsync(newChannelId);
                await Clients.Group(newGroupName).SendAsync("NewMessage", userTag + " a rejoint : " + newChannel.Title);
            }
        }

        public async Task SendMessage(string message, int channelId, string userId)
        {
            string userTag = "[De: " + CurentUser.Email! + "]";
            if (userId != null)
            {
                // TODO: Envoyer le message à cet utilisateur
                await Clients.User(userId).SendAsync("NewMessage", (userTag + " " + message));
            }
            else if (channelId != 0)
            {
                // TODO: Envoyer le message aux utilisateurs connectés à ce canal
                string channelGroupName = CreateChannelGroupName(channelId);
                await Clients.Group(channelGroupName).SendAsync("NewMessage", (userTag + " " + message));
            }
            else
            {
                await Clients.All.SendAsync("NewMessage", "[Tous] " + message);
            }
        }

        private static string CreateChannelGroupName(int channelId)
        {
            return "Channel" + channelId;
        }
    }
}
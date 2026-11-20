using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using signalr.backend.Data;
using signalr.backend.Hubs;
using signalr.backend.Models;

namespace signalr.backend.Services
{
    public class MonBackgroundService : BackgroundService
    {
        public const int DELAY = 30 * 1000;

        private IServiceScopeFactory _serviceScopeFactory;
        private IHubContext<ChatHub> _chatHub;

        public MonBackgroundService(IHubContext<ChatHub> chatHub, IServiceScopeFactory serviceScopeFactory) 
        {
            _serviceScopeFactory = serviceScopeFactory;
            _chatHub = chatHub;
        }

        public async Task FindMostPopularChannel(CancellationToken stoppingToken)
        {

            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // trouver le canal le plus populaire (le + de messages, on prend le premier en cas d'égalité entre au moins 2 canaux)
                var highestNbMessages = await dbContext.Channel.MaxAsync(c => c.NbMessages);
                var mostPopularChannels = await dbContext.Channel.Where(c => c.NbMessages == highestNbMessages).ToListAsync();

                var mostPopularChannel = mostPopularChannels.FirstOrDefault();

                // envoyer le message MostPopularChannel avec la quantité de messages au groupe
                if (mostPopularChannel != null)
                {
                    await SendMessageToUsers(mostPopularChannel);
                }
            }

        }

        public async Task SendMessageToUsers(Models.Channel mostPopularChannel)
        {
            string channelGroupName = "Channel" + mostPopularChannel.Id;
            await _chatHub.Clients.Group(channelGroupName).SendAsync("MostPopularChannel", $"Vous êtes dans le canal le plus populaire avec {mostPopularChannel.NbMessages} messages");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(DELAY, stoppingToken);
                await FindMostPopularChannel(stoppingToken);
            }
        }
    }
}

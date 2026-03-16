using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace FataleCore.Hubs
{
    // Optionally use [Authorize] if you want to restrict connecting to logged in users only
    public class RadioHub : Hub
    {
        public async Task JoinStation(int stationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, stationId.ToString());
            
            // Optionally notify others that a listener joined
            // await Clients.Group(stationId.ToString()).SendAsync("ListenerJoined", ...);
        }

        public async Task LeaveStation(int stationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, stationId.ToString());
            
            // Optionally notify others that a listener left
            // await Clients.Group(stationId.ToString()).SendAsync("ListenerLeft", ...);
        }

        [Authorize]
        public async Task SyncTrack(int stationId, object trackData, double currentTime, bool isPlaying)
        {
            // In a production app, you might want to verify that the Context.User is actually the host of the station
            
            // Broadcast the current track state and playback position to everyone in the station room EXCEPT the sender (the DJ)
            await Clients.GroupExcept(stationId.ToString(), Context.ConnectionId)
                         .SendAsync("TrackSynced", trackData, currentTime, isPlaying);
        }

        [Authorize]
        public async Task SendMessage(int stationId, string message, string username)
        {
            // Broadcast chat message to everyone in the station room
            await Clients.Group(stationId.ToString()).SendAsync("ReceiveMessage", new
            {
                username = username,
                message = message,
                timestamp = System.DateTime.UtcNow
            });
        }

        [Authorize]
        public async Task RequestTrack(int stationId, object trackData, string username)
        {
            // The listener sends a track request. This goes to the entire group, but the frontend DJ UI can listen for it.
            await Clients.Group(stationId.ToString()).SendAsync("TrackRequested", new
            {
                track = trackData,
                requestedBy = username,
                timestamp = System.DateTime.UtcNow
            });
        }
    }
}

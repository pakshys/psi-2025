using Microsoft.Extensions.Hosting;

namespace backend.Services;

public class RoomCleanupService : BackgroundService
{
  private readonly IRoomStateService _roomState;
  private readonly IServiceScopeFactory _scopeFactory;

  public RoomCleanupService(
    IRoomStateService roomState,
    IServiceScopeFactory scopeFactory)
  {
    _roomState = roomState;
    _scopeFactory = scopeFactory;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var checkInterval = TimeSpan.FromSeconds(20); // Check every 20 seconds
    var emptyTimeout = TimeSpan.FromSeconds(120); // Rooms empty for more than 2 minutes will be deleted

    while (!stoppingToken.IsCancellationRequested)
    {
      var now = DateTime.UtcNow;
      var emptyRooms = _roomState.GetEmptyRooms();

      foreach (var room in emptyRooms)
      {
        // Only delete if room is still empty
        if (now - room.Value > emptyTimeout && _roomState.GetUsersInRoom(room.Key).Count == 0)
        {
          using var scope = _scopeFactory.CreateScope();
          var persistence = scope.ServiceProvider.GetRequiredService<IRoomPersistenceService>();

          await persistence.DeleteRoomAsync(room.Key, stoppingToken);
          _roomState.DeleteRoom(room.Key);
        }
      }

      await Task.Delay(checkInterval, stoppingToken);
    }
  }
}
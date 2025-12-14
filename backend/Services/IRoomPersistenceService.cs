namespace backend.Services;

public interface IRoomPersistenceService
{
  Task DeleteRoomAsync(int roomId, CancellationToken cancellationToken);
}
using backend.Database;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Services;

public class RoomPersistenceService : IRoomPersistenceService
{
  private readonly ApplicationDbContext _db;

  public RoomPersistenceService(ApplicationDbContext db)
  {
    _db = db;
  }

  public async Task DeleteRoomAsync(int roomId, CancellationToken cancellationToken)
  {
    var rooms = _db.Set<PartyRoom>();
    await rooms
      .Where(r => r.Id == roomId)
      .ExecuteDeleteAsync(cancellationToken);

  }
}
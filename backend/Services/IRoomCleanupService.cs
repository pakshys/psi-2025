namespace backend.Services;

public interface IRoomCleanupService
{
  Task RunAsync(CancellationToken cancellationToken);
}
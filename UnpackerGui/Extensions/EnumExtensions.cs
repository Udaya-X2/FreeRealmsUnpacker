using System.Threading.Tasks;

namespace UnpackerGui.Extensions;

/// <summary>
/// Provides extension methods for various enum types.
/// </summary>
public static class EnumExtensions
{
    private const TaskStatus CompletedMask = TaskStatus.Canceled | TaskStatus.Faulted | TaskStatus.RanToCompletion;

    /// <inheritdoc cref="Task.IsCanceled"/>
    public static bool IsCanceled(this TaskStatus status) => status == TaskStatus.Canceled;

    /// <inheritdoc cref="Task.IsCompleted"/>
    public static bool IsCompleted(this TaskStatus status) => (status & CompletedMask) != 0;

    /// <inheritdoc cref="Task.IsCompletedSuccessfully"/>
    public static bool IsCompletedSuccessfully(this TaskStatus status) => status == TaskStatus.RanToCompletion;

    /// <inheritdoc cref="Task.IsFaulted"/>
    public static bool IsFaulted(this TaskStatus status) => status == TaskStatus.Faulted;
}

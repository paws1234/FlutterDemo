namespace Notification.Api.Services;

/// <summary>
/// The demo's order state. §1 In Scope excludes persistent history, so the store is
/// in-memory and knows only the orders the demo talks about. Ceiling: the state is
/// lost on restart and is shared by nothing. Upgrade path: swap this one implementation
/// for a persistent one — no other type names the storage.
/// </summary>
public interface IOrderStatusService
{
    /// <summary>Applies the status and returns true, or false when the id is unknown.</summary>
    bool TryUpdateStatus(int orderId, string status);
}

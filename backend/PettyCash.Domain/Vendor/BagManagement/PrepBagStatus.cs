namespace PettyCash.Domain.Vendor.BagManagement;

public enum PrepBagStatus
{
    Preparing = 0,   // 準備中
    HandedOver = 1,  // 引渡済
    Cancelled = 2    // 取消（戻し）
}

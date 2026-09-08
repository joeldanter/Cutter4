using Cutter4.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cutter4.Persistence.Repositories
{
    internal static class OrderRepositoryErrors
    {
        public static UserFacingException OrderSave_InsufficientStock(PanelKey panelKey, int available, int needed)
            => new UserFacingException(
                "Error_Persistence_OrderSave_InsufficientStock",
                $"Save denied: saving would require {needed} of {panelKey}, only {available} available",
                panelKey, available, needed);

        public static UserFacingException OrderSave_DuplicateSerialNumber(string serialNumber)
            => new UserFacingException(
                "Error_Persistence_OrderSave_DuplicateSerialNumber",
                $"Save denied: duplicate serial number {serialNumber}",
                serialNumber);

        public static UserFacingException OrderUndo_InsufficientStock(PanelKey panelKey, int available, int needed)
            => new UserFacingException(
                "Error_Persistence_OrderUndo_InsufficientStock",
                $"Undo denied: reverting would require {needed} of {panelKey}, only {available} available",
                panelKey, available, needed);

        public static UserFacingException OrderUndo_NoOrder()
            => new UserFacingException(
                "Error_Persistence_OrderUndo_NoOrder",
                "Undo denied: nothing to undo");
    }

    internal static class StorageRepositoryErrors
    {
        public static UserFacingException StorageEdit_InsufficientStock(PanelKey panelKey, int available, int requestedToRemove)
            => new UserFacingException(
                "Error_Persistence_StorageEdit_InsufficientStock",
                $"Edit denied: tried to remove {requestedToRemove} of {panelKey}, only {available} available",
                panelKey, available, requestedToRemove);
    }
}

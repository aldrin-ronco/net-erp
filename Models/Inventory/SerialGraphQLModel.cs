using Models.Global;
using System;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Inventory
{
    /// <summary>
    /// Serial (unidad física individualizable) de un ítem serial-tracked.
    /// Mapea al tipo <c>Serial</c> del schema GraphQL.
    /// </summary>
    public class SerialGraphQLModel
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>available | sold | reserved | defective | in_transit | in_repair | written_off | on_loan.</summary>
        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public ItemGraphQLModel Item { get; set; } = new();
        public SystemAccountGraphQLModel? CreatedBy { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Ubicación actual de un serial (vw_serial_current_location).</summary>
    public class SerialCurrentLocationGraphQLModel
    {
        public int SerialId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public int ItemId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? CurrentStorageId { get; set; }
        public string? CurrentStorageName { get; set; }
    }

    public class SerialCreateMessage
    {
        public UpsertResponseType<SerialGraphQLModel> CreatedSerial { get; set; } = new();
    }

    public class SerialUpdateMessage
    {
        public UpsertResponseType<SerialGraphQLModel> UpdatedSerial { get; set; } = new();
    }

    public class SerialDeleteMessage
    {
        public DeleteResponseType DeletedSerial { get; set; } = new();
    }
}

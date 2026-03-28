using System.Collections.ObjectModel;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    public class AccessProfileGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsSystemAdmin { get; set; }
        public ObservableCollection<AccessProfileMenuItemGraphQLModel> AccessProfileMenuItems { get; set; } = [];

        public class AccessProfileCreateMessage
        {
            public required UpsertResponseType<AccessProfileGraphQLModel> CreatedAccessProfile { get; set; }
        }

        public class AccessProfileUpdateMessage
        {
            public required UpsertResponseType<AccessProfileGraphQLModel> UpdatedAccessProfile { get; set; }
        }

        public class AccessProfileDeleteMessage
        {
            public required DeleteResponseType DeletedAccessProfile { get; set; }
        }
    }
}

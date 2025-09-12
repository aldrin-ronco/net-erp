using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class GraphQLResponseTypes
    {
        // Response wrapper types (compatible with existing queries)
        public class SingleItemResponseType<T>
        {
            public T CreateResponse { get; set; } = default!;
            public T UpdateResponse { get; set; } = default!;
            public T DeleteResponse { get; set; } = default!;
            public T SingleItemResponse { get; set; } = default!;
        }

        public class ListItemResponseType<T>
        {
            public ObservableCollection<T> ListResponse { get; set; } = [];
        }

        public class PageResponseType<T>
        {
            public PageType<T> PageResponse { get; set; } = new();
        }

        public class CanDeleteResponseType
        {
            public CanDeleteType CanDeleteResponse { get; set; } = new();
        }

        public class PageType<T>
        {
            public int Count { get; set; }
            public ObservableCollection<T> Rows { get; set; } = [];
        }

        public class CanDeleteType
        {
            public bool CanDelete { get; set; } = false;
            public string Message { get; set; } = string.Empty;
        }
    }
}

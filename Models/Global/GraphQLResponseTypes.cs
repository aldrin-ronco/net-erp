using Models.Login;
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
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
            public int TotalEntries { get; set; }
            public ObservableCollection<T> Entries { get; set; } = [];

            //Las siguientes propiedades están deprecadas y se mantienen solo por evitar errores de compilación en queries antiguas, remover cuando se haya 
            //hecho el cambio a las propiedades adecuadas para evitar errores de compilación
            public int Count { get; set; }
            public ObservableCollection<T> Rows { get; set; } = [];
        }

        public class CanDeleteType
        {
            public bool CanDelete { get; set; } = false;
            public string Message { get; set; } = string.Empty;
        }

        public class UpsertResponseType<T>
        {
            public T Entity {get; set;} = default!;
            public string Message {get; set;} = string.Empty;
            public bool Success {get; set;} = false;
            public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
        }

        public class DeleteResponseType
        {
            public int DeletedId { get; set; }
            public string Message { get; set; } = string.Empty;
            public bool Success { get; set; }
        }
    }
}

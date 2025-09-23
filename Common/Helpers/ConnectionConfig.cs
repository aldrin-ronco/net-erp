using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public static class ConnectionConfig
    {
        //public static string GraphQLAPIUrl { get; set; } = Debugger.IsAttached ? @"https://qts-erp-fox-api-staging.herokuapp.com/graphql" : @"https://qts-erp-fox-api.herokuapp.com/graphql";
        [System.Obsolete("This API URL is deprecated. Use MainGraphQLAPIUrl instead", false)]
        public static string GraphQLAPIUrl { get; set; } = Debugger.IsAttached ? @"https://localhost:7048/graphql/" : @"https://qts-erp-fox-api.herokuapp.com/graphql";
        public static string MainGraphQLAPIUrl { get; set; } = "https://api.qtsolutions.com.co/graphql";
        public static readonly string LoginAPIUrl = "https://accounts.qtsolutions.com.co/graphql";
        [System.Obsolete("This Database ID is deprecated.", false)]
        public static string DatabaseId { get; set; } = "b1df16c1-9e80-412f-9da0-c74c061de320";
    }
}

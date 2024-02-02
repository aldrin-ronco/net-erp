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
        public static string GraphQLAPIUrl { get; set; } = Debugger.IsAttached ? @"https://qts-erp-fox-api-staging.herokuapp.com/graphql" : @"https://qts-erp-fox-api.herokuapp.com/graphql";

        public static string DatabaseId { get; set; } = "00ba5c57-6c10-469e-a296-7cf75a091da3";
    }
}

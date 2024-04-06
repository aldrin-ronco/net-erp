using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public class GraphQLError
    {
        public Error[] Errors { get; set; }

        public class Error
        {
            public string Message { get; set; }
            public Location[] Locations { get; set; }

            public Extensions Extensions { get; set; }
        }

        public class Location
        {
            public int Line { get; set; }
            public int Column { get; set; }
        }

        public class Extensions
        {
            public string Message { get; set; }

        }
    }
}

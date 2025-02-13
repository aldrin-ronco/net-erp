﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class CityGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int DepartmentId { get; set; } = 0;
        public DepartmentGraphQLModel Department { get; set; } = new();
        public override string ToString()
        {
            return $"{this.Code} - {this.Name}";
        }
    }

    public class CityDTO: CityGraphQLModel
    {
        
    }
}

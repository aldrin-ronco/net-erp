using System;
using System.Collections.ObjectModel;


namespace Models.Global
{
    public class ParameterGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int ModuleId { get; set; }
        public int DatatypeId { get; set; }

        public Datatype Datatype { get; set; }
    
       public IEnumerable<Qualifier> Qualifiers { get; set; } = [];
       public ObservableCollection<Qualifier> QualifierScreens { get; set; } = [];
    }
    public class Datatype
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class Qualifier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int QualifierTypeId { get; set; }
        public bool IsChecked { get; set; } = false;
    }
   
}

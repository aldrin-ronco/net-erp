using System.Collections.Generic;
using Common.Helpers;

namespace NetErp.Tests.TestDoubles;

public class FakeCountry
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class FakeViewModelSimple
{
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public int Age { get; set; }
}

public class FakeViewModelWithAttributes
{
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }

    [ExpandoPath("data.countryId", SerializeAsId = true)]
    public FakeCountry? Country { get; set; }

    [ExpandoPath("data.notes")]
    public string Notes { get; set; } = "";

    public List<string> Tags { get; set; } = [];
}

namespace LearningTracker.Api.Logic.DTO.Catalog;

public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string L1Name { get; set; }
    public string L2Name { get; set; }
    public string UnitName { get; set; }
}

public class BookSummaryResponse
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string SeriesName { get; set; }
    public int TotalUnits { get; set; }
}

public class UnitGroupResponse
{
    public string L1Label { get; set; }
    public int L1Order { get; set; }
    public List<UnitResponse> Units { get; set; }
}

public class UnitResponse
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
    public int SortOrder { get; set; }
}

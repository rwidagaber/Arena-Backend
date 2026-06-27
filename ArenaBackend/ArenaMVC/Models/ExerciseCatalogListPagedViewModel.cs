using ArenaApplication.Dtos.Workout;
using System.Collections.Generic;

namespace ArenaMVC.Models
{
    public class ExerciseCatalogListPagedViewModel
    {
        public List<ExerciseCatalogItemDto> Items { get; set; } = new List<ExerciseCatalogItemDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? Search { get; set; }

        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}

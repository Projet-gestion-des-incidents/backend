using System;
using System.Collections.Generic;
using System.Text;


namespace projet0.Application.Common.Models.Pagination
{
    public class SortOption
    {
        public string Field { get; set; } = string.Empty;
        public bool IsDescending { get; set; } = false;

        public static SortOption? Parse(string? sortString)
        {
            if (string.IsNullOrWhiteSpace(sortString))
                return null;

            var parts = sortString.Split(' ');
            if (parts.Length != 2)
                return null;

            return new SortOption
            {
                Field = parts[0],
                IsDescending = parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
            };
        }
    }
}

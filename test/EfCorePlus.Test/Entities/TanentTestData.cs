using EfCorePlus.Entities;
using System.ComponentModel.DataAnnotations;

namespace EfCorePlus.Test.Entities
{
    public class TanentTestData : ITenant
    {
        [Key]
        public int Id { get; set; }

        public int TenantId { get; set; }

        public string? Data { get; set; }
    }
}

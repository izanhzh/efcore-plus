using EfCorePlus.Entities;
using System.ComponentModel.DataAnnotations;

namespace EfCorePlus.Test.Entities
{
    public class SoftDeleteTestData : ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        public string? Data { get; set; }

        public bool IsDeleted { get; set; }
    }
}

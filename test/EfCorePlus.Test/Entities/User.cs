using EfCorePlus.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EfCorePlus.Test.Entities
{
    [Table("Users")]
    public class User : ISoftDelete,ITenant
    {
        [Key]
        public int Id { get; set; }

        public int TenantId { get; set; }

        public string? Name { get; set; }

        public bool IsDeleted { get; set; }
    }
}

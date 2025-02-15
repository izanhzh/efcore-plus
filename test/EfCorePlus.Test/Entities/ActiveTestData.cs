using System.ComponentModel.DataAnnotations;

namespace EfCorePlus.Test.Entities
{
    public class ActiveTestData : IIsActive
    {
        [Key]
        public int Id { get; set; }

        public string? Data { get; set; }

        public bool IsActive { get; set; }
    }
}

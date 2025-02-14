using EfCorePlus.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EfCorePlus.Test.Entities
{
    public class Blog : ITenant, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        public int TenantId { get; set; }

        public string Name { get; set; }

        public virtual List<Post>? Posts { get; set; }

        public bool IsDeleted { get; set; }
    }
}

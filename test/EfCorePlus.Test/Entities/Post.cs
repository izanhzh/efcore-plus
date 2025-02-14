using EfCorePlus.Entities;
using System.ComponentModel.DataAnnotations;

namespace EfCorePlus.Test.Entities
{
    public class Post : ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }

        public bool IsDeleted { get; set; }

        public virtual Blog? Blog { get; set; }
    }
}

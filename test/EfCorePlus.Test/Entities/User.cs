﻿using EfCorePlus.Entities;
using System.ComponentModel.DataAnnotations;

namespace EfCorePlus.Test.Entities
{
    public class User : ITenant, ISoftDelete
    {
        [Key]
        public int Id { get; set; }

        public int TenantId { get; set; }

        public string Name { get; set; }

        public bool IsDeleted { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace CustomFilter.EntityFrameworkCore.Entities
{
    public class TestData : ILanguage
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Language { get; set; }
    }
}

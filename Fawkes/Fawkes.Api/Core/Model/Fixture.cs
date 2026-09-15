namespace Fawkes.Api.Core.Model
{
    public class Fixture
    {
        public int Id { get; set; }
        public Guid UniqueId { get; set; }
        public DateTime Date { get; set; }
        public string? LeagueName { get; set; }
        public string? FixtureName { get; set; }
        public string? Location { get; set; }
    }



}

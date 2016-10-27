namespace GitRollbacker.Models
{
    public class Config
    {
        public string Location { get; set; }
        public string[] RollbackItems { get; set; }
        public string[] IgnoreItems { get; set; }
    }
}

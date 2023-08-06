namespace FileSorter.Models
{
    public class RuleModel
    {
        public RuleModel(string[] extensions, string destination)
        {
            Extensions = extensions;
            Destination = destination;
        }

        public string[] Extensions { get; set; }
        public string Destination { get; set; }
    }
}
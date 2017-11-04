namespace RefactoringAndSmellsSaver.DomainModels
{
    public class Refactoring
    {
        public long Id { get; set; }
        
        public string CommitId { get; set; }

        public long ProjectId { get; set; }
        public Project Project { get; set; }
        public string Type { get; set; }

        public string SourceClassName { get; set; }

        public string SourceClassPackageName { get; set; }

        public string SourceClassPath { get; set; }
        
        public string TargetClassName { get; set; }

        public string TargetClassPackageName { get; set; }

        public string TargetClassPath { get; set; }

        public string SourceAttributeName { get; set; }

        public string TargetOperatationName { get; set; }

        public string SourceOperatationName { get; set; }        
    }
}
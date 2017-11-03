using System.Collections.Generic;

namespace RefactoringAndSmellsSaver.DomainModels
{
    public class OrganicClass
    {
        public long Id { get; set; }
        public long CommitId { get; set; }
        public Commit Commit { get; set; }
        public string FullyQualifiedName { get; set; }
        public List<OrganicMethod> Methods { get; set; }
        public List<OrganicSmell> Smells { get; set; }
        public OrganicMetrics MetricsValues { get; set; }
    }
}
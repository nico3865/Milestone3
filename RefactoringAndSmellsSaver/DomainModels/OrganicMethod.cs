using System.Collections.Generic;

namespace RefactoringAndSmellsSaver.DomainModels
{
    public class OrganicMethod
    {
        public long Id { get; set; }
        public long OrganicClassId { get; set; }
        public OrganicClass OrganicClass { get; set; }
        public string FullyQualifiedName { get; set; }
        public List<OrganicSmell> Smells { get; set; }
        public OrganicMetrics MetricsValues { get; set; }
    }
}
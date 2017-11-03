namespace RefactoringAndSmellsSaver.DomainModels
{
    public class OrganicMetrics
    {
        public long Id { get; set; }
        public double? ParameterCount { get; set; }
        public double? CyclomaticComplexity { get; set; }
        public double? LocalityRatio { get; set; }
        public double? MethodLinesOfCode { get; set; }
        public double? MaxCallChain { get; set; }
    }
}
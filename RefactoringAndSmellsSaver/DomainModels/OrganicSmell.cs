namespace RefactoringAndSmellsSaver.DomainModels
{
    public class OrganicSmell
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public long? OrganicClassId { get; set; }

        public OrganicClass OrganicClass { get; set; }
        public long? OrganicMethodId { get; set; }

        public OrganicMethod OrganicMethod { get; set; }
    }
}
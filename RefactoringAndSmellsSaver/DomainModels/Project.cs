using System.Collections.Generic;

namespace RefactoringAndSmellsSaver.DomainModels
{
    public class Project
    {
        public Project(string name)
        {
            Name=name;
        }

        private Project()
        {
            
        }
        public long Id { get; set; }
        public string  Name { get; private set; }
    }
}
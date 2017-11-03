using System;
using System.Collections.Generic;

namespace RefactoringAndSmellsSaver.DomainModels
{
    public class Commit
    {
        public Commit(long projectId, string commitId, OrganicClass[] classes)
        {
            ProjectId = projectId;
            CommitId = commitId;
            Classes = new List<OrganicClass>(classes);
        }

        private Commit()
        {

        }

        public long Id { get; set; }
        public long ProjectId { get; private set; }
        public Project Project { get; set; }
        public string AuthorName { get; set; }
        public string FullMessage { get; set; }
        public string ShortMessage { get; set; }
        public DateTime? DateTime { get; set; }
        public string CommitId { get; private set; }
        public string BranchName { get; set; }
        public List<OrganicClass> Classes { get; private set; }
    }
}
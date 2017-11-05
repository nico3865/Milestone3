using DataRepository;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;

namespace RefactoringAndSmellsSaver.ResearchQuestions.RQ3
{
    public class AnswerResolver : IAnswerResolver
    {
        public void Resolve()
        {
            using (var dbContext = new BadSmellMinerDbContext())
            {
                var refactorings = dbContext.Refactorings.ToArray();
                var commits = dbContext.Commits
                .Include(m=>m.Project)
                .ToDictionary(q => q.CommitId);
               
               /* var methods = dbContext.OrganicMethods
                .AsNoTracking()
                .ToArray();*/

                var refactoringDictionary = new Dictionary<long, Dictionary<string, int>>();

                foreach (var refactoring in refactorings)
                {
                    if (!refactoringDictionary.ContainsKey(refactoring.ProjectId))
                    {
                        refactoringDictionary[refactoring.ProjectId] = new Dictionary<string, int>();
                    }

                    if (!refactoringDictionary[refactoring.ProjectId].ContainsKey(refactoring.CommitId))
                    {
                        refactoringDictionary[refactoring.ProjectId][refactoring.CommitId] = 0;
                    }

                    refactoringDictionary[refactoring.ProjectId][refactoring.CommitId]++;
                }


                var lines=new List<string>();

                foreach (var projectId in refactoringDictionary.Keys)
                {
                    foreach (var commitSha in refactoringDictionary[projectId].Keys)
                    {
                        //if(!commits.ContainsKey(commitSha))
                            //continue;

                        //var commit = commits[commitSha];
                        lines.Add($"{projectId},{commitSha},{refactoringDictionary[projectId][commitSha]}");
                    }
                }

                File.WriteAllLines(@"RQs\RQ3\results2.txt",lines);
            }
        }
    }
}
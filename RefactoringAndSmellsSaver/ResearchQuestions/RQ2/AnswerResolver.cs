using DataRepository;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using RefactoringAndSmellsSaver.DomainModels;
using System.Collections.Generic;

namespace RefactoringAndSmellsSaver.ResearchQuestions.RQ2
{
    public class AnswerResolver : IAnswerResolver
    {
        public void Resolve()
        {
            using (var dbContext = new BadSmellMinerDbContext())
            {
                var projects = dbContext.Projects.ToArray();

                foreach (var project in projects)
                    AnalyzeProject(project, dbContext);
            }
        }

        private void AnalyzeProject(Project project, BadSmellMinerDbContext dbContext)
        {
            var commits = GetCommitsSortedByDate(project, dbContext);
            Console.WriteLine($"project: {project.Name}, Number of Commits: {commits.Length}");
            var smellHistoryHolder = new SmellHistoryHolder();
            
            foreach (var commit in commits)
            {
                Console.WriteLine($"{DateTime.Now} - Date Of Commit : {commit.DateTime}");

                Console.WriteLine($"{DateTime.Now} - Load Classes");
                var organicClasses = GetClassesOfCommit(commit, dbContext);

                Console.WriteLine($"{DateTime.Now} - Update Smell Statuses");
                foreach (var organicClass in organicClasses)
                    smellHistoryHolder.AddSmells(commit, organicClass);

                Console.WriteLine($"{DateTime.Now} - ResolveSmells");
                smellHistoryHolder.ResolveUnseenSmells(commit);

                Console.WriteLine(smellHistoryHolder.SmellsCount);
                Console.WriteLine(smellHistoryHolder.ResolvedSmellsCount);
            }
        }

        private object GetMethodsOfCommit(Commit commit, BadSmellMinerDbContext dbContext)
        {
            return dbContext.OrganicMethods.Where(q => q.OrganicClass.CommitId == commit.Id)
            .Include(m => m.Smells)
            .AsNoTracking()
            .ToArray();
        }

        private OrganicClass[] GetClassesOfCommit(Commit commit, BadSmellMinerDbContext dbContext)
        {
            return dbContext.OrganicClasses.Where(q => q.CommitId == commit.Id)
            .Include(m => m.Commit)
            .Include(m => m.Smells)
            .Include(m => m.Methods).ThenInclude(m=>m.Smells)
            .AsNoTracking()
            .ToArray();
        }

        private Commit[] GetCommitsSortedByDate(Project project, BadSmellMinerDbContext dbContext)
        {
            return dbContext.Commits.Where(q => q.ProjectId == project.Id)
                    .AsNoTracking()
                    .OrderBy(q => q.DateTime)
                    .ToArray();
        }
    }
}
using DataRepository;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace RefactoringAndSmellsSaver.ResearchQuestions.RQ7
{
    public class AnswerResolver : IAnswerResolver
    {
        public void Resolve()
        {
            using (var dbContext = new BadSmellMinerDbContext())
            {
                var refactorings = dbContext.Refactorings
                .Include(q => q.Project)
                .AsNoTracking()
                .ToArray();

                var firstGroup = refactorings.GroupBy(g => g.ProjectId)

                .Select(r => new
                {
                    ProjectName = r.First().Project.Name,
                    TestCount = r
                            .Where(rg => rg.SourceClassPath.ToLower().Contains("test")
                            || rg.TargetClassPath.ToLower().Contains("test")
                            || (!string.IsNullOrEmpty(rg.SourceOperatationName) && rg.SourceOperatationName.ToLower().Contains("test"))
                            || (!string.IsNullOrEmpty(rg.TargetOperatationName) && rg.TargetOperatationName.ToLower().Contains("test")))
                            .Count(),
                    Total = r.Count()
                });

                foreach (var g in firstGroup)
                {
                    Console.WriteLine($"{g.ProjectName},{g.Total},{g.TestCount},{g.Total - g.TestCount}");
                }
            }
        }
    }
}
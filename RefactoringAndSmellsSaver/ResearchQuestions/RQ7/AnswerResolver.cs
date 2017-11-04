using DataRepository;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace RefactoringAndSmellsSaver.ResearchQuestions.RQ7
{
    public class AnswerResolver : IAnswerResolver
    {
        public void Resolve()
        {
            using (var dbContext = new BadSmellMinerDbContext())
            {
                var refactorings = dbContext.Refactorings.ToArray();
                var groups = refactorings.GroupBy(g=>g.ProjectId)

                .Select(r=>new {
                        TestCount=r
                            .Where(rg=>rg.SourceClassPath.ToLower().Contains("test")).Count(),
                        ProductionCount=r
                        .Where(rg=>!rg.SourceClassPath.ToLower().Contains("test")).Count(),
                });

            }
        }
    }
}
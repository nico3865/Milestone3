using DataRepository;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace RefactoringAndSmellsSaver.ResearchQuestions.RQ3
{
    public class AnswerResolver : IAnswerResolver
    {
        public void Resolve()
        {
            using (var dbContext = new BadSmellMinerDbContext())
            {
               
            }
        }
    }
}
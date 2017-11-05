using System;
using System.Linq;
using System.Collections.Generic;
using RefactoringAndSmellsSaver.DomainModels;

namespace RefactoringAndSmellsSaver.ResearchQuestions.RQ2
{
    public class SmellHistoryHolder
    {
        private Dictionary<string, List<SmellLifetime>> _smellHolder = new Dictionary<string, List<SmellLifetime>>();


        public void AddSmells(Commit commit, OrganicClass organicClass)
        {
            AddClassRelatedSmells(organicClass, commit);
            AddMethodRelatedSmells(organicClass,commit);
        }

        public void ResolveUnseenSmells(Commit commit)
        {
            foreach(var key in _smellHolder.Keys)
            {
                var unseenSmells=_smellHolder[key]
                .Where(q=>!q.HaveBeenSeenInTheLastCommit && q.BirthDate!=commit.DateTime);

                foreach(var unseenSmell in unseenSmells)
                {
                    unseenSmell.Resolved=true;
                    unseenSmell.DemiseDate=commit.DateTime;
                }
            }
        }

        private void AddMethodRelatedSmells(OrganicClass organicClass,Commit commit)
        {

            foreach (var organicMethod in organicClass.Methods)
            {
                var key = $"method-{organicMethod.FullyQualifiedName}";

                if (!_smellHolder.ContainsKey(key))
                {
                    _smellHolder[key] = new List<SmellLifetime>();
                }

                var smells = organicMethod.Smells;

                foreach (var smell in smells)
                {
                    AddSmellToHistory(commit.DateTime.Value, key, smell);
                }
            }

        }

        private void AddClassRelatedSmells(OrganicClass organicClass, Commit commit)
        {
            var key = $"class-{organicClass.FullyQualifiedName}";

            if (!_smellHolder.ContainsKey(key))
            {
                _smellHolder[key] = new List<SmellLifetime>();
            }

            var smells = organicClass.Smells;

            foreach (var smell in smells)
            {
                AddSmellToHistory(commit.DateTime.Value, key, smell);
            }
        }

        private void AddSmellToHistory(DateTime smellBirthDate, string key, OrganicSmell smell)
        {
            var smellLifetimes = _smellHolder[key];
            var existingSmell = smellLifetimes
            .FirstOrDefault(q => q.SmellName == smell.Name && !q.Resolved);

            if (existingSmell == null)
            {
                smellLifetimes.Add(new SmellLifetime()
                {
                    SmellName = smell.Name,
                    BirthDate = smellBirthDate,
                    Resolved = false,
                    DemiseDate = null,
                    HaveBeenSeenInTheLastCommit = false
                });
            }
            else
                existingSmell.HaveBeenSeenInTheLastCommit=true;
        }

        public int SmellsCount => _smellHolder.Values.Sum(m=>m.Count);
        public int ResolvedSmellsCount => _smellHolder.Values.Sum(m=>m.Count(q=>q.Resolved));
    }

    public class SmellLifetime
    {
        public string SmellName { get; set; }
        public bool Resolved { get; set; }
        public bool HaveBeenSeenInTheLastCommit { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime? DemiseDate { get; set; }
    }
}
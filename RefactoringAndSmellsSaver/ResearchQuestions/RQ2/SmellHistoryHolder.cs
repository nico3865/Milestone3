using System;
using System.Linq;
using System.Collections.Generic;
using RefactoringAndSmellsSaver.DomainModels;

using DataRepository;
using Microsoft.EntityFrameworkCore;
using System.IO;


namespace RefactoringAndSmellsSaver.ResearchQuestions.RQ2
{
    public class SmellHistoryHolder
    {
        private static int counterOfSmellsThatDisappeared = 0;
        private Dictionary<string, List<SmellLifetime>> _smellHolder = new Dictionary<string, List<SmellLifetime>>();


        public void AddSmells(Commit commit, OrganicClass organicClass)
        {
            AddClassRelatedSmells(organicClass, commit);
            AddMethodRelatedSmells(organicClass,commit);
        }

        public void ResolveUnseenSmells(Commit commit)
        {
            using (var dbContext = new BadSmellMinerDbContext())
            {
                foreach(var key in _smellHolder.Keys)
                {
                    var unseenSmells=_smellHolder[key]
                    .Where(q=>!q.HaveBeenSeenInTheLastCommit && q.BirthDate!=commit.DateTime);

                    foreach(var unseenSmell in unseenSmells)
                    {
                        counterOfSmellsThatDisappeared++;
                        unseenSmell.Resolved=true;
                        unseenSmell.DemiseDate=commit.DateTime;

                        //ADD NIC: I could get the refactorings for each smell, in here:
                        var keyCleaned = cleanKey(key);
                        Refactoring[] refactoringsForSmell = null;
                        try
                        {
                            refactoringsForSmell = dbContext.Refactorings.Where(
                                    q => q.CommitId == commit.CommitId
                                            && ( true // just get whatever refactoring was done at that commit for now. Forget about matching the exact file, it doesn't work I don't know why.
                                                // string.IsNullOrEmpty(q.SourceClassName) ? false : q.SourceClassName.Split(".", StringSplitOptions.None).Last() == keyCleaned
                                                // || string.IsNullOrEmpty(q.TargetClassName) ? false : q.TargetClassName.Split(".", StringSplitOptions.None).Last() == keyCleaned
                                                // || string.IsNullOrEmpty(q.SourceOperatationName) ? false : q.SourceOperatationName.Split(".", StringSplitOptions.None).Last() == keyCleaned
                                                // || string.IsNullOrEmpty(q.TargetOperatationName) ? false : q.TargetOperatationName.Split(".", StringSplitOptions.None).Last() == keyCleaned
                                                )
                                        )
                                .AsNoTracking()
                                .ToArray();

                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine("commit.Id ==> "+commit.Id);
                        }
                        // output each of them for retrofit so I can verify against my own. (!!!!)
                        if(refactoringsForSmell != null && refactoringsForSmell.Count() > 0) 
                        {
                            foreach(Refactoring refactoring in refactoringsForSmell)
                            {
                                // Console.WriteLine("*************************************************************************************");
                                // Console.WriteLine("refactoring.ProjectId ==> "+refactoring.ProjectId);
                                // Console.WriteLine("refactoring.CommitId ==> "+refactoring.CommitId);
                                // Console.WriteLine("refactoring.Id) ==> "+refactoring.Id);
                                // Console.WriteLine("refactoring.SourceClassName ==> "+refactoring.SourceClassName);
                                // Console.WriteLine("refactoring.SourceOperatationName ==> "+refactoring.SourceOperatationName);
                                // Console.WriteLine("refactoring.TargetClassName ==> "+refactoring.TargetClassName);
                                // Console.WriteLine("refactoring.TargetOperatationName ==> "+refactoring.TargetOperatationName);
                                // Console.WriteLine("refactoring.Type ==> "+refactoring.Type);
                                // Console.WriteLine("--------------------------------------------------------------------------------------");
                                // Console.WriteLine("unseenSmell.SmellName ==> "+unseenSmell.SmellName);
                                // Console.WriteLine("smell key (which is the method or class) ==> "+key);
                                // Console.WriteLine("unseenSmell.Resolved ==> "+unseenSmell.Resolved);
                                // Console.WriteLine("unseenSmell.BirthDate ==> "+unseenSmell.BirthDate);
                                // Console.WriteLine("unseenSmell.DemiseDate ==> "+unseenSmell.DemiseDate);
                                // write csv:
                                using (StreamWriter sw = File.AppendText("smellsThatDisappearedWithCounterAndLocationAndDateOfBirthAndDemiseAndCorrespondingRefactoringTypeAndIdAndCommitIdIfAvailable.csv"))
                                {
                                    sw.WriteLine(
                                        refactoring.ProjectId + ", " +
                                        unseenSmell.CommitId + ", " +
                                        counterOfSmellsThatDisappeared + ", " + 
                                        unseenSmell.SmellName + " , " + 
                                        keyDoesStartWithMethodMarker(key) + " , " + 
                                        keyDoesStartWithClassMarker(key) + " , " + 
                                        cleanKeyToRemoveIsMethodOrIsClassAtStart(key) + ", " + 
                                        unseenSmell.BirthDate + ", " + 
                                        unseenSmell.DemiseDate + ", " + 
                                        refactoring.Type + ", " + 
                                        refactoring.Id + ", "
                                        );
                                }

                            }
                        }
                        else 
                        {
                            // write csv:
                            using (StreamWriter sw = File.AppendText("smellsThatDisappearedWithCounterAndLocationAndDateOfBirthAndDemiseAndCorrespondingRefactoringTypeAndIdAndCommitIdIfAvailable.csv"))
                            {
                                sw.WriteLine(
                                    unseenSmell.ProjectId + ", " +
                                    unseenSmell.CommitId + ", " +
                                    counterOfSmellsThatDisappeared + ", " + 
                                    unseenSmell.SmellName + " , " + 
                                    keyDoesStartWithMethodMarker(key) + " , " + 
                                    keyDoesStartWithClassMarker(key) + " , " + 
                                    cleanKeyToRemoveIsMethodOrIsClassAtStart(key) + ", " + 
                                    unseenSmell.BirthDate + ", " + 
                                    unseenSmell.DemiseDate
                                    );
                            }
                        }
                        //Console.WriteLine();
                        //
                    }
                }
            }

        }

        private string cleanKey(string key)
        {
            var splitByDot = key.Split(".");
            return splitByDot[splitByDot.Length-1];
        }
        private string cleanKeyToRemoveIsMethodOrIsClassAtStart(string key)
        {
            if(string.IsNullOrEmpty(key))
                return "";
            if(keyDoesStartWithMethodMarker(key))
            {
                return key.Substring(7,key.Length-8);
            } 
            else if(keyDoesStartWithClassMarker(key))
            {
                return key.Substring(5,key.Length-6);
            }
            else 
            {
                Console.WriteLine("a key didn't contain method- or class- as prefix");
                return "";
            }
        }
        private Boolean keyDoesStartWithMethodMarker(string keyBeforeCleaned)
        {
            return keyBeforeCleaned.StartsWith("method-");
        }
        private Boolean keyDoesStartWithClassMarker(string keyBeforeCleaned)
        {
            return keyBeforeCleaned.StartsWith("class-");
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
                    AddSmellToHistory(commit.DateTime.Value, key, smell, commit);
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
                AddSmellToHistory(commit.DateTime.Value, key, smell, commit);
            }
        }

        private void AddSmellToHistory(DateTime smellBirthDate, string key, OrganicSmell smell, Commit commit)
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
                    HaveBeenSeenInTheLastCommit = false,
                    ProjectId = commit.ProjectId,
                    CommitId = commit.Id
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
        public long ProjectId {get; set;}

        public long CommitId {get; set;}
    }
}
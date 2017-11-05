using DataRepository;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using RefactoringAndSmellsSaver.DomainModels;

namespace RefactoringAndSmellsSaver.ResearchQuestions.RQ1
{
    public class AnswerResolver : IAnswerResolver
    {
        Refactoring[] allRefactorings;
        OrganicClass[] allVersionsOfClassesAtCommit;
        OrganicMethod[] allVersionOfMethodAtCommit;
        Dictionary<string, Commit> allCommits;
        OrganicSmell[] allSmells;

        // re-hashed for faster lookup:
        Dictionary<long, List<OrganicClass>> classDictioanryByCommitId;
        Dictionary<long, List<OrganicMethod>> methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit;
        Dictionary<long, List<OrganicSmell>> smellDictioanryByOrganicClassID;
        Dictionary<long, List<OrganicSmell>> smellDictioanryByOrganicMethodID;

        int countOfCommitsThatWerentFoundForAGivenRefactoring = 0;

        HashSet<string> sourceAndTargetClassesNotFound = new HashSet<string>();
        HashSet<string> sourceAndTargetMethodsNotFound = new HashSet<string>();


        public void Resolve()
        {
            long myProjectId = 3; // 3 is gson // 4 is the longest, with 2229 refactorings. // 2 has zero refactorings!!! // 1 is similar to 3, about 549 refactorings, or 506.

            using (var context = new BadSmellMinerDbContext())
            {
                performAllQueriesForProjectAndStoreThem(myProjectId);
                hashAllNecessaryTables();
                findSmellsBeforeAndAfter();
            }
        }


        private void findSmellsBeforeAndAfter()
        {
            // here I want to get all the smells for a given refactoring.
            // 1- for each refactoring get the commitID
            // 2- use that commitID to get all versions-of-class-at-commit
            //      2.1- get all smells for each version-of-class-at-commit
            // 3- get all versionsOfMethods for each version-of-a-class-at-commit
            //      3.1- get all smells for each version-of-method-at-commit
            foreach (Refactoring refactoring in allRefactorings)
            {
                string refactoringCommitId = refactoring.CommitId; // sha hash from git, different from the 1,2,3,4... incremented DB Id (foreign key)
                Commit commit = null;
                try
                {
                    commit = allCommits[refactoringCommitId]; // allCommits does have git hashes (IDs) as dictionary keys ... if the commit is more than 2 years old.
                }
                catch (Exception e)
                {
                    Console.WriteLine(e); // it's probably because Organic writes the Commit table .... I imagine.
                    Console.WriteLine("countOfCommitsThatWerentFoundForAGivenRefactoring ==> " + ++countOfCommitsThatWerentFoundForAGivenRefactoring);
                    continue;
                }

                Console.WriteLine("#################################################################");
                Console.WriteLine("########################## REFACTORING #########################");
                Console.WriteLine("#################################################################");
                Console.WriteLine("refactoring.Type ==> " + refactoring.Type);
                Console.WriteLine("refactoring.Id ==> " + refactoring.Id);
                Console.WriteLine("commit.CommitId ==> " + commit.CommitId);

                Console.WriteLine("refactoring.SourceClassName ==> " + refactoring.SourceClassName);
                Console.WriteLine("refactoring.TargetClassName ==> " + refactoring.TargetClassName);
                Console.WriteLine("refactoring.SourceOperatationName ==> " + refactoring.SourceOperatationName);
                Console.WriteLine("refactoring.TargetOperatationName ==> " + refactoring.TargetOperatationName);



                Console.WriteLine("##################################################################################################################################");                
                Console.WriteLine("!!!!!!!!!!!!!!!!! CLASS SMELLS BEFORE: !!!!!!!!!!!!!!!!!!!!!!");
                List<OrganicClass> classesForCommitBefore = classDictioanryByCommitId[commit.Id - 1];
                IEnumerable<OrganicClass> collectionOfSourceClassOfRefactoringMatchedInCommit = classesForCommitBefore.Where(c => c.FullyQualifiedName == refactoring.SourceClassName);
                HashSet<string> setOfClassSmellsForClassVersionAtCommitBEFORE = getTheSetOfClassSmellsForClassVersionAtCommit_SAFE_HandleUnmatchedSourceAndTargetClassFiles(collectionOfSourceClassOfRefactoringMatchedInCommit, refactoring);


                Console.WriteLine("!!!!!!!!!!!!!!!!! CLASS SMELLS AFTER: !!!!!!!!!!!!!!!!!!!!!!");
                List<OrganicClass> classesForCommitAfter = classDictioanryByCommitId[commit.Id];
                IEnumerable<OrganicClass> collectionOfTargetClassOfRefactoringMatchedInCommit = classesForCommitAfter.Where(c => c.FullyQualifiedName == (string.IsNullOrEmpty(refactoring.TargetClassName) ? refactoring.SourceClassName : refactoring.TargetClassName));
                HashSet<string> setOfClassSmellsForClassVersionAtCommitAFTER = getTheSetOfClassSmellsForClassVersionAtCommit_SAFE_HandleUnmatchedSourceAndTargetClassFiles(collectionOfTargetClassOfRefactoringMatchedInCommit, refactoring);


                // THEN, COMPARE CLASS SMELLS BEFORE & AFTER:
                HashSet<string> classSmellsThatWentAway = setOfClassSmellsForClassVersionAtCommitBEFORE.Except(setOfClassSmellsForClassVersionAtCommitAFTER).ToHashSet();
                HashSet<string> classSmellsThatAppeared = setOfClassSmellsForClassVersionAtCommitAFTER.Except(setOfClassSmellsForClassVersionAtCommitBEFORE).ToHashSet();
                if (classSmellsThatWentAway.Count() < 0)
                {
                    Console.WriteLine("A CLASS REFACTORING REMOVED SOME CODE SMELLS");
                    Console.WriteLine(String.Join(",", classSmellsThatWentAway));
                    //TODO: write them to csv
                }
                if (classSmellsThatAppeared.Count() < 0)
                {
                    Console.WriteLine("A CLASS REFACTORING ADDED SOME CODE SMELLS");
                    Console.WriteLine(String.Join(",", classSmellsThatAppeared));
                    //TODO: write them to csv
                }
                if (setOfClassSmellsForClassVersionAtCommitBEFORE.SetEquals(setOfClassSmellsForClassVersionAtCommitAFTER))
                {
                    Console.WriteLine("A CLASS REFACTORING LEFT DETECTED CLASS SMELLS UNCHANGED !!!!!! ");
                }



                Console.WriteLine("##################################################################################################################################");
                Console.WriteLine("!!!!!!!!!!!!!!!!! METHOD SMELLS BEFORE: !!!!!!!!!!!!!!!!!!!!!!");
                HashSet<string> setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE = getMethodSmellsForMethodForClassVersionAtCommit_SAFE(collectionOfSourceClassOfRefactoringMatchedInCommit, refactoring);

                Console.WriteLine("!!!!!!!!!!!!!!!!! METHOD SMELLS AFTER: !!!!!!!!!!!!!!!!!!!!!!");
                HashSet<string> setOfMethodsSmellsForMethodForClassVersionAtCommitAFTER = getMethodSmellsForMethodForClassVersionAtCommit_SAFE(collectionOfTargetClassOfRefactoringMatchedInCommit, refactoring);

                // // FINALLY: COMPARE METHOD SMELLS BEFORE & AFTER:
                HashSet<string> methodSmellsThatWentAway = setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE.Except(setOfMethodsSmellsForMethodForClassVersionAtCommitAFTER).ToHashSet();
                HashSet<string> methodSmellsThatAppeared = setOfMethodsSmellsForMethodForClassVersionAtCommitAFTER.Except(setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE).ToHashSet();
                if (classSmellsThatWentAway.Count() < 0)
                {
                    Console.WriteLine("A METHOD REFACTORING REMOVED SOME CODE SMELLS");
                    Console.WriteLine(String.Join(",", methodSmellsThatWentAway));
                    //TODO: write them to csv
                }
                if (classSmellsThatAppeared.Count() < 0)
                {
                    Console.WriteLine("A METHOD REFACTORING ADDED SOME CODE SMELLS");
                    Console.WriteLine(String.Join(",", methodSmellsThatAppeared));
                    //TODO: write them to csv
                }
                if (setOfClassSmellsForClassVersionAtCommitBEFORE.SetEquals(setOfClassSmellsForClassVersionAtCommitAFTER))
                {
                    Console.WriteLine("A METHOD REFACTORING LEFT DETECTED METHOD SMELLS UNCHANGED !!!!!! ");
                }



            }
        }



        private HashSet<string> getMethodSmellsForMethodForClassVersionAtCommit_SAFE(IEnumerable<OrganicClass> collectionOfSourceClassOfRefactoringMatchedInCommit, Refactoring refactoring)
        {
            HashSet<string> setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE = new HashSet<string>();
            if (collectionOfSourceClassOfRefactoringMatchedInCommit.Count() == 1)
            {
                var x = refactoring.SourceOperatationName;
                var y = refactoring.TargetOperatationName;

                OrganicClass classVersionAtCommit = collectionOfSourceClassOfRefactoringMatchedInCommit.ToList()[0];
                var methodsForVersionOfClassAtCommit = methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[classVersionAtCommit.Id];
                IEnumerable<OrganicMethod> collectionOfMatchedSourceMethodsAtCommit = methodsForVersionOfClassAtCommit.Where(c => c.FullyQualifiedName == refactoring.SourceOperatationName);
                if (collectionOfMatchedSourceMethodsAtCommit.Count() == 1)
                {
                    OrganicMethod matchedSourceMethodsAtCommit = collectionOfMatchedSourceMethodsAtCommit.ToList()[0];
                    setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE = getMethodSmellsForMethodForClassVersionAtCommit(matchedSourceMethodsAtCommit);
                }
            }
            else
            {
                Console.WriteLine("FILENAME MATCH PROBLEM: not exactly one method was detected in this commit with the refactoring's <source/target> method name.");
                Console.WriteLine("refactoring.SourceOperatationName ==> " + refactoring.SourceOperatationName);
                Console.WriteLine("refactoring.TargetOperatationName ==> " + refactoring.TargetOperatationName);
                Console.WriteLine("list of filenames matching source filename ==> " + String.Join(",", collectionOfSourceClassOfRefactoringMatchedInCommit));
                sourceAndTargetMethodsNotFound.Add(refactoring.SourceOperatationName);
                sourceAndTargetMethodsNotFound.Add(refactoring.TargetClassName);
                Console.WriteLine("sourceAndTargetMethodsNotFound.Count() ==> " + sourceAndTargetMethodsNotFound.Count());
                Console.WriteLine("sourceAndTargetMethodsNotFound.Count ==> " + String.Join(",", sourceAndTargetMethodsNotFound));

            }
            return setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE;
        }

        private HashSet<string> getTheSetOfClassSmellsForClassVersionAtCommit_SAFE_HandleUnmatchedSourceAndTargetClassFiles(IEnumerable<OrganicClass> collectionOfSourceOrTargetClassOfRefactoring, Refactoring refactoring)
        {
            HashSet<string> setOfClassSmellsForClassVersionAtCommitBEFORE = new HashSet<string>();

            if (collectionOfSourceOrTargetClassOfRefactoring.Count() == 1)
            {
                OrganicClass sourceOrTargetClassOfRefactoring = collectionOfSourceOrTargetClassOfRefactoring.ToList()[0];
                setOfClassSmellsForClassVersionAtCommitBEFORE = getTheSetOfClassSmellsForClassVersionAtCommit(sourceOrTargetClassOfRefactoring);
            }
            else
            {
                Console.WriteLine("FILENAME MATCH PROBLEM: not exactly one class was detected in this commit with the refactoring's <source/target> class name.");
                Console.WriteLine("refactoring.SourceClassName ==> " + refactoring.SourceClassName);
                Console.WriteLine("refactoring.TargetClassName ==> " + refactoring.TargetClassName);
                Console.WriteLine("list of filenames matching source filename ==> " + String.Join(",", collectionOfSourceOrTargetClassOfRefactoring));
                sourceAndTargetClassesNotFound.Add(refactoring.SourceClassName);
                sourceAndTargetClassesNotFound.Add(refactoring.TargetClassName);
                Console.WriteLine("sourceAndTargetClassesNotFound.Count() ==> " + sourceAndTargetClassesNotFound.Count());
                Console.WriteLine("sourceAndTargetClassesNotFound ==> " + String.Join(",", sourceAndTargetClassesNotFound));
            }
            return setOfClassSmellsForClassVersionAtCommitBEFORE;

        }

        private HashSet<string> getTheSetOfClassSmellsForClassVersionAtCommit(OrganicClass sourceClassOfRefactoring)
        {
            HashSet<string> setOfClassSmellsForClassVersionAtCommitBEFORE = new HashSet<string>();

            setOfClassSmellsForClassVersionAtCommitBEFORE = findClassSmellsForClassVersionAtCommit(sourceClassOfRefactoring);

            // if there are no smells detected for the source class, then that's weird, eventually continue to the next refactoring:
            if (setOfClassSmellsForClassVersionAtCommitBEFORE.Count() == 0)
            {
                Console.WriteLine("A BIT STRANGE BUT NOT UNCOMMON: there are no smells detected at this commit.");
            }
            else
            {
                Console.WriteLine(String.Join(",", setOfClassSmellsForClassVersionAtCommitBEFORE));
            }
            return setOfClassSmellsForClassVersionAtCommitBEFORE;

        }


        private Dictionary<string, HashSet<string>> getSmellsForEachClassAtCommit(List<OrganicClass> classesForCommit)
        {
            Dictionary<string, HashSet<string>> smellsForEachClassAtCommit = new Dictionary<string, HashSet<string>>();
            foreach (OrganicClass classVersionAtCommit in classesForCommit)
            {
                Console.WriteLine("classVersionAtCommit.FullyQualifiedName ==> " + classVersionAtCommit.FullyQualifiedName);
                HashSet<string> setOfClassSmellsForClassVersionAtCommit = findClassSmellsForClassVersionAtCommit(classVersionAtCommit);
                foreach (var classSmell in setOfClassSmellsForClassVersionAtCommit)
                {
                    Console.WriteLine("classSmell ==> " + classSmell);

                    // populate the dict:
                    if (!smellsForEachClassAtCommit.ContainsKey(classVersionAtCommit.FullyQualifiedName))
                    {
                        smellsForEachClassAtCommit[classVersionAtCommit.FullyQualifiedName] = new HashSet<string>();
                    }
                    smellsForEachClassAtCommit[classVersionAtCommit.FullyQualifiedName].Add(classSmell);

                }
            }
            return smellsForEachClassAtCommit;

        }

        private HashSet<string> getMethodSmellsForMethodForClassVersionAtCommit(OrganicMethod methodVersionAtCommit)
        {
            HashSet<string> setOfMethodsSmellsForMethodForClassVersionAtCommit = new HashSet<string>();
            try
            {
                //get the smells for method:
                // var methodsForVersionOfClassAtCommit = methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[classVersionAtCommit.Id];
                // foreach (var methodVersionAtCommit in methodsForVersionOfClassAtCommit)
                // {
                var smellsForVersionOfMethodAtCommit = smellDictioanryByOrganicMethodID[methodVersionAtCommit.Id];
                //print them:
                foreach (var smellForMethod in smellsForVersionOfMethodAtCommit)
                {
                    Console.WriteLine("************************** findMethodSmellsForMethodForClassVersionAtCommit: *************************** ");
                    //Console.WriteLine("classVersionAtCommit.FullyQualifiedName ==> " + classVersionAtCommit.FullyQualifiedName);
                    Console.WriteLine("methodVersionAtCommit.FullyQualifiedName ==> " + methodVersionAtCommit.FullyQualifiedName);
                    Console.WriteLine("smellForMethod.Name ==> " + smellForMethod.Name);
                }
                // }
            }
            catch (Exception e)
            {
                //Console.WriteLine("{0} Exception caught.", e);
            }

            return setOfMethodsSmellsForMethodForClassVersionAtCommit;

        }

        private HashSet<string> findClassSmellsForClassVersionAtCommit(OrganicClass classVersionAtCommit)
        {
            HashSet<string> setOfClassSmellsForCommit = new HashSet<string>();
            //List<OrganicClass> classesForCommitAfter = classDictioanryByCommitId[commitId];

            // foreach (var classVersionAtCommit in classesForCommitAfter)
            // {
            // get the smells for class-at-commit:
            try
            {
                var smellsForVersionOfClassAtCommit = smellDictioanryByOrganicClassID[classVersionAtCommit.Id];
                // print them:
                foreach (var smellForClass in smellsForVersionOfClassAtCommit)
                {
                    Console.WriteLine("************************** findClassSmellsForClassVersionAtCommit: *************************** ");
                    Console.WriteLine("classInstanceAtCommit.FullyQualifiedName ==> " + classVersionAtCommit.FullyQualifiedName);
                    Console.WriteLine("smellForClass.Name ==> " + smellForClass.Name);

                    // populate the set:
                    setOfClassSmellsForCommit.Add(smellForClass.Name);
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine("{0} Exception caught.", e);
            }


            // }
            return setOfClassSmellsForCommit;
        }


        private void performAllQueriesForProjectAndStoreThem(long myProjectId)
        {
            using (var context = new BadSmellMinerDbContext())
            {
                // get all the refactorings:
                allRefactorings = context.Refactorings.Where(q => q.ProjectId == myProjectId).ToArray(); // this calls the query.
                Console.WriteLine(allRefactorings.Length);
                //var classInstances = context.OrganicClasses.AsNoTracking().ToArray(); // this calls the query.
                allVersionsOfClassesAtCommit = context.OrganicClasses.Where(q => q.Commit.ProjectId == myProjectId).AsNoTracking().ToArray(); // this calls the query.
                allVersionOfMethodAtCommit = context.OrganicMethods.Where(q => q.OrganicClass.Commit.ProjectId == myProjectId).AsNoTracking().ToArray();
                allCommits = context.Commits.Where(q => q.ProjectId == myProjectId).AsNoTracking().ToDictionary(k => k.CommitId); // this calls the query.
                allSmells = context.OrganicSmell.Where(
                    q => q.OrganicClass.Commit.ProjectId == myProjectId
                    || q.OrganicMethod.OrganicClass.Commit.ProjectId == myProjectId
                    ).AsNoTracking().ToArray();
            }


        }
        private void hashAllNecessaryTables()
        {
            // hashify the versions-of-classes-at-commit by commitID: to easily get all class-versions for a given commitID
            classDictioanryByCommitId = new Dictionary<long, List<OrganicClass>>();
            foreach (var classInstance in allVersionsOfClassesAtCommit)
            {
                if (!classDictioanryByCommitId.ContainsKey(classInstance.CommitId)) // an actual DB commitID, not the git sha hash.
                {
                    classDictioanryByCommitId[classInstance.CommitId] = new List<OrganicClass>();
                }
                classDictioanryByCommitId[classInstance.CommitId].Add(classInstance);
            }

            // hashify the methods by version-of-class-at-commit (organicClassId): to easily get all methods for a given class-version-at-commit
            methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit = new Dictionary<long, List<OrganicMethod>>();
            foreach (var methodInstance in allVersionOfMethodAtCommit)
            {
                if (!methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit.ContainsKey(methodInstance.OrganicClassId))
                {
                    methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[methodInstance.OrganicClassId] = new List<OrganicMethod>();
                }
                methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[methodInstance.OrganicClassId].Add(methodInstance);
            }

            // hashify the smells by classID (organicClassId): to easily get all smells for a given class-version-at-commit
            smellDictioanryByOrganicClassID = new Dictionary<long, List<OrganicSmell>>();
            foreach (var smell in allSmells)
            {
                if (smell.OrganicClassId != null)
                {
                    if (!smellDictioanryByOrganicClassID.ContainsKey(smell.OrganicClassId.Value))
                    {
                        smellDictioanryByOrganicClassID[smell.OrganicClassId.Value] = new List<OrganicSmell>();
                    }
                    smellDictioanryByOrganicClassID[smell.OrganicClassId.Value].Add(smell);
                }
            }
            // hashify the smells by methodID (organicMethodId): to easily get all smells for a given method-version-at-commit
            smellDictioanryByOrganicMethodID = new Dictionary<long, List<OrganicSmell>>();
            foreach (var smell in allSmells)
            {
                if (smell.OrganicMethodId != null)
                {
                    if (!smellDictioanryByOrganicMethodID.ContainsKey(smell.OrganicMethodId.Value))
                    {
                        smellDictioanryByOrganicMethodID[smell.OrganicMethodId.Value] = new List<OrganicSmell>();
                    }
                    smellDictioanryByOrganicMethodID[smell.OrganicMethodId.Value].Add(smell);
                }
            }
        }
    }
}
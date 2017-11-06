using DataRepository;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using RefactoringAndSmellsSaver.DomainModels;
using System.IO;

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

        private static int currentProjectId = 0;



        public void Resolve()
        {
            //long myProjectId = 1; // 3 is gson // 4 is the longest, with 2229 refactorings. // 2 has zero refactorings!!! // 1 is similar to 3, about 549 refactorings, or 506.
            int[] projectIdList = {1,2,3,4};
            foreach(var projectIdLoop in projectIdList)
            {
                currentProjectId = projectIdLoop;
                using (var context = new BadSmellMinerDbContext())
                {
                    performAllQueriesForProjectAndStoreThem(myProjectId);
                    hashAllNecessaryTables();
                    findSmellsBeforeAndAfter();
                }
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
                printClassSmellsBeforeAndAfter(commit, refactoring); // now does both methods and classes at the same time.

                Console.WriteLine("##################################################################################################################################");
                //printMethodSmellsBeforeAndAfter(commit, refactoring);
            }
        }


        private void printSmellsThatWentAwayAndSmellsThatAppeared(HashSet<string> setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE, HashSet<string> setOfMethodsSmellsForMethodForClassVersionAtCommitAFTER, Boolean isMethodNotClassRefactoring, Refactoring refactoring, Boolean wasCommonToBothCommits, Boolean wasBeforeOnly, Boolean wasAfterOnly)
        {
            HashSet<string> methodSmellsThatWentAway = setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE.Except(setOfMethodsSmellsForMethodForClassVersionAtCommitAFTER).ToHashSet();
            HashSet<string> methodSmellsThatAppeared = setOfMethodsSmellsForMethodForClassVersionAtCommitAFTER.Except(setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE).ToHashSet();
            if (methodSmellsThatWentAway.Count() > 0)
            {
                Console.WriteLine("A REFACTORING REMOVED SOME CODE SMELLS");
                Console.WriteLine(String.Join(",", methodSmellsThatWentAway));
                if (isMethodNotClassRefactoring)
                {
                    //TODO: EASY --> write them to methodSmellsThatWentAway.csv (only two fields per line: refactoringId, string-method-smell-that-went-away)
                    string filename = "smellsThatDisappearedByMethodRefactorings.csv";
                    foreach (var smell in methodSmellsThatWentAway)
                    {
                        appendLineToCsvFile(
                            filename, 
                            currentProjectId + ", " + 
                            refactoring.Id + ", " + 
                            smell + ", " + 
                            refactoring.SourceClassName + ", " + 
                            isMethodNotClassRefactoring + ", " + 
                            wasCommonToBothCommits + ", " + 
                            wasBeforeOnly + ", " + 
                            wasAfterOnly
                            );
                    }
                }
                else
                {
                    //TODO: EASY --> write them to classSmellsThatWentAway.csv 
                    string filename = "smellsThatDisappearedByClassRefactorings.csv";
                    foreach (var smell in methodSmellsThatWentAway)
                    {
                        appendLineToCsvFile(
                            filename, 
                            currentProjectId + ", " + 
                            refactoring.Id + ", " + 
                            smell + ", " + 
                            refactoring.SourceClassName + ", " + 
                            isMethodNotClassRefactoring + ", " + 
                            wasCommonToBothCommits + ", " + 
                            wasBeforeOnly + ", " + 
                            wasAfterOnly

                        );
                    }
                }
            }
            else if (methodSmellsThatAppeared.Count() > 0)
            {
                Console.WriteLine("A REFACTORING ADDED SOME CODE SMELLS");
                Console.WriteLine(String.Join(",", methodSmellsThatAppeared));
                if (isMethodNotClassRefactoring)
                {
                    //TODO: EASY --> write them to methodSmellsThatAppeared.csv 
                    string filename = "smellsThatAppearedByMethodRefactorings.csv";
                    foreach (var smell in methodSmellsThatAppeared)
                    {
                        appendLineToCsvFile(
                            filename, 
                            currentProjectId + ", " + 
                            refactoring.Id + ", " + 
                            smell + ", " + 
                            refactoring.SourceClassName + ", " + 
                            isMethodNotClassRefactoring + ", " + 
                            wasCommonToBothCommits + ", " + 
                            wasBeforeOnly + ", " + 
                            wasAfterOnly                            
                        );
                    }
                }
                else
                {
                    //TODO: EASY --> write them to classSmellsThatAppeared.csv 
                    string filename = "smellsThatAppearedByClassRefactorings.csv";
                    foreach (var smell in methodSmellsThatAppeared)
                    {
                        appendLineToCsvFile(
                            filename, 
                            currentProjectId + ", " + 
                            refactoring.Id + ", " + 
                            smell + ", " + 
                            refactoring.SourceClassName + ", " + 
                            isMethodNotClassRefactoring + ", " + 
                            wasCommonToBothCommits + ", " + 
                            wasBeforeOnly + ", " + 
                            wasAfterOnly                            
                        );
                    }
                }
            }
            else if (setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE.SetEquals(setOfMethodsSmellsForMethodForClassVersionAtCommitAFTER))
            {
                Console.WriteLine("A REFACTORING LEFT DETECTED METHOD SMELLS UNCHANGED !!!!!! ");
            }
            else 
            {
                Console.Write("ERROR: should never happen: the sets of smells setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE, setOfMethodsSmellsForMethodForClassVersionAtCommitAFTER");
            }
        }

        private void appendLineToCsvFile(string filename, string line)
        {
            using (StreamWriter sw = File.AppendText(filename))
            {
                sw.WriteLine(line);
            }
        }


        private IEnumerable<OrganicClass> getCollectionOfSourceClassOfRefactoringMatchedInCommitBeforeCommit(Commit commit, Refactoring refactoring)
        {
            List<OrganicClass> classesForCommitBefore = classDictioanryByCommitId[commit.Id - 1];
            IEnumerable<OrganicClass> collectionOfSourceClassOfRefactoringMatchedInCommit = classesForCommitBefore;//.Where(c => string.IsNullOrEmpty(c.FullyQualifiedName)? false : c.FullyQualifiedName.Contains(refactoring.SourceClassName));
            return collectionOfSourceClassOfRefactoringMatchedInCommit;

        }


        private IEnumerable<OrganicClass> getCollectionOfSourceClassOfRefactoringMatchedInCommitAfterCommit(Commit commit, Refactoring refactoring)
        {
            List<OrganicClass> classesForCommitAfter = classDictioanryByCommitId[commit.Id];
            IEnumerable<OrganicClass> collectionOfTargetClassOfRefactoringMatchedInCommit = classesForCommitAfter;//.Where(c => string.IsNullOrEmpty(c.FullyQualifiedName)? false : c.FullyQualifiedName.Contains((string.IsNullOrEmpty(refactoring.TargetClassName) ? refactoring.SourceClassName : refactoring.TargetClassName)));
            return collectionOfTargetClassOfRefactoringMatchedInCommit;

        }

        private void printClassSmellsBeforeAndAfter(Commit commit, Refactoring refactoring)
        {
            // in here, instead of doing it only for the matched file before and after....
            // just do it in a loop for each file in the whole commit. 
            // the comparison function will be called as many times as there are files. 
            // unmodified, as it is, the current comparison function will print to csv the refactoringId, and smells-that-disappeared-in-each-file.
            // that's it. that's all.
            List<OrganicClass> classesForCommitBefore = classDictioanryByCommitId[commit.Id - 1];
            List<OrganicClass> classesForCommitAfter = classDictioanryByCommitId[commit.Id];
            var classesForCommitBefore_DICT = classesForCommitBefore.GroupBy(x => x.FullyQualifiedName).ToDictionary(x=>x.Last().FullyQualifiedName,x=>x.Last());//classesForCommitBefore.Select(csvEntry => csvEntry.ToMyObject()).GroupBy(x => x.UniqueKey) ToDictionary(x=>x.FullyQualifiedName,x=>x);
            var classesForCommitAfter_DICT = classesForCommitAfter.GroupBy(x => x.FullyQualifiedName).ToDictionary(x=>x.Last().FullyQualifiedName,x=>x.Last());
            var classesForCommitBefore_DICT_RESULTS = new Dictionary<string, HashSet<string>>();
            var classesForCommitAfter_DICT_RESULTS = new Dictionary<string, HashSet<string>>();

            // collect the set of smells for each class in commits before and after so we can later compare them.
            foreach(var classBefore in classesForCommitBefore_DICT)
            {
                Console.WriteLine("!!!!!!!!!!!!!!!!! CLASS SMELLS BEFORE: !!!!!!!!!!!!!!!!!!!!!!");
                //List<OrganicClass> classesForCommitBefore = classDictioanryByCommitId[commit.Id - 1];
                //IEnumerable<OrganicClass> collectionOfSourceClassOfRefactoringMatchedInCommit = ;//classesForCommitBefore;//.Where(c => string.IsNullOrEmpty(c.FullyQualifiedName)? false : c.FullyQualifiedName.Contains(refactoring.SourceClassName));
                HashSet<string> setOfClassSmellsForClassVersionAtCommitBEFORE = getTheSetOfClassSmellsForClassVersionAtCommit_SAFE_HandleUnmatchedSourceAndTargetClassFiles(classBefore.Value, refactoring);
                classesForCommitBefore_DICT_RESULTS[classBefore.Key] = setOfClassSmellsForClassVersionAtCommitBEFORE;
            }
            foreach(var classAfter in classesForCommitAfter_DICT)
            {
                Console.WriteLine("!!!!!!!!!!!!!!!!! CLASS SMELLS AFTER: !!!!!!!!!!!!!!!!!!!!!!");
                //List<OrganicClass> classesForCommitAfter = classDictioanryByCommitId[commit.Id];
                //IEnumerable<OrganicClass> collectionOfTargetClassOfRefactoringMatchedInCommit = classesForCommitAfter;//.Where(c => string.IsNullOrEmpty(c.FullyQualifiedName)? false : c.FullyQualifiedName.Contains((string.IsNullOrEmpty(refactoring.TargetClassName) ? refactoring.SourceClassName : refactoring.TargetClassName)));
                HashSet<string> setOfClassSmellsForClassVersionAtCommitAFTER = getTheSetOfClassSmellsForClassVersionAtCommit_SAFE_HandleUnmatchedSourceAndTargetClassFiles(classAfter.Value, refactoring);
                classesForCommitAfter_DICT_RESULTS[classAfter.Key] = setOfClassSmellsForClassVersionAtCommitAFTER;
            }

            // 1- compare the common classes: do the set intersection
            var commonClasses_DICT = classesForCommitBefore_DICT_RESULTS.Where(x => classesForCommitAfter_DICT_RESULTS.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            foreach(var classInCommon in commonClasses_DICT)
            {
                // key to use to search:
                var keyToUseToSearch = classInCommon.Key;
                var setOfClassSmellsForClassVersionAtCommitBEFORE = classesForCommitBefore_DICT_RESULTS[keyToUseToSearch];
                var setOfClassSmellsForClassVersionAtCommitAFTER = classesForCommitAfter_DICT_RESULTS[keyToUseToSearch];
                // THEN, COMPARE CLASS SMELLS BEFORE & AFTER:
                printSmellsThatWentAwayAndSmellsThatAppeared(setOfClassSmellsForClassVersionAtCommitBEFORE, setOfClassSmellsForClassVersionAtCommitAFTER, false, refactoring, true, false, false);
                // THEN ALSO for methods:
                printMethodSmellsForMethodForClassVersionAtCommit_SAFE(classesForCommitBefore_DICT[keyToUseToSearch], classesForCommitAfter_DICT[keyToUseToSearch], refactoring);
            }

            // 2- compare the class only in before
            var classesOnlyInBefore_DICT = classesForCommitBefore_DICT_RESULTS.Where(x => !classesForCommitAfter_DICT_RESULTS.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            foreach(var classOnlyBefore in classesOnlyInBefore_DICT)
            {
                // key to use to search:
                var keyToUseToSearch = classOnlyBefore.Key;
                var setOfClassSmellsForClassVersionAtCommitBEFORE = classesForCommitBefore_DICT_RESULTS[keyToUseToSearch];
                var setOfClassSmellsForClassVersionAtCommitAFTER = new HashSet<string>(); // must be empty
                // THEN, COMPARE CLASS SMELLS BEFORE & AFTER:
                printSmellsThatWentAwayAndSmellsThatAppeared(setOfClassSmellsForClassVersionAtCommitBEFORE, setOfClassSmellsForClassVersionAtCommitAFTER, false, refactoring, false, true, false);
                //THEN also for methods:
                printMethodSmellsForMethodForClassVersionAtCommit_SAFE(classesForCommitBefore_DICT[keyToUseToSearch], null, refactoring);
            }

            // 3- compare the classes only in after
            var classesOnlyInAfter_DICT = classesForCommitAfter_DICT_RESULTS.Where(x => !classesForCommitBefore_DICT_RESULTS.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            foreach(var classOnlyInAfter in classesOnlyInAfter_DICT)
            {
                // key to use to search:
                var keyToUseToSearch = classOnlyInAfter.Key;
                var setOfClassSmellsForClassVersionAtCommitBEFORE = new HashSet<string>(); // must be empty
                var setOfClassSmellsForClassVersionAtCommitAFTER = classesForCommitAfter_DICT_RESULTS[keyToUseToSearch];
                // THEN, COMPARE CLASS SMELLS BEFORE & AFTER:
                printSmellsThatWentAwayAndSmellsThatAppeared(setOfClassSmellsForClassVersionAtCommitBEFORE, setOfClassSmellsForClassVersionAtCommitAFTER, false, refactoring, false, false, true);
                //THEN also for methods:
                printMethodSmellsForMethodForClassVersionAtCommit_SAFE(null, classesForCommitAfter_DICT[keyToUseToSearch], refactoring);
            }
            Console.WriteLine();

        }


        private void printMethodSmellsForMethodForClassVersionAtCommit_SAFE(OrganicClass sourceClass, OrganicClass targetClass, Refactoring refactoring)
        {
            // here we have only one class
            // and we write to csv the smell diff like for classes, in commit before and after the refactor
            HashSet<string> setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE = new HashSet<string>();
            List<OrganicMethod> methodsForVersionOfClassAtCommitBEFORE = null;
            List<OrganicMethod> methodsForVersionOfClassAtCommitAFTER = null;
            try
            {
                methodsForVersionOfClassAtCommitBEFORE = sourceClass == null? new List<OrganicMethod>() : methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[sourceClass.Id];
            }
            catch
            {
                methodsForVersionOfClassAtCommitBEFORE = new List<OrganicMethod>();
            }
            
            try
            {
                methodsForVersionOfClassAtCommitAFTER = targetClass == null? new List<OrganicMethod>() : methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[targetClass.Id];
            }
            catch
            {
                methodsForVersionOfClassAtCommitAFTER = new List<OrganicMethod>();
            }
            
            Dictionary<string, HashSet<string>> methodNameToSmellsBEFORE_DICT_RESULTS = new Dictionary<string, HashSet<string>>();
            Dictionary<string, HashSet<string>> methodNameToSmellsAFTER_DICT_RESULTS = new Dictionary<string, HashSet<string>>();

            // collect to compare:
            foreach(var sourceMethodAtCommit in methodsForVersionOfClassAtCommitBEFORE)
            {
                setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE = getMethodSmellsForMethodForClassVersionAtCommit(sourceMethodAtCommit);
                try { methodNameToSmellsBEFORE_DICT_RESULTS[sourceMethodAtCommit.FullyQualifiedName] = setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE; } catch{continue;}
            }
            foreach(var sourceMethodAtCommit in methodsForVersionOfClassAtCommitAFTER)
            {
                setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE = getMethodSmellsForMethodForClassVersionAtCommit(sourceMethodAtCommit);
                try { methodNameToSmellsAFTER_DICT_RESULTS[sourceMethodAtCommit.FullyQualifiedName] = setOfMethodsSmellsForMethodForClassVersionAtCommitBEFORE; } catch {continue;}            
            }

            // compare commons methods:
            // get the common ones: 
            var commonMethods_DICT = methodNameToSmellsBEFORE_DICT_RESULTS.Where(x => methodNameToSmellsAFTER_DICT_RESULTS.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            foreach(var methodInCommon in commonMethods_DICT)
            {
                // key to use to search:
                var keyToUseToSearch = methodInCommon.Key;
                var setOfMethodSmellsForMethodForClassVersionAtCommitBEFORE = methodNameToSmellsBEFORE_DICT_RESULTS[keyToUseToSearch];
                var setOfMethodSmellsForMethodForClassVersionAtCommitAFTER = methodNameToSmellsAFTER_DICT_RESULTS[keyToUseToSearch];
                // THEN, COMPARE CLASS SMELLS BEFORE & AFTER:
                printSmellsThatWentAwayAndSmellsThatAppeared(setOfMethodSmellsForMethodForClassVersionAtCommitBEFORE, setOfMethodSmellsForMethodForClassVersionAtCommitAFTER, true, refactoring, true, false, false);
            }

            // compare methods only in before:
            var methodsOnlyInBefore_DICT = methodNameToSmellsBEFORE_DICT_RESULTS.Where(x => !methodNameToSmellsAFTER_DICT_RESULTS.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            foreach(var methodInBeforeOnly in methodsOnlyInBefore_DICT)
            {
                var keyToUse = methodInBeforeOnly.Key;
                HashSet<string> setOfSmellsBEFORE = methodNameToSmellsBEFORE_DICT_RESULTS[keyToUse];
                HashSet<string> setOfSmellsAFTER = new HashSet<string>();

                printSmellsThatWentAwayAndSmellsThatAppeared(setOfSmellsBEFORE, setOfSmellsAFTER, true, refactoring, false, true, false);

            }
            
            // compare methods only in after:
            var methodsOnlyInAfter_DICT = methodNameToSmellsAFTER_DICT_RESULTS.Where(x => !methodNameToSmellsBEFORE_DICT_RESULTS.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            foreach(var methodInAfterOnly in methodsOnlyInAfter_DICT)
            {
                var keyToUse = methodInAfterOnly.Key;
                HashSet<string> setOfSmellsBEFORE = new HashSet<string>();
                HashSet<string> setOfSmellsAFTER = methodNameToSmellsAFTER_DICT_RESULTS[keyToUse];

                printSmellsThatWentAwayAndSmellsThatAppeared(setOfSmellsBEFORE, setOfSmellsAFTER, true, refactoring, false, false, true);

            }
        }

        private HashSet<string> getTheSetOfClassSmellsForClassVersionAtCommit_SAFE_HandleUnmatchedSourceAndTargetClassFiles(OrganicClass sourceOrTargetClassOfRefactoring, Refactoring refactoring)
        {
            HashSet<string> setOfClassSmellsForClassVersionAtCommitBEFORE = new HashSet<string>();
            setOfClassSmellsForClassVersionAtCommitBEFORE = getTheSetOfClassSmellsForClassVersionAtCommit(sourceOrTargetClassOfRefactoring);
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
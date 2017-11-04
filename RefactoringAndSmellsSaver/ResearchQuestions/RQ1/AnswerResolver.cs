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


        public void Resolve()
        {
            long myProjectId = 3; // 3 is gson

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
            foreach (var refactoring in allRefactorings)
            {
                var refactoringCommitId = refactoring.CommitId; // sha hash from git, different from the 1,2,3,4... incremented DB Id (foreign key)
                var commit = allCommits[refactoringCommitId]; // db incremented ID.

                Console.WriteLine("#################################################################");
                Console.WriteLine("##########################£ REFACTORING #########################");
                Console.WriteLine("#################################################################");
                Console.WriteLine("refactoring.Type ==> " + refactoring.Type);
                Console.WriteLine("refactoring.Id ==> " + refactoring.Id);
                Console.WriteLine("commit.CommitId ==> " + commit.CommitId);

                Console.WriteLine("!!!!!!!!!!!!!!!!! SMELLS BEFORE: !!!!!!!!!!!!!!!!!!!!!!");
                findSmellsForCommit(commit.Id - 1);

                Console.WriteLine("!!!!!!!!!!!!!!!!! SMELLS AFTER: !!!!!!!!!!!!!!!!!!!!!!");
                findSmellsForCommit(commit.Id);
            }

        }

        private void findSmellsForCommit(long commitId)
        {
            List<OrganicClass> classesForCommitAfter = classDictioanryByCommitId[commitId];

            foreach (var classVersionAtCommit in classesForCommitAfter)
            {
                // get the smells for class-at-commit:
                try
                {
                    var smellsForVersionOfClassAtCommit = smellDictioanryByOrganicClassID[classVersionAtCommit.Id];
                    // print them:
                    foreach (var smellForClass in smellsForVersionOfClassAtCommit)
                    {
                        Console.WriteLine("************************** smells for a version of CLASS at commit: *************************** ");
                        Console.WriteLine("classInstanceAtCommit.FullyQualifiedName ==> " + classVersionAtCommit.FullyQualifiedName);
                        Console.WriteLine("smellForClass.Name ==> " + smellForClass.Name);
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine("{0} Exception caught.", e);
                }

                try
                {
                    //get the smells for method:
                    var methodsForVersionOfClassAtCommit = methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[classVersionAtCommit.Id];
                    foreach (var methodVersionAtCommit in methodsForVersionOfClassAtCommit)
                    {
                        var smellsForVersionOfMethodAtCommit = smellDictioanryByOrganicMethodID[methodVersionAtCommit.Id];
                        //print them:
                        foreach (var smellForMethod in smellsForVersionOfMethodAtCommit)
                        {
                            Console.WriteLine("************************** smells for a version of METHOD at commit: *************************** ");
                            Console.WriteLine("classVersionAtCommit.FullyQualifiedName ==> " + classVersionAtCommit.FullyQualifiedName);
                            Console.WriteLine("methodVersionAtCommit.FullyQualifiedName ==> " + methodVersionAtCommit.FullyQualifiedName);
                            Console.WriteLine("smellForMethod.Name ==> " + smellForMethod.Name);
                        }

                    }

                }
                catch (Exception e)
                {
                    //Console.WriteLine("{0} Exception caught.", e);
                }

            }
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
                if (!classDictioanryByCommitId.ContainsKey(classInstance.CommitId))
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
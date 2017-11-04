using System;
using System.Diagnostics;
using System.IO;
using LibGit2Sharp;
using Newtonsoft.Json;
using DataRepository;
using System.Linq;
using RefactoringAndSmellsSaver.DomainModels;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RefactoringAndSmellsSaver
{
    class Program
    {
        private static string _eclipsePath = @"E:\Developing\eclipse-committers-oxygen-R-win32-x86_64\eclipse";
        private static string _equinoxPluginFile ="org.eclipse.equinox.launcher_1.4.0.v20161219-1356.jar";
        static void Main(string[] args)
        {
            if(args[0]=="save-refactorings")
                SaveRefactorings(args);
            else if(args[0]=="save-smells")
                DetectBadSmells(args);
            else if(args[0]=="rq7")
                new ResearchQuestions.RQ7.AnswerResolver().Resolve();
            else if(args[0]=="rq3")
                new ResearchQuestions.RQ3.AnswerResolver().Resolve();
        }

        private static void SaveRefactorings(string[] args)
        {
            var projectName = args[1];
            var refactoringFolderPath =  args[2];

            var refactorings = ExtractRefactorings(refactoringFolderPath);

            SaveRefactorings(projectName,refactorings);
        }

        private static void SaveRefactorings(string projectName, Refactoring[] refactorings)
        {
             using (var context = new BadSmellMinerDbContext())
            {
                var project = CreateProject(projectName, context);
                
                context.Refactorings.AddRange(refactorings);

                foreach (var refactoring in refactorings)
                {
                    refactoring.ProjectId=project.Id;
                }

                context.SaveChanges();
            }
        }

        private static Refactoring[] ExtractRefactorings(string refactoringFolderPath)
        {
            var files=Directory.GetFiles(refactoringFolderPath);
            var refactorings = new List<Refactoring>();

            foreach(var file in files)
            {
                var lines=File.ReadAllLines(file);
                
                foreach (var line in lines)
                {
                    var parts=line.Split(",");
                    refactorings.Add(new Refactoring(){
                        CommitId=Path.GetFileNameWithoutExtension(file),
                        Type=parts[0],
                        SourceClassName=parts[1],
                        SourceClassPackageName=parts[2],
                        SourceClassPath=parts[3],
                        TargetClassName=parts[4],
                        TargetClassPackageName=parts[5],
                        TargetClassPath=parts[6],
                        SourceOperatationName=parts[7],
                        TargetOperatationName=parts[8],
                        SourceAttributeName=parts[9],
                    });
                }
            }

            return refactorings.ToArray();
        }

        private static void QueryForResearchQuestion1()
        {
            long myProjectId = 3; // 3 is gson
            //var commitsHash = 
            var context = new BadSmellMinerDbContext();
            //context.ChangeTracker.AutoDetectChangesEnabled=false;
            //context=false;
            //context.OrganicClasses.Where(q=>q.FullyQualifiedName=="").ToArray(); // this calls the query.
            
            // get all the refactorings:
            var allRefactorings = context.Refactorings.Where(q=>q.ProjectId==myProjectId).ToArray(); // this calls the query.
            Console.WriteLine(allRefactorings.Length);
            //var classInstances = context.OrganicClasses.AsNoTracking().ToArray(); // this calls the query.
            var allVersionsOfClassesAtCommit = context.OrganicClasses.Where(q=>q.Commit.ProjectId==myProjectId).AsNoTracking().ToArray(); // this calls the query.
            var allVersionOfMethodAtCommit = context.OrganicMethods.Where(q=>q.OrganicClass.Commit.ProjectId==myProjectId).AsNoTracking().ToArray();
            var allCommits = context.Commits.Where(q=>q.ProjectId==myProjectId).AsNoTracking().ToDictionary(k=>k.CommitId); // this calls the query.
            var allSmells = context.OrganicSmell.Where(
                q=>q.OrganicClass.Commit.ProjectId==myProjectId
                || q.OrganicMethod.OrganicClass.Commit.ProjectId==myProjectId
                ).AsNoTracking().ToArray();

            // hashify the versions-of-classes-at-commit by commitID: to easily get all class-versions for a given commitID
            var classDictioanryByCommitId = new Dictionary<long, List<OrganicClass>>();
            foreach (var classInstance in allVersionsOfClassesAtCommit)
            {
                if (!classDictioanryByCommitId.ContainsKey(classInstance.CommitId))
                {
                    classDictioanryByCommitId[classInstance.CommitId] = new List<OrganicClass>();
                }
                classDictioanryByCommitId[classInstance.CommitId].Add(classInstance);
            }

            // hashify the methods by version-of-class-at-commit (organicClassId): to easily get all methods for a given class-version-at-commit
            var methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit = new Dictionary<long, List<OrganicMethod>>();
            foreach (var methodInstance in allVersionOfMethodAtCommit)
            {
                if (!methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit.ContainsKey(methodInstance.OrganicClassId))
                {
                    methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[methodInstance.OrganicClassId] = new List<OrganicMethod>();
                }
                methodDictioanryByOrganicClassIdWhichAreDistinctVersionsOfAClasseAtEachDifferentCommit[methodInstance.OrganicClassId].Add(methodInstance);
            }

            // hashify the smells by classID (organicClassId): to easily get all smells for a given class-version-at-commit
            var smellDictioanryByOrganicClassID = new Dictionary<long, List<OrganicSmell>>();
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
            var smellDictioanryByOrganicMethodID = new Dictionary<long, List<OrganicSmell>>();
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
                var classesForCommitAfter = classDictioanryByCommitId[commit.Id];
                var classesForCommitBefore = classDictioanryByCommitId[commit.Id-1];

                Console.WriteLine("#################################################################");
                Console.WriteLine("##########################£ REFACTORING #########################");
                Console.WriteLine("#################################################################");
                Console.WriteLine("refactoring.Type ==> " + refactoring.Type);
                Console.WriteLine("refactoring.Id ==> " + refactoring.Id);

                Console.WriteLine("!!!!!!!!!!!!!!!!! SMELLS AFTER: !!!!!!!!!!!!!!!!!!!!!!");

                foreach(var classVersionAtCommit in classesForCommitAfter)
                {
                    // get the smells for class-at-commit:
                    try
                    {
                        var smellsForVersionOfClassAtCommit = smellDictioanryByOrganicClassID[classVersionAtCommit.Id];
                        // print them:
                        foreach (var smellForClass in smellsForVersionOfClassAtCommit)
                        {
                            Console.WriteLine("************************** smells for a version of CLASS at commit: *************************** ");
                            Console.WriteLine("commit.CommitId ==> "+commit.CommitId);
                            Console.WriteLine("classInstanceAtCommit.FullyQualifiedName ==> "+classVersionAtCommit.FullyQualifiedName);
                            Console.WriteLine("smellForClass.Name ==> "+ smellForClass.Name);
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
                                Console.WriteLine("refactoring.Type ==> "+refactoring.Type);
                                Console.WriteLine("refactoring.Id ==> "+refactoring.Id);
                                Console.WriteLine("commit.CommitId ==> " + commit.CommitId);
                                Console.WriteLine("classVersionAtCommit.FullyQualifiedName ==> " + classVersionAtCommit.FullyQualifiedName);
                                Console.WriteLine("methodVersionAtCommit.FullyQualifiedName ==> " + methodVersionAtCommit.FullyQualifiedName);
                                Console.WriteLine("smellForMethod.Name ==> " + smellForMethod.Name);
                            }

                        }

                    }
                    catch(Exception e)
                    {
                        //Console.WriteLine("{0} Exception caught.", e);
                    }

                }
                
            }

            //var methods = context.OrganicMethods.ToArray(); // not optimized
            //var methods = context.OrganicMethods.AsNoTracking
            //var methods = context.OrganicMethods.AsNoTracking().ToArray();
            //Console.WriteLine(allVersionOfMethodAtCommit.Length);
            
        }

        private static void DetectBadSmells(string[] args)
        {
            var clonePath = args[2] ;// "https://github.com/google/gson.git";
            var projectName = args[1];// "gson";
            var projectPath= args[3];// $@"C:\Users\Ehsan\Desktop\BadSmellMiner\projects\{projectName}";
            var localRepoPath = $@"{projectPath}\source";
            var branchName="master";

            CloneRepostory(clonePath, localRepoPath);

            WalkIntoProjectHistory(projectName, localRepoPath,projectPath,branchName,HandleCheckout);
        }

        private static void WalkIntoProjectHistory(string projectName, string localRepoPath
        ,string projectPath,string branchName,Action<LibGit2Sharp.Commit, string,string,string> CheckoutHandler)
        {
            using (var repo = new Repository(localRepoPath))
            {
                foreach (var commit in repo.Branches[branchName].Commits)
                {
                    var checkoutOptions = new CheckoutOptions();
                    checkoutOptions.CheckoutModifiers = CheckoutModifiers.Force;
                    repo.Checkout(commit, checkoutOptions);

                    CheckoutHandler(commit, projectName,projectPath,branchName);
                }
            }
        }

        private static void HandleCheckout(LibGit2Sharp.Commit checkoutCommit, string projectName, string projectPath,string branchName)
        {
            using (var context = new BadSmellMinerDbContext())
            {
                if(context.Commits.Any(q=>q.CommitId==checkoutCommit.Sha))
                    return;

                var organicAnalysisFilePath = RunOrganic(checkoutCommit.Sha, projectName, projectPath);

                var project = CreateProject(projectName, context);
                
                var organicClasses=ExtractOrganicClasses(organicAnalysisFilePath);

                CreateNewCommit(project,organicClasses,branchName,checkoutCommit,context);
            }
        }

        private static void CreateNewCommit(Project project, OrganicClass[] organicClasses,string branchName, LibGit2Sharp.Commit checkoutCommit, BadSmellMinerDbContext context)
        {
            var commit = new DomainModels.Commit(project.Id, checkoutCommit.Sha, organicClasses);
            commit.AuthorName=checkoutCommit.Committer.Email;
            commit.FullMessage=checkoutCommit.Message;
            commit.ShortMessage=checkoutCommit.MessageShort;
            commit.DateTime= checkoutCommit.Committer.When.DateTime;
            commit.BranchName=branchName;
            
            context.Commits.Add(commit);

            context.SaveChanges();
        }

        private static OrganicClass[] ExtractOrganicClasses(string organicAnalysisFilePath)
        {
            var smells = File.ReadAllText(organicAnalysisFilePath);

            var organicClasses = JsonConvert.DeserializeObject<OrganicClass[]>(smells);

            return organicClasses;
        }

        private static Project CreateProject(string projectName, BadSmellMinerDbContext context)
        {
            var project = context.Projects.FirstOrDefault(q => q.Name == projectName);

            if (project == null)
            {
                project = new Project(projectName);
                context.Projects.Add(project);
                context.SaveChanges();
            }

            return project;
        }

        private static string RunOrganic(string commitSha, string projectName, string localRepoPath)
        {
            var equinox = $@"{_eclipsePath}\plugins\{_equinoxPluginFile} org.eclipse.core.launcher.Main";
            var smellDetector = "smell-detector-plugin.SmellDetector";

            var commitDirectory=$@"{localRepoPath}\commits\{commitSha}";
            Directory.CreateDirectory(commitDirectory);

            var dest = $@"{commitDirectory}\smells.json";
            var command = $"-jar -XX:MaxPermSize=2560m -Xms40m -Xmx2500m {equinox} -application {smellDetector} -output={dest} -src={localRepoPath}";

            var processInfo = new ProcessStartInfo("java.exe", command)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var process = Process.Start(processInfo);
            process.WaitForExit();
            int exitCode = process.ExitCode;
            process.Dispose();

            return dest;
        }

        private static void CloneRepostory(string clonePath, string localRepoPath)
        {
            if(!Directory.Exists(localRepoPath) || Directory.GetFiles(localRepoPath).Count()==0)
            {
                Directory.CreateDirectory(localRepoPath);
                Repository.Clone(clonePath, localRepoPath);
            }
                
        }
    }
}

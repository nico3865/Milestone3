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
            else if(args[0]=="rq1")
                new ResearchQuestions.RQ1.AnswerResolver().Resolve();
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

import java.util.List;

import org.eclipse.jgit.lib.Repository;
import org.eclipse.jgit.revwalk.RevCommit;
//import org.eclipse.jgit.lib.Repository;
//import org.kohsuke.github.GHEventPayload.Repository;
import org.refactoringminer.api.GitHistoryRefactoringMiner;
import org.refactoringminer.api.GitService;
import org.refactoringminer.api.Refactoring;
import org.refactoringminer.api.RefactoringHandler;
import org.refactoringminer.rm1.GitHistoryRefactoringMinerImpl;
import org.refactoringminer.util.GitServiceImpl;
import org.refactoringminer.utils.RefactoringCollector;

//import org.kohsuke.github.GHEventPayload.Repository;
//import org.refactoringminer.api.GitHistoryRefactoringMiner;
////import org.refactoringminer.api.GitService;
//import org.refactoringminer.api.Refactoring;
//import org.refactoringminer.api.RefactoringHandler;
////import org.refactoringminer.rm1.GitHistoryRefactoringMinerImpl;
//import org.refactoringminer.util.GitServiceImpl;

public class Main {

	public static void main(String[] args) throws Exception {
		
		//WORKS:
		GitService gitService = new GitServiceImpl();
		GitHistoryRefactoringMiner miner = new GitHistoryRefactoringMinerImpl();

		//Repository repo = gitService.openRepository("/Users/nicolasg-chausseau/eclipse-workspace-commiters/SOEN6491_Midterm_2017");//SOEN691_Midterm_2015_bis
		//Repository repo = gitService.openRepository("/Users/nicolasg-chausseau/eclipse-workspace-commiters/SOEN691_Midterm_2015_bis");
		//Repository repo = gitService.cloneIfNotExists("tmp/refactoring-toy-example", "https://github.com/danilofes/refactoring-toy-example.git");
		//Repository repo = gitService.cloneIfNotExists("tmp/refactoring-toy-example", "https://github.com/danilofes/refactoring-toy-example.git");
		Repository repo = gitService.cloneIfNotExists("tmp/RETROFIT", "https://github.com/square/retrofit.git");
		System.out.println("*****************");
		
		miner.detectAll(repo, "master", new RefactoringHandler() {
			@Override
			public void handle(RevCommit commitData, List<Refactoring> refactorings) {
				
				if(!refactorings.isEmpty()) {
					
					System.out.println("================================================commit info==================================================");
					System.out.println("commitData.getId().getName(): " + commitData.getId().getName());
					System.out.println("getShortMessage(): " + commitData.getShortMessage());
					System.out.println("commitData.getCommitTime(): " + commitData.getCommitTime());
					System.out.println("commitData.getEncodingName(): " + commitData.getEncodingName());
					System.out.println("commitData.getFullMessage(): " + commitData.getFullMessage());
					System.out.println("commitData.getName(): " + commitData.getName());
					System.out.println("commitData.getParentCount(): " + commitData.getParentCount());
					System.out.println("commitData.toString(): " + commitData.toString());
					System.out.println("commitData.getCommitterIdent(): " + commitData.getCommitterIdent());
					System.out.println("commitData.getCommitterIdent(): " + commitData.getCommitterIdent().getName());
					System.out.println("commitData.getCommitterIdent(): " + commitData.getCommitterIdent().getEmailAddress());
					System.out.println("commitData.getCommitterIdent(): " + commitData.getCommitterIdent().getWhen());
					System.out.println("commitData.getCommitterIdent(): " + commitData.getCommitterIdent().getTimeZone());
					System.out.println("commitData.getAuthorIdent(): " + commitData.getAuthorIdent());
					//System.out.println("commitData.getAuthorIdent(): " + commitData.);
					
					for (Refactoring ref : refactorings) {
						
						System.out.println(">>>>>>>>>refactoring info>>>>>>>>>");
						System.out.println(ref.toString());
						System.out.println(ref.getRefactoringType());
						System.out.println(ref.getName());
						
					}
					
					System.out.println("================================================END COMMIT==================================================");
					
				}
				
				
				// ORGANIC:
				// --0. TEST to see if the folder changes
				// 1. get the folder ... that we defined above: tmp/RETROFIT
				// 2. not run git, but directly run Organic a first time (to get the smells after)
				// OPTION1 no:
					// 3. checkout previous commit
					// 4. run Organic again
					// 5. checkout the current commit AGAIN (so that refactoring miner isn't affected).
				// OPTION2:
					// 1. we store the organic output (10seconds only) at every handle callback call.
					// 2. only do a diff if there was a refactoring.
				
				
				// is it in a test folder:
				// manually get the path of the test folder for each roject
				// we search for the class name (inside ref.toString()) ... and search for that class file in the tmp/RETROFIT .... and then get its full path and check if it's the hardcoded test path for that project  
				
				// LATER ---> JDEODORANT CMD LINE:
				// 1. run git with commit number for the project in the other workspace (jfreechart) -->	
					// 1.1: run Maven or Gradle automatically (special command for making an eclipse project mvn;eclipse --gradle eclipse_build) 
				// 2. with the previous commit before the refactoring. 
				

			}
			
			// NB: after installing RefactoringMiner --> don't forget to add the call for the callback just above with signature (1):
			// --> handle(RevCommit commitData, List<Refactoring> refactorings)
			// To do so, get the call hierarchy for the similar callback just below (2):
			// --> handle(String commitId, List<Refactoring> refactorings)
			// and just below the call for this callback (2), add the call for the other callback (1)
			// then finally the callback above will work.
			@Override
			public void handle(String commitId, List<Refactoring> refactorings) {
				System.out.println("==============================================");
				System.out.println("@every_commit!!!");
				System.out.println("Refactorings at " + commitId);
				for (Refactoring ref : refactorings) {
					System.out.println(">>>>>>>>>>>>>>>>>>");
					System.out.println(ref.toString());
					System.out.println(ref.getRefactoringType());
					System.out.println(ref.getName());
				}
			}
			
			public void onFinish(int refactoringsCount, int commitsCount, int errorCommitsCount) {
				System.out.println("refactoringsCount ==> "+refactoringsCount);
				System.out.println("commitsCount ==> "+commitsCount);
				System.out.println("errorCommitsCount ==> "+errorCommitsCount);
				System.out.println("@finish!!!");
			}
		});
		
		//miner.detectAll(repo, "master", new AnalyzeProjectsHandler()); //RefactoringHandler
		
		//WORKS
		//miner.detectAll(repo, "master", new RefactoringCollector("https://github.com/square/retrofit.git", "b16374e5c5624bd138f93164026b7b12150d1962"));
		
		
	}

}

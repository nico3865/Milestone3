package com.company;

        import java.io.FileWriter;
        import java.io.IOException;
        import java.io.PrintWriter;
        import java.util.ArrayList;
        import java.util.List;
        import gr.uom.java.xmi.diff.*;
        import org.eclipse.jgit.lib.Repository;
        import org.refactoringminer.api.GitHistoryRefactoringMiner;
        import org.refactoringminer.api.GitService;
        import org.refactoringminer.api.Refactoring;
        import org.refactoringminer.api.RefactoringHandler;
        import org.refactoringminer.rm1.GitHistoryRefactoringMinerImpl;
        import org.refactoringminer.util.GitServiceImpl;

public class Main {

    public static void main(String[] args) throws Exception {

        GitService gitService = new GitServiceImpl();
        GitHistoryRefactoringMiner miner = new GitHistoryRefactoringMinerImpl();

        String projectName="okhttp";
        String localRepoPath="Projects\\"+projectName+"\\source";
        String clonePath="https://github.com/square/okhttp.git";
        String branchName="master";
        String resultPath="Projects\\"+projectName+"\\refactorings";


        Repository repo = gitService.cloneIfNotExists(localRepoPath,clonePath);

        miner.detectAll(repo, branchName, new RefactoringHandler() {
            @Override
            public void handle(String commitId, List<Refactoring> refactorings) {

                ArrayList<RefactoringTemplate> refactoringTemplates= new ArrayList<>();

                for(int i=0;i<refactorings.size();i++){
                    RefactoringTemplate refactoringTemplate= null;
                    try {
                        refactoringTemplate = GetRefactoringTemplate(commitId,refactorings.get(i));
                    } catch (Exception e) {
                        e.printStackTrace();
                    }
                    refactoringTemplates.add(refactoringTemplate);
                }

                try {
                    SaveRefactoringTemplates(refactoringTemplates,resultPath,commitId);
                } catch (IOException e) {
                    e.printStackTrace();
                }

            }
        });

    }

    private static void SaveRefactoringTemplates(ArrayList<RefactoringTemplate> refactoringTemplates, String resultPath, String commitId) throws IOException {

        String path=resultPath+"\\"+commitId+".txt";

        FileWriter fileWriter = new FileWriter(path);
        PrintWriter printWriter = new PrintWriter(fileWriter);

        for ( int i=0;i<refactoringTemplates.size();i++){

            RefactoringTemplate refactoringTemplate=refactoringTemplates.get(i);
            printWriter.printf("%s,%s,%s,%s,%s,%s,%s,%s,%s,%s"
                    ,refactoringTemplate.refactoringName
                    ,refactoringTemplate.sourceClass!=null?refactoringTemplate.sourceClass.getName():""
                    ,refactoringTemplate.sourceClass!=null?refactoringTemplate.sourceClass.getPackageName():""
                    ,refactoringTemplate.sourceClass!=null?refactoringTemplate.sourceClass.getSourceFile():""
                    ,refactoringTemplate.targetClass!=null?refactoringTemplate.targetClass.getName():""
                    ,refactoringTemplate.targetClass!=null?refactoringTemplate.targetClass.getPackageName():""
                    ,refactoringTemplate.targetClass!=null?refactoringTemplate.targetClass.getSourceFile():""
                    ,refactoringTemplate.sourceOperation!=null?refactoringTemplate.sourceOperation.getName():""
                    ,refactoringTemplate.targetOperation!=null?refactoringTemplate.targetOperation.getName():""
                    ,refactoringTemplate.sourceAttribute!=null?refactoringTemplate.sourceAttribute.getName():"");
            printWriter.println();

        }


        printWriter.close();

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, Refactoring refactoring) throws Exception {

        if(refactoring instanceof MoveAttributeRefactoring){
            return GetRefactoringTemplate(commitId,(MoveAttributeRefactoring)refactoring);
        }

        if(refactoring instanceof ConvertAnonymousClassToTypeRefactoring){
            return GetRefactoringTemplate(commitId,(ConvertAnonymousClassToTypeRefactoring)refactoring);
        }

        if(refactoring instanceof ExtractAndMoveOperationRefactoring){
            return GetRefactoringTemplate(commitId,(ExtractAndMoveOperationRefactoring)refactoring);
        }

        if(refactoring instanceof ExtractOperationRefactoring){
            return GetRefactoringTemplate(commitId,(ExtractOperationRefactoring)refactoring);
        }

        if(refactoring instanceof ExtractSuperclassRefactoring){
            return GetRefactoringTemplate(commitId,(ExtractSuperclassRefactoring)refactoring);
        }

        if(refactoring instanceof InlineOperationRefactoring){
            return GetRefactoringTemplate(commitId,(InlineOperationRefactoring)refactoring);
        }

        if(refactoring instanceof MoveClassRefactoring){
            return GetRefactoringTemplate(commitId,(MoveClassRefactoring)refactoring);
        }

        if(refactoring instanceof MoveOperationRefactoring){
            return GetRefactoringTemplate(commitId,(MoveOperationRefactoring)refactoring);
        }

        if(refactoring instanceof MoveSourceFolderRefactoring){
            return GetRefactoringTemplate(commitId,(MoveSourceFolderRefactoring)refactoring);
        }

        if(refactoring instanceof PullUpAttributeRefactoring){
            return GetRefactoringTemplate(commitId,(PullUpAttributeRefactoring)refactoring);
        }

        if(refactoring instanceof PullUpOperationRefactoring){
            return GetRefactoringTemplate(commitId,(PullUpOperationRefactoring)refactoring);
        }

        if(refactoring instanceof PushDownAttributeRefactoring){
            return GetRefactoringTemplate(commitId,(PushDownAttributeRefactoring)refactoring);
        }

        if(refactoring instanceof PushDownOperationRefactoring){
            return GetRefactoringTemplate(commitId,(PushDownOperationRefactoring)refactoring);
        }

        if(refactoring instanceof RenameClassRefactoring){
            return GetRefactoringTemplate(commitId,(RenameClassRefactoring)refactoring);
        }

        if(refactoring instanceof RenameOperationRefactoring){
            return GetRefactoringTemplate(commitId,(RenameOperationRefactoring)refactoring);

        }
        if(refactoring instanceof RenamePackageRefactoring){
            return GetRefactoringTemplate(commitId,(RenamePackageRefactoring)refactoring);
        }

        throw new Exception("Something unexpected happened");
    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, ExtractSuperclassRefactoring refactoring) {

        return new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getExtractedClass(),null);

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, InlineOperationRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate = new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getInlinedOperation().getClassOwner(),refactoring.getInlinedToOperation().getClassOwner());

        refactoringTemplate.sourceOperation=refactoring.getInlinedOperation();
        refactoringTemplate.targetOperation=refactoring.getInlinedToOperation();

        return refactoringTemplate;

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, MoveClassRefactoring refactoring) {

        return new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getOriginalClass(),refactoring.getMovedClass());

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, MoveOperationRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate =  new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getOriginalOperation().getClassOwner(),refactoring.getMovedOperation().getClassOwner());

        refactoringTemplate.sourceOperation=refactoring.getOriginalOperation();
        refactoringTemplate.targetOperation=refactoring.getMovedOperation();

        return refactoringTemplate;

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, MoveSourceFolderRefactoring refactoring) {

        return new RefactoringTemplate(commitId,refactoring.getName()
                ,null,null);

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, PullUpAttributeRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate =  new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getSourceClass(),refactoring.getTargetClass());

        refactoringTemplate.sourceAttribute=refactoring.getMovedAttribute();


        return refactoringTemplate;

    }


    private static RefactoringTemplate GetRefactoringTemplate(String commitId, PullUpOperationRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate =  new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getOriginalOperation().getClassOwner(),refactoring.getMovedOperation().getClassOwner());

        refactoringTemplate.sourceOperation=refactoring.getOriginalOperation();
        refactoringTemplate.targetOperation=refactoring.getMovedOperation();

        return refactoringTemplate;

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, PushDownAttributeRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate =  new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getSourceClass(),refactoring.getTargetClass());

        refactoringTemplate.sourceAttribute=refactoring.getMovedAttribute();

        return refactoringTemplate;
    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, PushDownOperationRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate = new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getOriginalOperation().getClassOwner(),refactoring.getMovedOperation().getClassOwner());

        refactoringTemplate.sourceOperation=refactoring.getOriginalOperation();
        refactoringTemplate.targetOperation=refactoring.getMovedOperation();

        return refactoringTemplate;

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, RenameClassRefactoring refactoring) {

        return new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getOriginalClass(),refactoring.getRenamedClass());

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, RenameOperationRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate = new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getOriginalOperation().getClassOwner(),null);

        refactoringTemplate.sourceOperation=refactoring.getOriginalOperation();
        refactoringTemplate.targetOperation=refactoring.getRenamedOperation();

        return refactoringTemplate;

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, RenamePackageRefactoring refactoring) {

        return new RefactoringTemplate(commitId,refactoring.getName()
                ,null,null);

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, ExtractOperationRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate = new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getExtractedFromOperation().getClassOwner(),refactoring.getExtractedOperation().getClassOwner());

        refactoringTemplate.sourceOperation=refactoring.getExtractedFromOperation();
        refactoringTemplate.targetOperation=refactoring.getExtractedOperation();

        return refactoringTemplate;

    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, ExtractAndMoveOperationRefactoring refactoring) {

        RefactoringTemplate refactoringTemplate = new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getExtractedFromOperation().getClassOwner(),refactoring.getExtractedOperation().getClassOwner());

        refactoringTemplate.sourceOperation=refactoring.getExtractedFromOperation();
        refactoringTemplate.targetOperation=refactoring.getExtractedOperation();

        return refactoringTemplate;
        
    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, ConvertAnonymousClassToTypeRefactoring refactoring) {
        
        return new RefactoringTemplate(commitId,refactoring.getName()
                ,refactoring.getAnonymousClass(),refactoring.getAddedClass());
    }

    private static RefactoringTemplate GetRefactoringTemplate(String commitId, MoveAttributeRefactoring refactoring) {

        return new RefactoringTemplate(commitId,refactoring.getName(),refactoring.getSourceClass(),refactoring.getTargetClass());
    }
}



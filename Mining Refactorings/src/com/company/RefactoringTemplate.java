package com.company;

import gr.uom.java.xmi.UMLAttribute;
import gr.uom.java.xmi.UMLBaseClass;
import gr.uom.java.xmi.UMLOperation;

public class RefactoringTemplate{

    public  RefactoringTemplate(String commitId, String refactoringName, UMLBaseClass sourceClass, UMLBaseClass targetClass) {

        this.commitId=commitId;
        this.refactoringName=refactoringName;
        this.sourceClass=sourceClass;
        this.targetClass=targetClass;

    }

    public String commitId;

    public String refactoringName;

    public UMLBaseClass sourceClass;

    public UMLBaseClass targetClass;

    public UMLOperation sourceOperation;

    public UMLOperation targetOperation;

    public UMLAttribute sourceAttribute;
}

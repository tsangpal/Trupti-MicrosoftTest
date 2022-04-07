import * as Lint from "tslint/lib";
import * as ts from "typescript";


export class Rule extends Lint.Rules.AbstractRule {
    public static FAILURE_STRING = "The '|' and '&' operators must be surrounded by single spaces";

    public apply(sourceFile: ts.SourceFile): Lint.RuleFailure[] {
        return this.applyWithWalker(new TypeOperatorSpacingWalker(sourceFile, this.getOptions()));
    }
}

class TypeOperatorSpacingWalker extends Lint.RuleWalker {
    public visitNode(node: ts.Node) {
        if (node.kind === ts.SyntaxKind.UnionType || node.kind === ts.SyntaxKind.IntersectionType) {
            const types = (<ts.UnionOrIntersectionTypeNode>node).types;
            let expectedStart = types[0].end + 2; // space, | or &
            for (let i = 1; i < types.length; i++) {
                const currentType = types[i];
                if (expectedStart !== currentType.pos || currentType.getLeadingTriviaWidth() !== 1) {
                    const sourceFile = currentType.getSourceFile();
                    const previousTypeEndPos = sourceFile.getLineAndCharacterOfPosition(types[i - 1].end);
                    const currentTypeStartPos = sourceFile.getLineAndCharacterOfPosition(currentType.pos);
                    if (previousTypeEndPos.line === currentTypeStartPos.line) {
                        const failure = this.createFailure(currentType.pos, currentType.getWidth(), Rule.FAILURE_STRING);
                        this.addFailure(failure);
                    }
                }
                expectedStart = currentType.end + 2;
            }
        }
        super.visitNode(node);
    }
}

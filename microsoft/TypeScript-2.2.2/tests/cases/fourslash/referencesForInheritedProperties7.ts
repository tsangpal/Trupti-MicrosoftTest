/// <reference path='fourslash.ts'/>

//// class class1 extends class1 {
////    [|{| "isWriteAccess": true, "isDefinition": true |}doStuff|]() { }
////    [|{| "isWriteAccess": true, "isDefinition": true |}propName|]: string;
//// }
//// interface interface1 extends interface1 {
////    [|{| "isWriteAccess": true, "isDefinition": true |}doStuff|](): void;
////    [|{| "isWriteAccess": true, "isDefinition": true |}propName|]: string;
//// }
//// class class2 extends class1 implements interface1 {
////    [|{| "isWriteAccess": true, "isDefinition": true |}doStuff|]() { }
////    [|{| "isWriteAccess": true, "isDefinition": true |}propName|]: string;
//// }
////
//// var v: class2;
//// v.[|doStuff|]();
//// v.[|propName|];

const [r0, r1, r2, r3, r4, r5, r6, r7] = test.ranges();
verify.referenceGroups(r0, [{ definition: "(method) class1.doStuff(): void", ranges: [r0, r4, r6] }]);
verify.referenceGroups(r1, [{ definition: "(property) class1.propName: string", ranges: [r1, r5, r7] }]);
verify.referenceGroups(r2, [{ definition: "(method) interface1.doStuff(): void", ranges: [r2, r4, r6] }]);
verify.referenceGroups(r3, [{ definition: "(property) interface1.propName: string", ranges: [r3, r5, r7] }]);
verify.referenceGroups(r4, [
    { definition: "(method) class1.doStuff(): void", ranges: [r0] },
    { definition: "(method) interface1.doStuff(): void", ranges: [r2] },
    { definition: "(method) class2.doStuff(): void", ranges: [r4, r6] }
]);
verify.referenceGroups([r5, r7], [
    { definition: "(property) class1.propName: string", ranges: [r1] },
    { definition: "(property) interface1.propName: string", ranges: [r3] },
    { definition: "(property) class2.propName: string", ranges: [r5, r7] }
]);
verify.referenceGroups(r6, [
    { definition: "(method) class1.doStuff(): void", ranges: [r0] },
    { definition: "(method) interface1.doStuff(): void", ranges: [r2] },
    { definition: "(method) class2.doStuff(): void", ranges: [r4] },
    { definition: "(method) class2.doStuff(): void", ranges: [r6] }
]);

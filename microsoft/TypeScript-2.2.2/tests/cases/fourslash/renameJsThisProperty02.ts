/// <reference path='fourslash.ts'/>

// @allowJs: true
// @Filename: a.js
////function bar() {
////    this./**/[|x|] = 10;
////}
////var t = new bar();
////t.[|x|] = 11;

goTo.marker();
verify.renameLocations( /*findInStrings*/ false, /*findInComments*/ false);
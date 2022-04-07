///<reference path="fourslash.ts" />

// Assignments to 'module.exports' create an external module

// @allowJs: true
// @Filename: myMod.js
//// if (true) {
////     module.exports = { a: 10 };
//// }
//// var invisible = true;

// @Filename: isGlobal.js
//// var y = 10;

// @Filename: consumer.js
//// var x = require('./myMod');
//// /**/;

goTo.file('consumer.js');
goTo.marker();

verify.completionListContains('y');
verify.not.completionListContains('invisible');

edit.insert('x.');
verify.memberListContains('a', undefined, undefined, 'property');
edit.insert('a.');
verify.memberListContains('toFixed', undefined, undefined, 'method');
